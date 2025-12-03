<template>
  <div class="space-y-6">
    <!-- Header -->
    <div class="flex justify-between items-center">
      <div>
        <h1 class="text-2xl font-bold text-sunskog-dark">{{ t('timesheets.title') }}</h1>
        <p v-if="isViewOnly" class="text-sm text-orange-600 mt-1">
          {{ t('timesheets.viewOnlyMode') }}
        </p>
      </div>
      <!-- Tlačítko pro nový výkaz - ne pro účetní a management -->
      <button
        v-if="canCreateTimesheets"
        @click="showCreateModal = true"
        class="bg-sunskog-primary hover:bg-sunskog-hover text-white px-4 py-2 rounded-lg flex items-center space-x-2 transition"
      >
        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
        </svg>
        <span>{{ t('timesheets.addTimesheet') }}</span>
      </button>
    </div>

    <!-- Tabs pro vedoucí a adminy -->
    <div v-if="showTabs" class="border-b border-gray-200">
      <nav class="flex space-x-8">
        <button
          v-if="canCreateTimesheets"
          @click="activeTab = 'my'"
          :class="tabClass('my')"
        >
          {{ t('timesheets.myTimesheets') }}
        </button>
        <button
          v-if="authStore.isTeamLead && !authStore.isAccountant"
          @click="activeTab = 'team'"
          :class="tabClass('team')"
        >
          {{ t('timesheets.teamTimesheets') }}
        </button>
        <button
          v-if="authStore.canViewAllTimesheets && !authStore.isTeamLead"
          @click="activeTab = 'all'"
          :class="tabClass('all')"
        >
          {{ t('timesheets.allTimesheets') }}
        </button>
      </nav>
    </div>

    <!-- Loading -->
    <div v-if="loading" class="bg-white rounded-lg shadow p-8 text-center">
      <div class="animate-spin w-8 h-8 border-4 border-sunskog-primary border-t-transparent rounded-full mx-auto"></div>
      <p class="mt-4 text-gray-600">{{ t('common.loading') }}</p>
    </div>

    <!-- Error -->
    <div v-else-if="error" class="bg-red-50 border border-red-200 rounded-lg p-6 text-center">
      <p class="text-red-600">{{ error }}</p>
      <button @click="loadTimesheets" class="mt-4 text-sunskog-primary hover:underline">
        {{ t('common.tryAgain') }}
      </button>
    </div>

    <!-- Seznam výkazů -->
    <div v-else class="bg-white rounded-lg shadow overflow-hidden">
      <table class="min-w-full divide-y divide-gray-200">
        <thead class="bg-gray-50">
          <tr>
            <th class="px-4 py-3 text-center text-xs font-medium text-gray-500 uppercase tracking-wider w-12">
              #
            </th>
            <!-- Sloupec pro jméno - jen když vidíme cizí výkazy -->
            <th v-if="activeTab !== 'my'" class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
              {{ t('admin.reports.employee') }}
            </th>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
              {{ t('timesheets.period') }}
            </th>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
              {{ t('timesheets.status') }}
            </th>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
              {{ t('timesheets.hours') }}
            </th>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
              {{ t('timesheets.km') }}
            </th>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
              {{ t('timesheets.total') }}
            </th>
            <th class="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
              {{ t('common.actions') }}
            </th>
          </tr>
        </thead>
        <tbody class="bg-white divide-y divide-gray-200">
          <tr v-if="filteredTimesheets.length === 0">
            <td :colspan="activeTab !== 'my' ? 8 : 7" class="px-6 py-8 text-center text-gray-500">
              {{ activeTab === 'team' ? t('timesheets.noTeamTimesheets') : t('timesheets.noTimesheets') }}
            </td>
          </tr>
          <tr 
            v-for="(ts, index) in filteredTimesheets" 
            :key="ts.id"
            class="hover:bg-gray-50 cursor-pointer"
            @click="openDetail(ts)"
          >
            <td class="px-4 py-4 text-center text-sm text-gray-400">
              {{ index + 1 }}
            </td>
            <td v-if="activeTab !== 'my'" class="px-6 py-4 whitespace-nowrap">
              <div class="flex items-center">
                <div class="w-8 h-8 bg-sunskog-primary rounded-full flex items-center justify-center text-white text-xs font-bold mr-2">
                  {{ getInitials(ts.userName || ts.userEmail) }}
                </div>
                <span class="text-sm text-gray-900">{{ ts.userName || ts.userEmail }}</span>
              </div>
            </td>
            <td class="px-6 py-4 whitespace-nowrap">
              <div class="text-sm font-medium text-gray-900">
                {{ formatDate(ts.periodStart) }} – {{ formatDate(ts.periodEnd) }}
              </div>
            </td>
            <td class="px-6 py-4 whitespace-nowrap">
              <span :class="statusClass(ts.status)">
                {{ statusLabel(ts.status) }}
              </span>
            </td>
            <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
              {{ ts.totalHours }}h
            </td>
            <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
              {{ ts.totalKm }} km
            </td>
            <td class="px-6 py-4 whitespace-nowrap text-sm font-semibold text-gray-900">
              {{ formatCurrency(ts.totalPay) }}
            </td>
            <td class="px-6 py-4 whitespace-nowrap text-right text-sm">
              <button
                @click.stop="openDetail(ts)"
                class="text-sunskog-primary hover:text-sunskog-hover font-medium"
              >
                {{ t('timesheets.detail') }}
              </button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Modal: Vytvořit nový výkaz -->
    <div v-if="showCreateModal" class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
      <div class="bg-white rounded-lg shadow-xl w-full max-w-md p-6">
        <h2 class="text-xl font-bold text-sunskog-dark mb-4">{{ t('timesheets.addTimesheet') }}</h2>
        
        <form @submit.prevent="createTimesheet" class="space-y-4">
          <div class="grid grid-cols-2 gap-4">
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('timesheets.periodStart') }}</label>
              <input
                v-model="newTimesheet.periodStart"
                type="date"
                required
                class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sunskog-primary"
              />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('timesheets.periodEnd') }}</label>
              <input
                v-model="newTimesheet.periodEnd"
                type="date"
                required
                class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sunskog-primary"
              />
            </div>
          </div>

          <div class="flex justify-end space-x-3 pt-4">
            <button type="button" @click="showCreateModal = false" class="px-4 py-2 text-gray-600 hover:text-gray-800">
              {{ t('common.cancel') }}
            </button>
            <button type="submit" :disabled="creating" class="px-4 py-2 bg-sunskog-primary hover:bg-sunskog-hover text-white rounded-lg disabled:opacity-50">
              {{ creating ? t('common.loading') : t('common.save') }}
            </button>
          </div>
        </form>
      </div>
    </div>

    <!-- Modal: Detail výkazu -->
    <div v-if="selectedTimesheet" class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
      <div class="bg-white rounded-lg shadow-xl w-full max-w-4xl max-h-[90vh] overflow-hidden flex flex-col">
        <!-- Header -->
        <div class="p-6 border-b border-gray-200">
          <div class="flex justify-between items-start">
            <div>
              <h2 class="text-xl font-bold text-sunskog-dark">
                {{ t('timesheets.detail') }}: {{ formatDate(selectedTimesheet.periodStart) }} – {{ formatDate(selectedTimesheet.periodEnd) }}
              </h2>
              <p v-if="selectedTimesheet.userName" class="text-gray-500 mt-1">
                {{ selectedTimesheet.userName || selectedTimesheet.userEmail }}
              </p>
              <p v-if="isViewOnly" class="text-sm text-orange-600 mt-1">
                {{ t('common.viewOnly') }}
              </p>
            </div>
            <span :class="statusClass(selectedTimesheet.status)" class="text-sm">
              {{ statusLabel(selectedTimesheet.status) }}
            </span>
          </div>
          
          <!-- Souhrn -->
          <div class="mt-4 grid grid-cols-4 gap-4">
            <div class="bg-gray-50 p-3 rounded-lg text-center">
              <p class="text-sm text-gray-500">{{ t('timesheets.hours') }}</p>
              <p class="text-xl font-bold text-sunskog-dark">{{ selectedTimesheet.totalHours || 0 }}h</p>
            </div>
            <div class="bg-gray-50 p-3 rounded-lg text-center">
              <p class="text-sm text-gray-500">{{ t('timesheets.km') }}</p>
              <p class="text-xl font-bold text-sunskog-dark">{{ selectedTimesheet.totalKm || 0 }} km</p>
            </div>
            <div class="bg-gray-50 p-3 rounded-lg text-center">
              <p class="text-sm text-gray-500">{{ t('timesheets.pieces') }}</p>
              <p class="text-xl font-bold text-sunskog-dark">{{ selectedTimesheet.totalPieces || 0 }}</p>
            </div>
            <div class="bg-sunskog-primary/10 p-3 rounded-lg text-center">
              <p class="text-sm text-sunskog-primary">{{ t('timesheets.total') }}</p>
              <p class="text-xl font-bold text-sunskog-primary">{{ formatCurrency(selectedTimesheet.totalPay || 0) }}</p>
            </div>
          </div>
        </div>

        <!-- Entries Table -->
        <div class="flex-1 overflow-y-auto p-6">
          <div class="flex justify-between items-center mb-4">
            <h3 class="font-semibold text-sunskog-dark">{{ t('timesheets.entries') }}</h3>
            <!-- Přidat položku - pouze pokud může upravovat -->
            <button
              v-if="canEditThisTimesheet"
              @click="showEntryModal = true; editingEntry = null; resetEntryForm()"
              class="text-sm bg-sunskog-primary hover:bg-sunskog-hover text-white px-3 py-1 rounded flex items-center space-x-1"
            >
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
              </svg>
              <span>{{ t('timesheets.addEntry') }}</span>
            </button>
          </div>

          <div v-if="loadingEntries" class="text-center py-8">
            <div class="animate-spin w-6 h-6 border-4 border-sunskog-primary border-t-transparent rounded-full mx-auto"></div>
          </div>

          <table v-else class="min-w-full divide-y divide-gray-200">
            <thead class="bg-gray-50">
              <tr>
                <th class="px-4 py-2 text-center text-xs font-medium text-gray-500 w-10">#</th>
                <th class="px-4 py-2 text-left text-xs font-medium text-gray-500">{{ t('timesheets.date') }}</th>
                <th class="px-4 py-2 text-left text-xs font-medium text-gray-500">{{ t('timesheets.project') }}</th>
                <th class="px-4 py-2 text-left text-xs font-medium text-gray-500">{{ t('timesheets.task') }}</th>
                <th class="px-4 py-2 text-right text-xs font-medium text-gray-500">{{ t('timesheets.hours') }}</th>
                <th class="px-4 py-2 text-right text-xs font-medium text-gray-500">{{ t('timesheets.km') }}</th>
                <th class="px-4 py-2 text-right text-xs font-medium text-gray-500">{{ t('timesheets.pieces') }}</th>
                <th class="px-4 py-2 text-right text-xs font-medium text-gray-500">{{ t('timesheets.amount') }}</th>
                <th v-if="canEditThisTimesheet" class="px-4 py-2"></th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-200">
              <tr v-if="entries.length === 0">
                <td :colspan="canEditThisTimesheet ? 9 : 8" class="px-4 py-8 text-center text-gray-500">
                  {{ t('timesheets.noEntries') }}
                </td>
              </tr>
              <tr v-for="(entry, index) in entries" :key="entry.id" class="hover:bg-gray-50">
                <td class="px-4 py-2 text-sm text-center text-gray-400">{{ index + 1 }}</td>
                <td class="px-4 py-2 text-sm">{{ formatDate(entry.workDate) }}</td>
                <td class="px-4 py-2 text-sm">{{ entry.project || '-' }}</td>
                <td class="px-4 py-2 text-sm">{{ entry.task || '-' }}</td>
                <td class="px-4 py-2 text-sm text-right">{{ entry.hours }}h</td>
                <td class="px-4 py-2 text-sm text-right">{{ entry.km }}</td>
                <td class="px-4 py-2 text-sm text-right">{{ entry.pieces }}</td>
                <td class="px-4 py-2 text-sm text-right font-medium">{{ formatCurrency(entry.entryPay) }}</td>
                <td v-if="canEditThisTimesheet" class="px-4 py-2 text-right">
                  <div class="flex space-x-2 justify-end">
                    <button @click="editEntry(entry)" class="text-blue-600 hover:text-blue-800">
                      <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                      </svg>
                    </button>
                    <button @click="deleteEntry(entry)" class="text-red-600 hover:text-red-800">
                      <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                      </svg>
                    </button>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>

          <!-- Sazby info (pouze pro čtení) -->
          <div class="mt-6 p-4 bg-gray-50 rounded-lg">
            <h4 class="font-medium text-gray-700 mb-2">
              {{ isTimesheetLocked ? t('rates.usedRates') : t('rates.title') }}
            </h4>
            
            <!-- Varování pokud jsou různé sazby -->
            <div v-if="hasDifferentRates" class="mb-3 p-3 bg-orange-100 border border-orange-300 rounded-lg">
              <div class="flex items-center text-orange-800 mb-2">
                <svg class="w-5 h-5 mr-2 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
                </svg>
                <span class="text-sm font-medium">{{ t('timesheets.differentRatesWarning') }}</span>
              </div>
              
              <!-- Tabulka různých sazeb -->
              <table class="w-full text-sm text-orange-800">
                <thead>
                  <tr class="border-b border-orange-200">
                    <th class="text-left py-1 font-medium" style="width: 20%">{{ t('timesheets.recordNumbers') }}</th>
                    <th class="text-right py-1 font-medium" style="width: 26%">{{ t('rates.hourlyRate') }}</th>
                    <th class="text-right py-1 font-medium" style="width: 27%">{{ t('rates.kmRate') }}</th>
                    <th class="text-right py-1 font-medium" style="width: 27%">{{ t('rates.pieceRate') }}</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="(rateSet, index) in uniqueRatesInEntries" :key="index">
                    <td class="py-1">{{ rateSet.entryNumbers.join(', ') }}</td>
                    <td class="text-right py-1">{{ rateSet.hourRate }} kr/h</td>
                    <td class="text-right py-1">{{ rateSet.kmRate }} kr/km</td>
                    <td class="text-right py-1">{{ rateSet.pieceRate }} kr/ks</td>
                  </tr>
                </tbody>
              </table>
            </div>
            
            <!-- Standardní zobrazení sazeb (pokud jsou všechny stejné) -->
            <div v-if="!hasDifferentRates" class="grid grid-cols-3 gap-4 text-sm text-gray-600">
              <div>{{ t('rates.hourlyRate') }}: <span class="font-medium">{{ displayRates.hourlyRate }} kr/h</span></div>
              <div>{{ t('rates.kmRate') }}: <span class="font-medium">{{ displayRates.kmRate }} kr/km</span></div>
              <div>{{ t('rates.pieceRate') }}: <span class="font-medium">{{ displayRates.pieceRate }} kr/ks</span></div>
            </div>
            
            <p v-if="isTimesheetLocked && !hasDifferentRates" class="text-xs text-gray-500 mt-2">
              {{ t('rates.lockedRatesNote') }}
            </p>
          </div>
        </div>

        <!-- Footer s akcemi -->
        <div class="p-6 border-t border-gray-200 flex justify-between">
          <button @click="selectedTimesheet = null" class="px-4 py-2 text-gray-600 hover:text-gray-800">
            {{ t('common.close') }}
          </button>
          
          <div class="flex space-x-3">
            <!-- Submit - pouze pro vlastní výkazy v Draft/Returned a pokud může upravovat -->
            <button
              v-if="canSubmitThisTimesheet"
              @click="submitTimesheet"
              :disabled="submitting"
              class="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg disabled:opacity-50"
            >
              {{ submitting ? t('common.loading') : t('timesheets.submit') }}
            </button>
            
            <!-- Approve/Return - pouze pro cizí výkazy v Submitted a pokud může schvalovat -->
            <template v-if="canApproveThisTimesheet">
              <button
                @click="returnTimesheet"
                :disabled="submitting"
                class="px-4 py-2 bg-orange-500 hover:bg-orange-600 text-white rounded-lg disabled:opacity-50"
              >
                {{ t('timesheets.return') }}
              </button>
              <button
                @click="approveTimesheet"
                :disabled="submitting"
                class="px-4 py-2 bg-green-600 hover:bg-green-700 text-white rounded-lg disabled:opacity-50"
              >
                {{ submitting ? t('common.loading') : t('timesheets.approve') }}
              </button>
            </template>
          </div>
        </div>
      </div>
    </div>

    <!-- Modal: Přidat/Upravit položku -->
    <div v-if="showEntryModal" class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
      <div class="bg-white rounded-lg shadow-xl w-full max-w-lg p-6">
        <h2 class="text-xl font-bold text-sunskog-dark mb-4">
          {{ editingEntry ? t('timesheets.editEntry') : t('timesheets.addEntry') }}
        </h2>
        
        <form @submit.prevent="saveEntry" class="space-y-4 max-h-[70vh] overflow-y-auto pr-2">
          <!-- Datum a Projekt -->
          <div class="grid grid-cols-2 gap-4">
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('timesheets.date') }} *</label>
              <input 
                v-model="entryForm.workDate" 
                type="date" 
                required 
                :min="selectedTimesheet?.periodStart?.split('T')[0]" 
                :max="selectedTimesheet?.periodEnd?.split('T')[0]"
                class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sunskog-primary" 
              />
              <p class="text-xs text-gray-500 mt-1">
                {{ t('timesheets.allowedPeriod') }}: {{ formatDate(selectedTimesheet?.periodStart) }} – {{ formatDate(selectedTimesheet?.periodEnd) }}
              </p>
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('timesheets.project') }}</label>
              <input v-model="entryForm.project" type="text" class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sunskog-primary" placeholder="SE-FOREST-001" />
            </div>
          </div>

          <!-- Úkol -->
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('timesheets.task') }}</label>
            <input v-model="entryForm.task" type="text" class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sunskog-primary" :placeholder="t('timesheets.taskPlaceholder')" />
          </div>

          <!-- Plocha -->
          <div class="grid grid-cols-2 gap-4">
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('timesheets.areaName') }}</label>
              <input v-model="entryForm.areaName" type="text" class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sunskog-primary" :placeholder="t('timesheets.areaNamePlaceholder')" />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('timesheets.areaCode') }}</label>
              <input v-model="entryForm.areaCode" type="text" class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sunskog-primary" :placeholder="t('timesheets.areaCodePlaceholder')" />
            </div>
          </div>

          <!-- Časy -->
          <div class="grid grid-cols-4 gap-4">
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('timesheets.fromTime') }}</label>
              <input v-model="entryForm.fromTime" type="time" class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sunskog-primary" />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('timesheets.toTime') }}</label>
              <input v-model="entryForm.toTime" type="time" class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sunskog-primary" />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('timesheets.pauseMinutes') }}</label>
              <input v-model.number="entryForm.pauseMinutes" type="number" min="0" class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sunskog-primary" />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('timesheets.hours') }}</label>
              <input v-model.number="entryForm.hours" type="number" step="0.5" min="0" class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sunskog-primary" />
            </div>
          </div>

          <!-- Úkolová mzda - kusy -->
          <div class="bg-yellow-50 p-3 rounded-lg">
            <p class="text-xs font-medium text-yellow-800 mb-2">{{ t('timesheets.pieceWorkSection') }}</p>
            <div class="grid grid-cols-3 gap-4">
              <div>
                <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('timesheets.pieces') }}</label>
                <input v-model.number="entryForm.pieces" type="number" min="0" class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sunskog-primary" />
              </div>
              <div>
                <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('timesheets.trKind') }}</label>
                <select v-model="entryForm.trKind" class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sunskog-primary">
                  <option value="">-- {{ t('common.select') }} --</option>
                  <option value="tack">Täckrot</option>
                  <option value="barrot">Barrot</option>
                  <option value="plugg">Plugg</option>
                </select>
              </div>
              <div>
                <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('timesheets.extraNote') }}</label>
                <input v-model="entryForm.extraNote" type="text" class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sunskog-primary" />
              </div>
            </div>
          </div>

          <!-- Úkolová mzda - hektary -->
          <div class="bg-green-50 p-3 rounded-lg">
            <p class="text-xs font-medium text-green-800 mb-2">{{ t('timesheets.hectareWorkSection') }}</p>
            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('timesheets.hectares') }}</label>
                <input v-model.number="entryForm.hectares" type="number" step="0.01" min="0" class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sunskog-primary" />
              </div>
              <div>
                <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('timesheets.boxCarryCount') }}</label>
                <input v-model.number="entryForm.boxCarryCount" type="number" min="0" class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sunskog-primary" />
              </div>
            </div>
          </div>

          <!-- Cestovné -->
          <div class="bg-blue-50 p-3 rounded-lg">
            <p class="text-xs font-medium text-blue-800 mb-2">{{ t('timesheets.travelSection') }}</p>
            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('timesheets.km') }}</label>
                <input v-model.number="entryForm.km" type="number" min="0" class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sunskog-primary" />
              </div>
              <div>
                <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('timesheets.travelMinutes') }}</label>
                <input v-model.number="entryForm.travelMinutes" type="number" min="0" class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sunskog-primary" />
              </div>
            </div>
          </div>

          <!-- Komentář -->
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('timesheets.comment') }}</label>
            <input v-model="entryForm.comment" type="text" class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sunskog-primary" />
          </div>

          <!-- Výpočet -->
          <div class="bg-gray-50 p-4 rounded-lg">
            <p class="text-sm text-gray-600">
              {{ t('timesheets.estimate') }}: <span class="font-bold text-sunskog-primary">{{ formatCurrency(calculateEntryPay()) }}</span>
            </p>
          </div>

          <div class="flex justify-end space-x-3 pt-4">
            <button type="button" @click="showEntryModal = false" class="px-4 py-2 text-gray-600 hover:text-gray-800">
              {{ t('common.cancel') }}
            </button>
            <button type="submit" :disabled="savingEntry" class="px-4 py-2 bg-sunskog-primary hover:bg-sunskog-hover text-white rounded-lg disabled:opacity-50">
              {{ savingEntry ? t('common.loading') : t('common.save') }}
            </button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import api from '@/services/api'

