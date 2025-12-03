<template>
  <div class="space-y-6">
    <!-- Header -->
    <div class="flex justify-between items-center">
      <h1 class="text-2xl font-bold text-sunskog-dark">{{ t('warehouse.title') }}</h1>
      <div class="flex space-x-3">
        <!-- Filtr kategorie -->
        <select
          v-if="authStore.canManageWarehouse && categories.length > 0"
          v-model="selectedCategoryId"
          class="px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sunskog-primary"
        >
          <option value="">{{ t('warehouse.allCategories') }}</option>
          <option v-for="cat in categories" :key="cat.id" :value="cat.id">
            {{ cat.name }} ({{ cat.itemCount }})
          </option>
        </select>
        
        <!-- Vyhledávání -->
        <div class="relative">
          <input
            v-model="searchQuery"
            type="text"
            :placeholder="t('common.search')"
            class="pl-10 pr-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sunskog-primary"
          />
          <svg class="w-5 h-5 absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
          </svg>
        </div>
        
        <!-- Přidat položku - pouze pro warehouse managery -->
        <button
          v-if="authStore.canManageWarehouse"
          @click="openItemModal()"
          class="bg-sunskog-primary hover:bg-sunskog-hover text-white px-4 py-2 rounded-lg flex items-center space-x-2 transition"
        >
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
          </svg>
          <span>{{ t('warehouse.addItem') }}</span>
        </button>
      </div>
    </div>

    <!-- Low stock alert - pouze pro warehouse managery -->
    <div v-if="authStore.canManageWarehouse && lowStockItems.length > 0" class="bg-orange-100 border-l-4 border-orange-500 p-4 rounded">
      <div class="flex items-center">
        <svg class="w-6 h-6 text-orange-500 mr-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
        </svg>
        <span class="text-orange-700 font-medium">{{ t('warehouse.lowStockAlert') }} {{ lowStockItems.length }} {{ t('warehouse.itemsBelowMin') }}</span>
      </div>
    </div>

    <!-- Tabs -->
    <div class="border-b border-gray-200">
      <nav class="flex space-x-8">
        <!-- Sklad tab - pouze pro warehouse/management/admin (NE pro TeamLead) -->
        <button
          v-if="authStore.canManageWarehouse"
          @click="activeTab = 'warehouse'"
          :class="[
            'py-2 px-1 border-b-2 font-medium text-sm transition',
            activeTab === 'warehouse' 
              ? 'border-sunskog-primary text-sunskog-primary' 
              : 'border-transparent text-gray-500 hover:text-gray-700'
          ]"
        >
          {{ t('warehouse.stock') }}
        </button>
        
        <!-- Přiděleno tab -->
        <button
          @click="activeTab = 'assigned'"
          :class="[
            'py-2 px-1 border-b-2 font-medium text-sm transition',
            activeTab === 'assigned' 
              ? 'border-sunskog-primary text-sunskog-primary' 
              : 'border-transparent text-gray-500 hover:text-gray-700'
          ]"
        >
          {{ isWorkerOnly ? t('warehouse.myAssigned') : (isTeamLeadOnly ? t('warehouse.teamAssigned') : t('warehouse.assignedToEmployees')) }}
        </button>
        
        <!-- Kategorie tab - pouze pro adminy -->
        <button
          v-if="authStore.isAdmin"
          @click="activeTab = 'categories'"
          :class="[
            'py-2 px-1 border-b-2 font-medium text-sm transition',
            activeTab === 'categories' 
              ? 'border-sunskog-primary text-sunskog-primary' 
              : 'border-transparent text-gray-500 hover:text-gray-700'
          ]"
        >
          {{ t('warehouse.categories') }}
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
      <button @click="loadAll" class="mt-4 text-sunskog-primary hover:underline">{{ t('common.tryAgain') }}</button>
    </div>

    <!-- ========== WAREHOUSE TAB ========== -->
    <div v-else-if="activeTab === 'warehouse'" class="bg-white rounded-lg shadow overflow-hidden">
      <table class="min-w-full divide-y divide-gray-200">
        <thead class="bg-gray-50">
          <tr>
            <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">{{ t('warehouse.name') }}</th>
            <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">{{ t('warehouse.category') }}</th>
            <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">{{ t('warehouse.size') }}</th>
            <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">{{ t('warehouse.code') }}</th>
            <th class="px-4 py-3 text-center text-xs font-medium text-gray-500 uppercase">{{ t('warehouse.quantity') }}</th>
            <th class="px-4 py-3 text-center text-xs font-medium text-gray-500 uppercase">{{ t('warehouse.qrCode') }}</th>
            <th class="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase">{{ t('common.actions') }}</th>
          </tr>
        </thead>
        <tbody class="bg-white divide-y divide-gray-200">
          <tr v-if="warehouseItems.length === 0">
            <td colspan="7" class="px-4 py-8 text-center text-gray-500">{{ t('warehouse.noItems') }}</td>
          </tr>
          <tr 
            v-for="item in warehouseItems" 
            :key="item.id"
            :class="{'bg-orange-50': isLowStock(item)}"
          >
            <td class="px-4 py-3">
              <div class="font-medium text-gray-900">{{ item.name }}</div>
              <div v-if="item.itemType" class="text-xs text-gray-500">{{ item.itemType }}</div>
              <div v-if="!item.isActive" class="text-xs text-red-500">{{ t('warehouse.inactive') }}</div>
            </td>
            <td class="px-4 py-3 text-sm text-gray-500">
              <span v-if="item.categoryName" class="px-2 py-1 bg-gray-100 rounded text-xs">
                {{ item.categoryName }}
              </span>
              <span v-else>-</span>
            </td>
            <td class="px-4 py-3 text-sm text-gray-500">
              <span v-if="item.size" class="px-2 py-1 bg-blue-100 text-blue-800 rounded text-xs font-medium">
                {{ item.size }}
              </span>
              <span v-else>-</span>
            </td>
            <td class="px-4 py-3 text-sm text-gray-500">{{ item.sku || item.serialNumber || '-' }}</td>
            <td class="px-4 py-3 text-center">
              <span :class="getStockClass(item)" class="font-semibold">
                {{ getStock(item.id) }}
              </span>
              <div v-if="item.minStock" class="text-xs text-gray-400">min: {{ item.minStock }}</div>
            </td>
            <td class="px-4 py-3 text-center">
              <button @click="showQr(item)" class="text-blue-600 hover:text-blue-800" :title="t('warehouse.showQR')">
                <svg class="w-6 h-6 inline" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v1m6 11h2m-6 0h-2v4m0-11v3m0 0h.01M12 12h4.01M16 20h4M4 12h4m12 0h.01M5 8h2a1 1 0 001-1V5a1 1 0 00-1-1H5a1 1 0 00-1 1v2a1 1 0 001 1zm12 0h2a1 1 0 001-1V5a1 1 0 00-1-1h-2a1 1 0 00-1 1v2a1 1 0 001 1zM5 20h2a1 1 0 001-1v-2a1 1 0 00-1-1H5a1 1 0 00-1 1v2a1 1 0 001 1z" />
                </svg>
              </button>
            </td>
            <td class="px-4 py-3 text-right space-x-1">
              <!-- Příjem/Výdej - pouze pro warehouse managery -->
              <template v-if="authStore.canManageWarehouse">
                <button @click="openMovementModal(item, 'in')" class="text-green-600 hover:text-green-800 px-2 py-1 text-sm" :title="t('warehouse.receive')">
                  +{{ t('warehouse.receive') }}
                </button>
                <button @click="openMovementModal(item, 'out')" class="text-red-600 hover:text-red-800 px-2 py-1 text-sm" :title="t('warehouse.issue')">
                  -{{ t('warehouse.issue') }}
                </button>
              </template>
              <!-- Přiřadit osobě -->
              <button 
                v-if="authStore.canManageWarehouse || authStore.isTeamLead"
                @click="openAssignModal(item, 'person')" 
                class="text-blue-600 hover:text-blue-800 px-2 py-1 text-sm"
              >
                {{ t('warehouse.assignPerson') }}
              </button>
              <!-- Přiřadit týmu -->
              <button 
                v-if="authStore.canManageWarehouse"
                @click="openAssignModal(item, 'team')" 
                class="text-purple-600 hover:text-purple-800 px-2 py-1 text-sm"
              >
                {{ t('warehouse.assignTeam') }}
              </button>
              <!-- Upravit/Smazat - pouze pro warehouse managery -->
              <template v-if="authStore.canManageWarehouse">
                <button @click="openItemModal(item)" class="text-gray-600 hover:text-gray-800 px-1">
                  <svg class="w-4 h-4 inline" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                  </svg>
                </button>
                <button @click="deleteItem(item)" class="text-red-600 hover:text-red-800 px-1">
                  <svg class="w-4 h-4 inline" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                  </svg>
                </button>
              </template>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- ========== ASSIGNED TAB ========== -->
    <div v-else-if="activeTab === 'assigned'" class="bg-white rounded-lg shadow overflow-hidden">
      <table class="min-w-full divide-y divide-gray-200">
        <thead class="bg-gray-50">
          <tr>
            <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">{{ t('warehouse.item') }}</th>
            <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">{{ t('warehouse.category') }}</th>
            <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">{{ t('warehouse.size') }}</th>
            <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">{{ t('warehouse.assignedTo') }}</th>
            <th class="px-4 py-3 text-center text-xs font-medium text-gray-500 uppercase">{{ t('warehouse.quantity') }}</th>
            <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">{{ t('warehouse.date') }}</th>
            <th class="px-4 py-3 text-center text-xs font-medium text-gray-500 uppercase">{{ t('warehouse.qrCode') }}</th>
            <th v-if="authStore.canManageWarehouse" class="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase">{{ t('common.actions') }}</th>
          </tr>
        </thead>
        <tbody class="bg-white divide-y divide-gray-200">
          <tr v-if="filteredAssignments.length === 0">
            <td :colspan="authStore.canManageWarehouse ? 8 : 7" class="px-4 py-8 text-center text-gray-500">{{ t('warehouse.noAssignments') }}</td>
          </tr>
          <tr v-for="assign in filteredAssignments" :key="assign.id">
            <td class="px-4 py-3">
              <div class="font-medium text-gray-900">{{ assign.itemName }}</div>
              <div v-if="assign.itemSku" class="text-xs text-gray-500">{{ assign.itemSku }}</div>
            </td>
            <td class="px-4 py-3 text-sm text-gray-500">
              <span v-if="assign.categoryName" class="px-2 py-1 bg-gray-100 rounded text-xs">
                {{ assign.categoryName }}
              </span>
              <span v-else>-</span>
            </td>
            <td class="px-4 py-3 text-sm">
              <span v-if="assign.size" class="px-2 py-1 bg-blue-100 text-blue-800 rounded text-xs font-medium">
                {{ assign.size }}
              </span>
              <span v-else>-</span>
            </td>
            <td class="px-4 py-3">
              <div class="flex items-center space-x-2">
                <!-- Ikona pro tým vs osobu -->
                <span v-if="assign.teamId" class="text-purple-600" :title="t('warehouse.team')">
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z" />
                  </svg>
                </span>
                <span v-else class="text-blue-600" :title="t('warehouse.person')">
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
                  </svg>
                </span>
                <span class="text-sm text-gray-900">{{ assign.teamName || assign.employeeName }}</span>
              </div>
            </td>
            <td class="px-4 py-3 text-center font-medium">{{ assign.quantity }}</td>
            <td class="px-4 py-3 text-sm text-gray-500">{{ formatDate(assign.assignedAt) }}</td>
            <td class="px-4 py-3 text-center">
              <button @click="showQrForAssignment(assign)" class="text-blue-600 hover:text-blue-800">
                <svg class="w-5 h-5 inline" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v1m6 11h2m-6 0h-2v4m0-11v3m0 0h.01M12 12h4.01M16 20h4M4 12h4m12 0h.01M5 8h2a1 1 0 001-1V5a1 1 0 00-1-1H5a1 1 0 00-1 1v2a1 1 0 001 1zm12 0h2a1 1 0 001-1V5a1 1 0 00-1-1h-2a1 1 0 00-1 1v2a1 1 0 001 1zM5 20h2a1 1 0 001-1v-2a1 1 0 00-1-1H5a1 1 0 00-1 1v2a1 1 0 001 1z" />
                </svg>
              </button>
            </td>
            <td v-if="authStore.canManageWarehouse" class="px-4 py-3 text-right">
              <button @click="returnToWarehouse(assign)" class="text-orange-600 hover:text-orange-800 text-sm">
                {{ t('warehouse.returnToStock') }}
              </button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- ========== CATEGORIES TAB ========== -->
    <div v-else-if="activeTab === 'categories'" class="bg-white rounded-lg shadow overflow-hidden">
      <div class="p-4 border-b flex justify-between items-center">
        <h3 class="font-medium text-gray-900">{{ t('warehouse.categoriesManagement') }}</h3>
        <button @click="openCategoryModal()" class="bg-sunskog-primary hover:bg-sunskog-hover text-white px-4 py-2 rounded-lg text-sm">
          + {{ t('warehouse.addCategory') }}
        </button>
      </div>
      <table class="min-w-full divide-y divide-gray-200">
        <thead class="bg-gray-50">
          <tr>
            <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">{{ t('warehouse.name') }}</th>
            <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">{{ t('warehouse.nameEn') }}</th>
            <th class="px-4 py-3 text-center text-xs font-medium text-gray-500 uppercase">{{ t('warehouse.hasSizes') }}</th>
            <th class="px-4 py-3 text-center text-xs font-medium text-gray-500 uppercase">{{ t('warehouse.hasTypes') }}</th>
            <th class="px-4 py-3 text-center text-xs font-medium text-gray-500 uppercase">{{ t('warehouse.itemCount') }}</th>
            <th class="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase">{{ t('common.actions') }}</th>
          </tr>
        </thead>
        <tbody class="bg-white divide-y divide-gray-200">
          <tr v-if="categories.length === 0">
            <td colspan="6" class="px-4 py-8 text-center text-gray-500">{{ t('warehouse.noCategories') }}</td>
          </tr>
          <tr v-for="cat in categories" :key="cat.id" :class="{'opacity-50': !cat.isActive}">
            <td class="px-4 py-3 font-medium text-gray-900">{{ cat.name }}</td>
            <td class="px-4 py-3 text-sm text-gray-500">{{ cat.nameEn || '-' }}</td>
            <td class="px-4 py-3 text-center">
              <span v-if="cat.hasSizes" class="text-green-600">✓</span>
              <span v-else class="text-gray-300">-</span>
            </td>
            <td class="px-4 py-3 text-center">
              <span v-if="cat.hasItemTypes" class="text-green-600">✓</span>
              <span v-else class="text-gray-300">-</span>
            </td>
            <td class="px-4 py-3 text-center">{{ cat.itemCount }}</td>
            <td class="px-4 py-3 text-right space-x-2">
              <button @click="openCategoryModal(cat)" class="text-gray-600 hover:text-gray-800">
                <svg class="w-4 h-4 inline" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                </svg>
              </button>
              <button @click="deleteCategory(cat)" class="text-red-600 hover:text-red-800" :disabled="cat.itemCount > 0">
                <svg class="w-4 h-4 inline" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                </svg>
              </button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- ========== MODALS ========== -->

    <!-- Item Modal -->
    <div v-if="showItemModal" class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
      <div class="bg-white rounded-lg shadow-xl w-full max-w-md p-6">
        <h2 class="text-xl font-bold mb-4">{{ editingItem ? t('warehouse.editItem') : t('warehouse.addItem') }}</h2>
        <form @submit.prevent="saveItem" class="space-y-4">
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('warehouse.name') }} *</label>
            <input v-model="itemForm.name" type="text" required class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sunskog-primary" />
          </div>
          
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('warehouse.category') }}</label>
            <select v-model="itemForm.categoryId" class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sunskog-primary">
              <option :value="null">-- {{ t('warehouse.noCategory') }} --</option>
              <option v-for="cat in categories" :key="cat.id" :value="cat.id">{{ cat.name }}</option>
            </select>
          </div>
          
          <!-- Velikost - zobrazit jen pokud má kategorie hasSizes -->
          <div v-if="selectedCategoryHasSizes">
            <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('warehouse.size') }}</label>
            <input v-model="itemForm.size" type="text" class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sunskog-primary" placeholder="S, M, L, XL nebo 8, 9, 10..." />
          </div>
          
          <!-- Druh/Typ - zobrazit jen pokud má kategorie hasItemTypes -->
          <div v-if="selectedCategoryHasItemTypes">
            <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('warehouse.itemType') }}</label>
            <input v-model="itemForm.itemType" type="text" class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sunskog-primary" placeholder="Sázení, Řezačské - Tegera..." />
          </div>
          
          <div class="grid grid-cols-2 gap-4">
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">SKU</label>
              <input v-model="itemForm.sku" type="text" class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sunskog-primary" />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('warehouse.serialNumber') }}</label>
              <input v-model="itemForm.serialNumber" type="text" class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sunskog-primary" />
            </div>
          </div>
          
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('warehouse.minStock') }}</label>
            <input v-model.number="itemForm.minStock" type="number" min="0" class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sunskog-primary" />
          </div>
          
          <div v-if="editingItem" class="flex items-center">
            <input v-model="itemForm.isActive" type="checkbox" id="isActive" class="mr-2" />
            <label for="isActive" class="text-sm text-gray-700">{{ t('warehouse.active') }}</label>
          </div>
          
          <div class="flex justify-end space-x-3 pt-4">
            <button type="button" @click="showItemModal = false" class="px-4 py-2 border rounded-lg hover:bg-gray-50">{{ t('common.cancel') }}</button>
            <button type="submit" :disabled="savingItem" class="px-4 py-2 bg-sunskog-primary text-white rounded-lg hover:bg-sunskog-hover disabled:opacity-50">
              {{ savingItem ? t('common.saving') : t('common.save') }}
            </button>
          </div>
        </form>
      </div>
    </div>

    <!-- Movement Modal (Příjem/Výdej) -->
    <div v-if="showMovementModal" class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
      <div class="bg-white rounded-lg shadow-xl w-full max-w-md p-6">
        <h2 class="text-xl font-bold mb-4">
          {{ movementType === 'in' ? t('warehouse.receiveItem') : t('warehouse.issueItem') }}: {{ movementItem?.name }}
        </h2>
        <form @submit.prevent="saveMovement" class="space-y-4">
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('warehouse.quantity') }} *</label>
            <input v-model.number="movementForm.quantity" type="number" min="1" required class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sunskog-primary" />
          </div>
          
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('warehouse.date') }}</label>
            <input v-model="movementForm.movementDate" type="date" class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sunskog-primary" />
          </div>
          
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('warehouse.note') }}</label>
            <input v-model="movementForm.note" type="text" class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sunskog-primary" />
          </div>
          
          <div class="flex justify-end space-x-3 pt-4">
            <button type="button" @click="showMovementModal = false" class="px-4 py-2 border rounded-lg hover:bg-gray-50">{{ t('common.cancel') }}</button>
            <button type="submit" :disabled="savingMovement" :class="movementType === 'in' ? 'bg-green-600 hover:bg-green-700' : 'bg-red-600 hover:bg-red-700'" class="px-4 py-2 text-white rounded-lg disabled:opacity-50">
              {{ savingMovement ? t('common.saving') : (movementType === 'in' ? t('warehouse.receive') : t('warehouse.issue')) }}
            </button>
          </div>
        </form>
      </div>
    </div>

    <!-- Assign Modal (Osobě nebo Týmu) -->
    <div v-if="showAssignModal" class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
      <div class="bg-white rounded-lg shadow-xl w-full max-w-md p-6">
        <h2 class="text-xl font-bold mb-4">
          {{ assignType === 'team' ? t('warehouse.assignToTeam') : t('warehouse.assignToPerson') }}: {{ assignItem?.name }}
        </h2>
        <form @submit.prevent="assignToTarget" class="space-y-4">
          <!-- Výběr osoby nebo týmu -->
          <div v-if="assignType === 'person'">
            <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('warehouse.employee') }} *</label>
            <select v-model="assignForm.employeeId" required class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sunskog-primary">
              <option value="">-- {{ t('warehouse.selectEmployee') }} --</option>
              <option v-for="emp in employees" :key="emp.id" :value="emp.id">
                {{ emp.name || emp.email }}
              </option>
            </select>
          </div>
          
          <div v-else>
            <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('warehouse.team') }} *</label>
            <select v-model="assignForm.teamId" required class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sunskog-primary">
              <option value="">-- {{ t('warehouse.selectTeam') }} --</option>
              <option v-for="team in teams" :key="team.id" :value="team.id">
                {{ team.name }}
              </option>
            </select>
          </div>
          
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('warehouse.quantity') }}</label>
            <input v-model.number="assignForm.quantity" type="number" min="1" class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sunskog-primary" />
          </div>
          
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('warehouse.note') }}</label>
            <input v-model="assignForm.note" type="text" class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sunskog-primary" />
          </div>
          
          <div class="flex justify-end space-x-3 pt-4">
            <button type="button" @click="showAssignModal = false" class="px-4 py-2 border rounded-lg hover:bg-gray-50">{{ t('common.cancel') }}</button>
            <button type="submit" :disabled="assigning" :class="assignType === 'team' ? 'bg-purple-600 hover:bg-purple-700' : 'bg-blue-600 hover:bg-blue-700'" class="px-4 py-2 text-white rounded-lg disabled:opacity-50">
              {{ assigning ? t('common.saving') : t('warehouse.assign') }}
            </button>
          </div>
        </form>
      </div>
    </div>

    <!-- Category Modal -->
    <div v-if="showCategoryModal" class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
      <div class="bg-white rounded-lg shadow-xl w-full max-w-md p-6">
        <h2 class="text-xl font-bold mb-4">{{ editingCategory ? t('warehouse.editCategory') : t('warehouse.addCategory') }}</h2>
        <form @submit.prevent="saveCategory" class="space-y-4">
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('warehouse.name') }} (CZ) *</label>
            <input v-model="categoryForm.name" type="text" required class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sunskog-primary" />
          </div>
          
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('warehouse.nameEn') }}</label>
            <input v-model="categoryForm.nameEn" type="text" class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sunskog-primary" />
          </div>
          
          <div class="flex items-center space-x-6">
            <div class="flex items-center">
              <input v-model="categoryForm.hasSizes" type="checkbox" id="hasSizes" class="mr-2" />
              <label for="hasSizes" class="text-sm text-gray-700">{{ t('warehouse.hasSizes') }}</label>
            </div>
            <div class="flex items-center">
              <input v-model="categoryForm.hasItemTypes" type="checkbox" id="hasItemTypes" class="mr-2" />
              <label for="hasItemTypes" class="text-sm text-gray-700">{{ t('warehouse.hasTypes') }}</label>
            </div>
          </div>
          
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('warehouse.sortOrder') }}</label>
            <input v-model.number="categoryForm.sortOrder" type="number" min="0" class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sunskog-primary" />
          </div>
          
          <div v-if="editingCategory" class="flex items-center">
            <input v-model="categoryForm.isActive" type="checkbox" id="catIsActive" class="mr-2" />
            <label for="catIsActive" class="text-sm text-gray-700">{{ t('warehouse.active') }}</label>
          </div>
          
          <div class="flex justify-end space-x-3 pt-4">
            <button type="button" @click="showCategoryModal = false" class="px-4 py-2 border rounded-lg hover:bg-gray-50">{{ t('common.cancel') }}</button>
            <button type="submit" :disabled="savingCategory" class="px-4 py-2 bg-sunskog-primary text-white rounded-lg hover:bg-sunskog-hover disabled:opacity-50">
              {{ savingCategory ? t('common.saving') : t('common.save') }}
            </button>
          </div>
        </form>
      </div>
    </div>

    <!-- QR Modal -->
    <div v-if="showQrModal" class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
      <div class="bg-white rounded-lg shadow-xl w-full max-w-sm p-6 text-center">
        <h2 class="text-xl font-bold mb-2">{{ qrItem?.name }}</h2>
        <p class="text-gray-500 text-sm mb-4">{{ qrItem?.sku || qrItem?.serialNumber || '-' }}</p>
        
        <div v-if="qrImageUrl" class="mb-4">
          <img :src="qrImageUrl" alt="QR Code" class="mx-auto w-48 h-48" />
        </div>
        <div v-else class="w-48 h-48 mx-auto bg-gray-100 flex items-center justify-center mb-4">
          <div class="animate-spin w-6 h-6 border-2 border-sunskog-primary border-t-transparent rounded-full"></div>
        </div>
        
        <div class="flex justify-center space-x-3">
          <button @click="printQr" class="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700">
            {{ t('warehouse.print') }}
          </button>
          <button @click="showQrModal = false" class="px-4 py-2 border rounded-lg hover:bg-gray-50">
            {{ t('common.close') }}
          </button>
        </div>
      </div>
    </div>

  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth'