const { t, locale } = useI18n()
const route = useRoute()
const authStore = useAuthStore()

// State
const loading = ref(true)
const error = ref<string | null>(null)
const timesheets = ref<any[]>([])
const selectedTimesheet = ref<any | null>(null)
const entries = ref<any[]>([])
const loadingEntries = ref(false)

// Nastavit aktivní tab z query parametru (např. ?tab=team)
const activeTab = ref((route.query.tab as string) || 'my')

// Modals
const showCreateModal = ref(false)
const showEntryModal = ref(false)
const creating = ref(false)
const submitting = ref(false)
const savingEntry = ref(false)
const editingEntry = ref<any | null>(null)

// Forms
const newTimesheet = reactive({ periodStart: '', periodEnd: '', notes: '' })
const entryForm = reactive({
  workDate: '',
  project: '',
  task: '',
  // Časové údaje
  fromTime: '',
  toTime: '',
  pauseMinutes: 0,
  travelMinutes: 0,
  // Plocha
  areaName: '',
  areaCode: '',
  // Práce
  hours: 0,
  km: 0,
  pieces: 0,
  trKind: '',
  extraNote: '',
  boxCarryCount: 0,
  // Hektary
  hectares: 0,
  hectareRate: 0,
  // Komentář
  comment: ''
})

// Sazby z localStorage
const rates = reactive({
  hourlyRate: 150,
  kmRate: 2.5,
  pieceRate: 10,
  hectareRate: 100
})

// Členové týmu (pro TeamLeada)
const teamMemberIds = ref<string[]>([])

// Automatický výpočet hodin z Od-Do minus pauza
watch(
  () => [entryForm.fromTime, entryForm.toTime, entryForm.pauseMinutes],
  ([fromTime, toTime, pauseMinutes]) => {
    if (fromTime && toTime) {
      // Parsovat časy
      const [fromH, fromM] = (fromTime as string).split(':').map(Number)
      const [toH, toM] = (toTime as string).split(':').map(Number)
      
      // Výpočet v minutách
      let fromMinutes = fromH * 60 + fromM
      let toMinutes = toH * 60 + toM
      
      // Pokud je "do" menší než "od", přičteme 24 hodin (noční směna)
      if (toMinutes < fromMinutes) {
        toMinutes += 24 * 60
      }
      
      // Rozdíl minus pauza
      const totalMinutes = toMinutes - fromMinutes - ((pauseMinutes as number) || 0)
      
      // Převést na hodiny (zaokrouhlit na 0.5)
      if (totalMinutes > 0) {
        const hours = Math.round((totalMinutes / 60) * 2) / 2 // Zaokrouhlení na 0.5
        entryForm.hours = hours
      } else {
        entryForm.hours = 0
      }
    }
  }
)