import api from '@/services/api'

const { t, locale } = useI18n()
const authStore = useAuthStore()

// State
const loading = ref(false)
const error = ref<string | null>(null)
const activeTab = ref('warehouse')
const searchQuery = ref('')
const selectedCategoryId = ref('')

// Data
const items = ref<any[]>([])
const categories = ref<any[]>([])
const teams = ref<any[]>([])
const lowStockItems = ref<any[]>([])
const stockMap = ref<Record<string, number>>({})
const employees = ref<any[]>([])
const assignments = ref<any[]>([])

// Modals
const showItemModal = ref(false)
const showMovementModal = ref(false)
const showAssignModal = ref(false)
const showCategoryModal = ref(false)
const showQrModal = ref(false)

// Editing state
const editingItem = ref<any>(null)
const editingCategory = ref<any>(null)
const movementItem = ref<any>(null)
const movementType = ref<'in' | 'out'>('in')
const assignItem = ref<any>(null)
const assignType = ref<'person' | 'team'>('person')
const qrItem = ref<any>(null)
const qrImageUrl = ref<string | null>(null)
const qrUrl = ref('')

// Loading states
const savingItem = ref(false)
const savingMovement = ref(false)
const savingCategory = ref(false)
const assigning = ref(false)

// Forms
const itemForm = reactive({
  name: '',
  sku: '',
  serialNumber: '',
  minStock: 0,
  isActive: true,
  categoryId: null as string | null,
  size: '',
  itemType: ''
})