// Načíst aktuální sazby z API
const loadCurrentRates = async () => {
  try {
    const data = await api.get<any>('/api/rates/current')
    rates.hourlyRate = data.hourRate || 150
    rates.kmRate = data.kmRate || 2.5
    rates.pieceRate = data.pieceRate || 10
    
    // Uložit do localStorage jako fallback
    localStorage.setItem('sunskog_rates', JSON.stringify({
      hourlyRate: data.hourRate,
      kmRate: data.kmRate,
      pieceRate: data.pieceRate
    }))
  } catch (e) {
    console.warn('Failed to load rates from API, using localStorage fallback')
    const savedRates = localStorage.getItem('sunskog_rates')
    if (savedRates) {
      const parsed = JSON.parse(savedRates)
      rates.hourlyRate = parsed.hourlyRate || 150
      rates.kmRate = parsed.kmRate || 2.5
      rates.pieceRate = parsed.pieceRate || 10
    }
  }
}

// Načíst sazby
onMounted(async () => {
  // Načíst aktuální sazby z API
  await loadCurrentRates()
  
  // Nastavit výchozí tab podle role
  if (authStore.isAccountant && !authStore.isAdmin) {
    activeTab.value = 'all'
  } else if (authStore.isManagement && !authStore.isAdmin) {
    activeTab.value = 'all'
  } else if (authStore.isTeamLead && !authStore.canViewAllTimesheets) {
    // TeamLead - výchozí záložka je tým (pokud nepřišel z URL)
    if (!route.query.tab) {
      activeTab.value = 'team'
    }
  }
  
  // Pro TeamLeada načíst členy týmu
  if (authStore.isTeamLead) {
    await loadTeamMembers()
  }
  
  loadTimesheets()
})

// Načíst členy týmu pro vedoucího
const loadTeamMembers = async () => {
  try {
    const userId = authStore.user?.id
    
    // Použít nový endpoint /api/my-team
    const response = await api.get<any>('/api/my-team')
    
    if (response.team) {
      // Získat ID všech členů týmu
      const memberIds = response.members?.map((m: any) => m.userId) || []
      
      // Přidat vedoucího pokud tam není
      if (response.team.leadUserId && !memberIds.includes(response.team.leadUserId)) {
        memberIds.push(response.team.leadUserId)
      }
      
      // Přidat sebe pokud tam nejsem
      if (userId && !memberIds.includes(userId)) {
        memberIds.push(userId)
      }
      
      teamMemberIds.value = memberIds
    } else {
      // Není v žádném týmu - zobrazí jen své výkazy
      teamMemberIds.value = [userId || '']
    }
  } catch (e) {
    console.error('Failed to load team members', e)
    // Fallback - jen vlastní ID
    teamMemberIds.value = [authStore.user?.id || '']
  }
}

// Computed pro role-based přístup
const isViewOnly = computed(() => authStore.isAccountant && !authStore.isAdmin && !authStore.isManagement)

const canCreateTimesheets = computed(() => {
  // Účetní a management nemohou vytvářet výkazy
  if (authStore.isAccountant && !authStore.isAdmin) return false
  if (authStore.isManagement && !authStore.isAdmin) return false
  return true
})