const movementForm = reactive({
  quantity: 1,
  note: '',
  movementDate: ''
})

const assignForm = reactive({
  employeeId: '',
  teamId: '',
  quantity: 1,
  note: ''
})

const categoryForm = reactive({
  name: '',
  nameEn: '',
  hasSizes: false,
  hasItemTypes: false,
  sortOrder: 0,
  isActive: true
})

// Computed
const isWorkerOnly = computed(() => authStore.isWorker && !authStore.canManageWarehouse && !authStore.isTeamLead)
const isTeamLeadOnly = computed(() => authStore.isTeamLead && !authStore.isAdmin && !authStore.isManagement && !authStore.canManageWarehouse)

const selectedCategoryHasSizes = computed(() => {
  if (!itemForm.categoryId) return false
  const cat = categories.value.find(c => c.id === itemForm.categoryId)
  return cat?.hasSizes || false
})

const selectedCategoryHasItemTypes = computed(() => {
  if (!itemForm.categoryId) return false
  const cat = categories.value.find(c => c.id === itemForm.categoryId)
  return cat?.hasItemTypes || false
})

const warehouseItems = computed(() => {
  let filtered = items.value.filter(i => i.isActive)
  
  // Filtr podle kategorie
  if (selectedCategoryId.value) {
    filtered = filtered.filter(i => i.categoryId === selectedCategoryId.value)
  }
  
  // Filtr podle textu
  if (searchQuery.value) {
    const q = searchQuery.value.toLowerCase()
    filtered = filtered.filter(i => 
      i.name.toLowerCase().includes(q) ||
      (i.sku && i.sku.toLowerCase().includes(q)) ||
      (i.size && i.size.toLowerCase().includes(q)) ||
      (i.itemType && i.itemType.toLowerCase().includes(q)) ||
      (i.categoryName && i.categoryName.toLowerCase().includes(q))
    )
  }
  return filtered
})