const showTabs = computed(() => {
  // Ukazovat taby pokud může vidět více než jen své výkazy
  return authStore.isTeamLead || authStore.canViewAllTimesheets || authStore.isAdmin
})

const filteredTimesheets = computed(() => {
  const userId = authStore.user?.id
  
  if (activeTab.value === 'my') {
    return timesheets.value.filter(ts => ts.userId === userId)
  } else if (activeTab.value === 'team') {
    // Pro vedoucí - výkazy všech členů týmu (včetně vlastních)
    if (teamMemberIds.value.length > 0) {
      const filtered = timesheets.value.filter(ts => teamMemberIds.value.includes(ts.userId))
      return filtered
    }
    // Fallback - zobrazit jen vlastní
    return timesheets.value.filter(ts => ts.userId === userId)
  } else {
    // Všechny
    return timesheets.value
  }
})

const canEditThisTimesheet = computed(() => {
  if (!selectedTimesheet.value) return false
  if (isViewOnly.value) return false
  
  const isOwner = selectedTimesheet.value.userId === authStore.user?.id
  const isDraftOrReturned = ['Draft', 'Returned'].includes(selectedTimesheet.value.status)
  
  // Pouze vlastník může upravovat a pouze pokud je Draft nebo Returned
  return isOwner && isDraftOrReturned
})

const canSubmitThisTimesheet = computed(() => {
  if (!selectedTimesheet.value) return false
  if (isViewOnly.value) return false
  
  const isOwner = selectedTimesheet.value.userId === authStore.user?.id
  const isDraftOrReturned = ['Draft', 'Returned'].includes(selectedTimesheet.value.status)
  
  return isOwner && isDraftOrReturned
})

const canApproveThisTimesheet = computed(() => {
  if (!selectedTimesheet.value) return false
  if (isViewOnly.value) return false
  
  const isOwner = selectedTimesheet.value.userId === authStore.user?.id
  const isSubmitted = selectedTimesheet.value.status === 'Submitted'
  
  // Nesmí schvalovat vlastní výkazy
  if (isOwner) return false
  
  // Musí být Submitted a mít oprávnění schvalovat
  return isSubmitted && authStore.canApproveTimesheets
})

// Je výkaz uzamčený? (Submitted nebo Approved)
const isTimesheetLocked = computed(() => {
  if (!selectedTimesheet.value) return false
  return ['Submitted', 'Approved'].includes(selectedTimesheet.value.status)
})

// Kontrola zda mají položky různé sazby
const hasDifferentRates = computed(() => {
  if (entries.value.length < 2) return false
  
  const firstEntry = entries.value[0]
  return entries.value.some(entry => 
    entry.hourRate !== firstEntry.hourRate ||
    entry.kmRate !== firstEntry.kmRate ||
    entry.pieceRate !== firstEntry.pieceRate
  )
})