const filteredAssignments = computed(() => {
  return assignments.value
})

// Methods
const loadAll = async () => {
  loading.value = true
  error.value = null
  try {
    if (isWorkerOnly.value || isTeamLeadOnly.value) {
      await loadAssignments()
    } else {
      await Promise.all([
        loadCategories(),
        loadItems(),
        loadLowStock(),
        loadMovements(),
        loadEmployees(),
        loadTeams(),
        loadAssignments()
      ])
    }
    
    // Worker/TeamLead default tab
    if (isWorkerOnly.value || isTeamLeadOnly.value) {
      activeTab.value = 'assigned'
    }
  } catch (e: any) {
    console.error('loadAll error:', e)
    error.value = e.response?.data?.message || t('common.loadError')
  } finally {
    loading.value = false
  }
}

const loadCategories = async () => {
  try {
    const data = await api.get<any[]>('/api/categories')
    categories.value = data
  } catch { categories.value = [] }
}

const loadItems = async () => {
  let query = ''
  const params = []
  if (searchQuery.value) params.push(`q=${encodeURIComponent(searchQuery.value)}`)
  if (selectedCategoryId.value) params.push(`categoryId=${selectedCategoryId.value}`)
  if (params.length) query = '?' + params.join('&')
  
  const data = await api.get<any[]>(`/api/inventory/items${query}`)
  items.value = data
}

const loadLowStock = async () => {
  try {
    const data = await api.get<any[]>('/api/inventory/low-stock')
    lowStockItems.value = data
  } catch { lowStockItems.value = [] }
}

const loadMovements = async () => {
  try {
    const data = await api.get<any[]>('/api/inventory/movements')
    const stocks: Record<string, number> = {}
    data.forEach((m: any) => {
      if (!stocks[m.itemId]) stocks[m.itemId] = 0
      stocks[m.itemId] += m.quantity
    })
    stockMap.value = stocks
  } catch { stockMap.value = {} }
}

const loadEmployees = async () => {
  try {
    const data = await api.get<any[]>('/api/employees')
    employees.value = data
  } catch { employees.value = [] }
}

const loadTeams = async () => {
  try {
    const data = await api.get<any[]>('/api/teams')
    teams.value = data
  } catch { teams.value = [] }
}

const loadAssignments = async () => {
  try {
    let endpoint = '/api/assignments'
    if (isWorkerOnly.value) {
      endpoint = '/api/assignments/my'
    } else if (authStore.isTeamLead && !authStore.isAdmin && !authStore.isManagement) {
      endpoint = '/api/assignments/team'
    }
    
    const data = await api.get<any[]>(endpoint)
    assignments.value = data.map(a => ({
      id: a.id,
      itemId: a.itemId,
      itemName: a.itemName,
      itemSku: a.itemSKU,
      categoryName: a.categoryName,
      size: a.size,
      employeeId: a.employeeId,
      employeeName: a.employeeName,
      teamId: a.teamId,
      teamName: a.teamName,
      quantity: a.quantity || 1,
      note: a.note,
      assignedAt: a.assignedAt
    }))
  } catch (e) {
    console.error('Failed to load assignments:', e)
    assignments.value = []
  }
}