// Všechny unikátní sazby v položkách
const uniqueRatesInEntries = computed(() => {
  const ratesMap = new Map<string, { hourRate: number, kmRate: number, pieceRate: number, entryNumbers: number[] }>()
  
  entries.value.forEach((entry, index) => {
    const key = `${entry.hourRate}-${entry.kmRate}-${entry.pieceRate}`
    if (!ratesMap.has(key)) {
      ratesMap.set(key, {
        hourRate: entry.hourRate || 0,
        kmRate: entry.kmRate || 0,
        pieceRate: entry.pieceRate || 0,
        entryNumbers: []
      })
    }
    ratesMap.get(key)!.entryNumbers.push(index + 1) // +1 protože zobrazujeme od 1, ne od 0
  })
  
  return Array.from(ratesMap.values())
})

// Sazby k zobrazení - pro uzamčené výkazy použít sazby z položek
const displayRates = computed(() => {
  // Pokud je výkaz uzamčený a má položky, použít sazby z první položky
  if (isTimesheetLocked.value && entries.value.length > 0) {
    const firstEntry = entries.value[0]
    return {
      hourlyRate: firstEntry.hourRate || 0,
      kmRate: firstEntry.kmRate || 0,
      pieceRate: firstEntry.pieceRate || 0
    }
  }
  // Jinak použít aktuální sazby z nastavení
  return rates
})

// Helpers
const tabClass = (tab: string) => [
  'py-2 px-1 border-b-2 font-medium text-sm transition',
  activeTab.value === tab 
    ? 'border-sunskog-primary text-sunskog-primary' 
    : 'border-transparent text-gray-500 hover:text-gray-700'
]

const formatDate = (dateStr: string) => {
  if (!dateStr) return '-'
  const localeCode = locale.value === 'cs' ? 'cs-CZ' : 'en-US'
  return new Date(dateStr).toLocaleDateString(localeCode)
}

const formatCurrency = (amount: number) => {
  return `${(amount || 0).toFixed(2).replace('.', ',')} kr`
}

const statusClass = (status: string) => {
  const classes: Record<string, string> = {
    'Draft': 'px-2 py-1 text-xs font-medium rounded-full bg-gray-100 text-gray-800',
    'Submitted': 'px-2 py-1 text-xs font-medium rounded-full bg-blue-100 text-blue-800',
    'Approved': 'px-2 py-1 text-xs font-medium rounded-full bg-green-100 text-green-800',
    'Returned': 'px-2 py-1 text-xs font-medium rounded-full bg-orange-100 text-orange-800'
  }
  return classes[status] || classes['Draft']
}

const statusLabel = (status: string) => {
  const labels: Record<string, string> = {
    'Draft': t('timesheets.statusDraft'),
    'Submitted': t('timesheets.statusPending'),
    'Approved': t('timesheets.statusApproved'),
    'Returned': t('timesheets.statusReturned')
  }
  return labels[status] || status
}

const getInitials = (name: string) => {
  if (!name) return '?'
  return name.split(' ').map(n => n[0]).join('').toUpperCase().substring(0, 2)
}

const calculateEntryPay = () => {
  const hoursPay = (entryForm.hours || 0) * rates.hourlyRate
  const kmPay = (entryForm.km || 0) * rates.kmRate
  const piecesPay = (entryForm.pieces || 0) * rates.pieceRate
  const hectaresPay = (entryForm.hectares || 0) * (entryForm.hectareRate || rates.hectareRate)
  return hoursPay + kmPay + piecesPay + hectaresPay
}

// API Methods
const loadTimesheets = async () => {
  loading.value = true
  error.value = null
  try {
    const data = await api.get<any[]>('/api/timesheets')
    timesheets.value = data
  } catch (e: any) {
    error.value = e.response?.data?.error || t('common.loadError')
  } finally {
    loading.value = false
  }
}

const createTimesheet = async () => {
  creating.value = true
  try {
    await api.post('/api/timesheets', {
      periodStart: newTimesheet.periodStart,
      periodEnd: newTimesheet.periodEnd,
      notes: newTimesheet.notes || null
    })
    showCreateModal.value = false
    newTimesheet.periodStart = ''
    newTimesheet.periodEnd = ''
    newTimesheet.notes = ''
    await loadTimesheets()
  } catch (e: any) {
    alert(e.response?.data?.error || t('common.saveError'))
  } finally {
    creating.value = false
  }
}

const openDetail = async (ts: any) => {
  selectedTimesheet.value = ts
  loadingEntries.value = true
  try {
    const data = await api.get<any>(`/api/timesheets/${ts.id}/entries`)
    entries.value = data.entries || []
    selectedTimesheet.value = {
      ...selectedTimesheet.value,
      totalHours: data.totalHours,
      totalKm: data.totalKm,
      totalPieces: data.totalPieces,
      totalPay: data.totalPay
    }
  } catch {
    entries.value = []
  } finally {
    loadingEntries.value = false
  }
}

const submitTimesheet = async () => {
  if (!selectedTimesheet.value) return
  submitting.value = true
  try {
    await api.post(`/api/timesheets/${selectedTimesheet.value.id}/submit`)
    selectedTimesheet.value.status = 'Submitted'
    await loadTimesheets()
  } catch (e: any) {
    alert(e.response?.data?.error || t('common.saveError'))
  } finally {
    submitting.value = false
  }
}