const getStock = (itemId: string) => stockMap.value[itemId] || 0

const getStockClass = (item: any) => {
  const stock = getStock(item.id)
  if (item.minStock && stock < item.minStock) return 'text-orange-600'
  if (stock <= 0) return 'text-red-600'
  return 'text-green-600'
}

const isLowStock = (item: any) => {
  return item.minStock && getStock(item.id) < item.minStock
}

const formatDate = (dateStr: string) => {
  if (!dateStr) return '-'
  const localeCode = locale.value === 'cs' ? 'cs-CZ' : 'en-US'
  return new Date(dateStr).toLocaleDateString(localeCode)
}

// ========== ITEM CRUD ==========
const openItemModal = (item?: any) => {
  editingItem.value = item || null
  if (item) {
    itemForm.name = item.name
    itemForm.sku = item.sku || ''
    itemForm.serialNumber = item.serialNumber || ''
    itemForm.minStock = item.minStock || 0
    itemForm.isActive = item.isActive
    itemForm.categoryId = item.categoryId || null
    itemForm.size = item.size || ''
    itemForm.itemType = item.itemType || ''
  } else {
    itemForm.name = ''
    itemForm.sku = ''
    itemForm.serialNumber = ''
    itemForm.minStock = 0
    itemForm.isActive = true
    itemForm.categoryId = null
    itemForm.size = ''
    itemForm.itemType = ''
  }
  showItemModal.value = true
}