const approveTimesheet = async () => {
  if (!selectedTimesheet.value) return
  submitting.value = true
  try {
    await api.post(`/api/timesheets/${selectedTimesheet.value.id}/approve`)
    selectedTimesheet.value.status = 'Approved'
    await loadTimesheets()
  } catch (e: any) {
    alert(e.response?.data?.error || t('common.saveError'))
  } finally {
    submitting.value = false
  }
}

const returnTimesheet = async () => {
  if (!selectedTimesheet.value) return
  submitting.value = true
  try {
    await api.post(`/api/timesheets/${selectedTimesheet.value.id}/return`)
    selectedTimesheet.value.status = 'Returned'
    await loadTimesheets()
  } catch (e: any) {
    alert(e.response?.data?.error || t('common.saveError'))
  } finally {
    submitting.value = false
  }
}

const resetEntryForm = () => {
  entryForm.workDate = new Date().toISOString().split('T')[0]
  entryForm.project = ''
  entryForm.task = ''
  entryForm.fromTime = ''
  entryForm.toTime = ''
  entryForm.pauseMinutes = 0
  entryForm.travelMinutes = 0
  entryForm.areaName = ''
  entryForm.areaCode = ''
  entryForm.hours = 0
  entryForm.km = 0
  entryForm.pieces = 0
  entryForm.trKind = ''
  entryForm.extraNote = ''
  entryForm.boxCarryCount = 0
  entryForm.hectares = 0
  entryForm.hectareRate = 0
  entryForm.comment = ''
}

const editEntry = (entry: any) => {
  editingEntry.value = entry
  entryForm.workDate = entry.workDate?.split('T')[0] || ''
  entryForm.project = entry.project || ''
  entryForm.task = entry.task || ''
  entryForm.fromTime = entry.fromTime || ''
  entryForm.toTime = entry.toTime || ''
  entryForm.pauseMinutes = entry.pauseMinutes || 0
  entryForm.travelMinutes = entry.travelMinutes || 0
  entryForm.areaName = entry.areaName || ''
  entryForm.areaCode = entry.areaCode || ''
  entryForm.hours = entry.hours || 0
  entryForm.km = entry.km || 0
  entryForm.pieces = entry.pieces || 0
  entryForm.trKind = entry.trKind || ''
  entryForm.extraNote = entry.extraNote || ''
  entryForm.boxCarryCount = entry.boxCarryCount || 0
  entryForm.hectares = entry.hectares || 0
  entryForm.hectareRate = entry.hectareRate || 0
  entryForm.comment = entry.comment || ''
  showEntryModal.value = true
}

const saveEntry = async () => {
  if (!selectedTimesheet.value) return
  
  // Validace data v rámci období výkazu
  const workDate = new Date(entryForm.workDate)
  const periodStart = new Date(selectedTimesheet.value.periodStart)
  const periodEnd = new Date(selectedTimesheet.value.periodEnd)
  
  if (workDate < periodStart || workDate > periodEnd) {
    alert(t('timesheets.dateOutOfRange'))
    return
  }
  
  savingEntry.value = true
  try {
    // Pomocná funkce pro konverzi času na správný formát
    const formatTime = (time: string) => {
      if (!time) return null
      // Přidat sekundy pokud chybí (08:00 -> 08:00:00)
      return time.length === 5 ? `${time}:00` : time
    }
    
    const payload = {
      workDate: entryForm.workDate,
      project: entryForm.project || null,
      task: entryForm.task || null,
      // Časové údaje - musí být ve formátu HH:mm:ss nebo null
      fromTime: formatTime(entryForm.fromTime),
      toTime: formatTime(entryForm.toTime),
      pauseMinutes: entryForm.pauseMinutes || 0,
      travelMinutes: entryForm.travelMinutes || 0,
      // Plocha
      areaName: entryForm.areaName || null,
      areaCode: entryForm.areaCode || null,
      // Práce
      hours: entryForm.hours,
      km: entryForm.km,
      pieces: entryForm.pieces,
      trKind: entryForm.trKind || null,
      extraNote: entryForm.extraNote || null,
      boxCarryCount: entryForm.boxCarryCount || 0,
      // Hektary
      hectares: entryForm.hectares || 0,
      hectareRate: entryForm.hectareRate || rates.hectareRate,
      // Sazby
      hourRate: rates.hourlyRate,
      kmRate: rates.kmRate,
      pieceRate: rates.pieceRate,
      // Komentář
      comment: entryForm.comment || null
    }
    
    if (editingEntry.value) {
      await api.put(`/api/timesheets/${selectedTimesheet.value.id}/entries/${editingEntry.value.id}`, payload)
    } else {
      await api.post(`/api/timesheets/${selectedTimesheet.value.id}/entries`, payload)
    }
    
    showEntryModal.value = false
    await openDetail(selectedTimesheet.value)
    await loadTimesheets()
  } catch (e: any) {
    alert(e.response?.data?.error || t('common.saveError'))
  } finally {
    savingEntry.value = false
  }
}

const deleteEntry = async (entry: any) => {
  if (!selectedTimesheet.value) return
  if (!confirm(t('common.confirmDelete') + '?')) return
  try {
    await api.delete(`/api/timesheets/${selectedTimesheet.value.id}/entries/${entry.id}`)
    await openDetail(selectedTimesheet.value)
    await loadTimesheets()
  } catch (e: any) {
    alert(e.response?.data?.error || t('common.deleteError'))
  }
}
</script>