const saveItem = async () => {
  savingItem.value = true
  try {
    const payload = {
      name: itemForm.name,
      sku: itemForm.sku || null,
      serialNumber: itemForm.serialNumber || null,
      minStock: itemForm.minStock,
      isActive: itemForm.isActive,
      categoryId: itemForm.categoryId || null,
      size: itemForm.size || null,
      itemType: itemForm.itemType || null
    }
    if (editingItem.value) {
      await api.put(`/api/inventory/items/${editingItem.value.id}`, payload)
    } else {
      await api.post('/api/inventory/items', payload)
    }
    showItemModal.value = false
    await loadAll()
  } catch (e: any) {
    alert(e.response?.data?.message || t('common.saveError'))
  } finally {
    savingItem.value = false
  }
}

const deleteItem = async (item: any) => {
  if (!confirm(`${t('common.confirmDelete')} "${item.name}"?`)) return
  try {
    await api.delete(`/api/inventory/items/${item.id}`)
    await loadAll()
  } catch (e: any) {
    alert(e.response?.data?.message || t('common.deleteError'))
  }
}

// ========== MOVEMENTS ==========
const openMovementModal = (item: any, type: 'in' | 'out') => {
  movementItem.value = item
  movementType.value = type
  movementForm.quantity = 1
  movementForm.note = ''
  movementForm.movementDate = new Date().toISOString().split('T')[0]
  showMovementModal.value = true
}

const saveMovement = async () => {
  if (!movementItem.value) return
  savingMovement.value = true
  try {
    const quantity = movementType.value === 'in' ? movementForm.quantity : -movementForm.quantity
    await api.post('/api/inventory/movements', {
      itemId: movementItem.value.id,
      quantity: quantity,
      note: movementForm.note || null,
      movementDate: movementForm.movementDate ? new Date(movementForm.movementDate).toISOString() : null
    })
    showMovementModal.value = false
    await loadAll()
  } catch (e: any) {
    alert(e.response?.data?.message || t('common.saveError'))
  } finally {
    savingMovement.value = false
  }
}

// ========== ASSIGN ==========
const openAssignModal = (item: any, type: 'person' | 'team') => {
  assignItem.value = item
  assignType.value = type
  assignForm.employeeId = ''
  assignForm.teamId = ''
  assignForm.quantity = 1
  assignForm.note = ''
  showAssignModal.value = true
}

const assignToTarget = async () => {
  if (!assignItem.value) return
  if (assignType.value === 'person' && !assignForm.employeeId) return
  if (assignType.value === 'team' && !assignForm.teamId) return
  
  assigning.value = true
  try {
    const targetName = assignType.value === 'person'
      ? employees.value.find(e => e.id === assignForm.employeeId)?.name
      : teams.value.find(t => t.id === assignForm.teamId)?.name
    
    // Odečíst ze skladu
    await api.post('/api/inventory/movements', {
      itemId: assignItem.value.id,
      quantity: -assignForm.quantity,
      note: `${t('warehouse.assignedTo')}: ${targetName} - ${assignForm.note || ''}`
    })
    
    // Vytvořit přiřazení
    await api.post('/api/assignments', {
      itemId: assignItem.value.id,
      employeeId: assignType.value === 'person' ? assignForm.employeeId : null,
      teamId: assignType.value === 'team' ? assignForm.teamId : null,
      quantity: assignForm.quantity,
      note: assignForm.note
    })
    
    showAssignModal.value = false
    await loadAll()
  } catch (e: any) {
    alert(e.response?.data?.message || e.response?.data?.error || t('common.saveError'))
  } finally {
    assigning.value = false
  }
}

const returnToWarehouse = async (assignment: any) => {
  if (!confirm(`${t('warehouse.confirmReturn')} "${assignment.itemName}"?`)) return
  try {
    await api.post('/api/inventory/movements', {
      itemId: assignment.itemId,
      quantity: assignment.quantity || 1,
      note: `${t('warehouse.returnedFrom')}: ${assignment.teamName || assignment.employeeName}`
    })
    
    await api.delete(`/api/assignments/${assignment.id}`)
    await loadAll()
  } catch (e: any) {
    alert(e.response?.data?.message || t('common.saveError'))
  }
}

// ========== CATEGORIES ==========
const openCategoryModal = (cat?: any) => {
  editingCategory.value = cat || null
  if (cat) {
    categoryForm.name = cat.name
    categoryForm.nameEn = cat.nameEn || ''
    categoryForm.hasSizes = cat.hasSizes
    categoryForm.hasItemTypes = cat.hasItemTypes
    categoryForm.sortOrder = cat.sortOrder
    categoryForm.isActive = cat.isActive
  } else {
    categoryForm.name = ''
    categoryForm.nameEn = ''
    categoryForm.hasSizes = false
    categoryForm.hasItemTypes = false
    categoryForm.sortOrder = 0
    categoryForm.isActive = true
  }
  showCategoryModal.value = true
}

const saveCategory = async () => {
  savingCategory.value = true
  try {
    const payload = {
      name: categoryForm.name,
      nameEn: categoryForm.nameEn || null,
      hasSizes: categoryForm.hasSizes,
      hasItemTypes: categoryForm.hasItemTypes,
      sortOrder: categoryForm.sortOrder,
      isActive: categoryForm.isActive
    }
    if (editingCategory.value) {
      await api.put(`/api/categories/${editingCategory.value.id}`, payload)
    } else {
      await api.post('/api/categories', payload)
    }
    showCategoryModal.value = false
    await loadCategories()
  } catch (e: any) {
    alert(e.response?.data?.message || t('common.saveError'))
  } finally {
    savingCategory.value = false
  }
}

const deleteCategory = async (cat: any) => {
  if (cat.itemCount > 0) {
    alert(t('warehouse.cannotDeleteCategoryWithItems'))
    return
  }
  if (!confirm(`${t('common.confirmDelete')} "${cat.name}"?`)) return
  try {
    await api.delete(`/api/categories/${cat.id}`)
    await loadCategories()
  } catch (e: any) {
    alert(e.response?.data?.message || t('common.deleteError'))
  }
}

// ========== QR ==========
const showQr = async (item: any) => {
  qrItem.value = item
  qrImageUrl.value = null
  showQrModal.value = true
  
  const baseUrl = window.location.origin
  qrUrl.value = `${baseUrl}/app/warehouse?item=${item.id}`
  
  try {
    const response = await fetch(`${import.meta.env.VITE_API_URL || ''}/api/inventory/${item.id}/qrcode`, {
      headers: { 'Authorization': `Bearer ${localStorage.getItem('token')}` }
    })
    if (response.ok) {
      const blob = await response.blob()
      qrImageUrl.value = URL.createObjectURL(blob)
    } else {
      qrImageUrl.value = `https://api.qrserver.com/v1/create-qr-code/?size=200x200&data=${encodeURIComponent(qrUrl.value)}`
    }
  } catch {
    qrImageUrl.value = `https://api.qrserver.com/v1/create-qr-code/?size=200x200&data=${encodeURIComponent(qrUrl.value)}`
  }
}

const showQrForAssignment = (assignment: any) => {
  const item = items.value.find(i => i.id === assignment.itemId)
  if (item) {
    showQr(item)
  } else {
    // Pokud nemáme item v paměti, vytvořme mock
    showQr({ id: assignment.itemId, name: assignment.itemName, sku: assignment.itemSku })
  }
}

const printQr = () => {
  const printWindow = window.open('', '_blank')
  if (printWindow && qrItem.value) {
    printWindow.document.write(`
      <html>
        <head><title>QR - ${qrItem.value.name}</title></head>
        <body style="text-align:center;padding:20px;">
          <h2>${qrItem.value.name}</h2>
          <p>SKU: ${qrItem.value.sku || '-'}</p>
          <img src="${qrImageUrl.value}" style="width:200px;height:200px;" />
          <p style="font-size:10px;">${qrUrl.value}</p>
        </body>
      </html>
    `)
    printWindow.document.close()
    printWindow.print()
  }
}

// Search debounce
let searchTimeout: ReturnType<typeof setTimeout> | null = null
watch(searchQuery, () => {
  if (searchTimeout) clearTimeout(searchTimeout)
  searchTimeout = setTimeout(() => {
    if (!isWorkerOnly.value && !isTeamLeadOnly.value) loadItems()
  }, 300)
})

watch(selectedCategoryId, () => {
  if (!isWorkerOnly.value && !isTeamLeadOnly.value) loadItems()
})

// Lifecycle
onMounted(() => {
  loadAll()
})
</script>