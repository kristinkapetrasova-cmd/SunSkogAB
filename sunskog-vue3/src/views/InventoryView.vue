<template>
  <div class="space-y-6">
    <!-- Header -->
    <div class="flex justify-between items-center">
      <h1 class="text-2xl font-bold text-sunskog-dark">{{ t('inventory.title') }}</h1>
      <button
        @click="openItemModal(null)"
        class="bg-sunskog-primary hover:bg-sunskog-hover text-white px-4 py-2 rounded-lg flex items-center space-x-2 transition"
      >
        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
        </svg>
        <span>{{ t('inventory.addItem') }}</span>
      </button>
    </div>

    <!-- Low stock alert -->
    <div v-if="lowStockItems.length > 0" class="bg-orange-50 border border-orange-200 rounded-lg p-4">
      <div class="flex items-center space-x-3">
        <svg class="w-6 h-6 text-orange-500 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
        </svg>
        <div>
          <p class="font-semibold text-orange-800">{{ t('inventory.lowStockAlert') }}</p>
          <p class="text-sm text-orange-600">
            {{ lowStockItems.length }} {{ t('inventory.itemsBelowMin') }}: 
            <span v-for="(item, idx) in lowStockItems.slice(0, 3)" :key="item.id">
              {{ item.name }} ({{ item.current }}/{{ item.min }})<span v-if="idx < Math.min(lowStockItems.length, 3) - 1">, </span>
            </span>
            <span v-if="lowStockItems.length > 3">...</span>
          </p>
        </div>
      </div>
    </div>

    <!-- Tabs -->
    <div class="bg-white rounded-lg shadow">
      <div class="border-b border-gray-200">
        <nav class="flex -mb-px">
          <button
            @click="activeTab = 'warehouse'"
            :class="[
              'px-6 py-3 text-sm font-medium border-b-2 transition',
              activeTab === 'warehouse' 
                ? 'border-sunskog-primary text-sunskog-primary' 
                : 'border-transparent text-gray-500 hover:text-gray-700'
            ]"
          >
            {{ t('inventory.inWarehouse') }}
            <span class="ml-2 px-2 py-0.5 text-xs rounded-full bg-gray-100">{{ warehouseItems.length }}</span>
          </button>
          <button
            @click="activeTab = 'assigned'"
            :class="[
              'px-6 py-3 text-sm font-medium border-b-2 transition',
              activeTab === 'assigned' 
                ? 'border-sunskog-primary text-sunskog-primary' 
                : 'border-transparent text-gray-500 hover:text-gray-700'
            ]"
          >
            {{ t('inventory.assignedToEmployees') }}
            <span class="ml-2 px-2 py-0.5 text-xs rounded-full bg-gray-100">{{ assignments.length }}</span>
          </button>
        </nav>
      </div>

      <!-- Search -->
      <div class="p-4 border-b border-gray-200">
        <div class="flex space-x-4">
          <div class="flex-1 relative">
            <input
              v-model="searchQuery"
              type="text"
              :placeholder="t('common.search') + '...'"
              class="w-full pl-10 pr-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sunskog-primary focus:border-transparent"
              @input="debouncedSearch"
            />
            <svg class="w-5 h-5 text-gray-400 absolute left-3 top-2.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
            </svg>
          </div>
          <button
            @click="loadAll"
            class="px-4 py-2 text-gray-600 hover:text-gray-800 border border-gray-300 rounded-lg"
          >
            {{ t('common.refresh') }}
          </button>
        </div>
      </div>

      <!-- Loading -->
      <div v-if="loading" class="p-8 text-center">
        <div class="animate-spin w-8 h-8 border-4 border-sunskog-primary border-t-transparent rounded-full mx-auto"></div>
        <p class="mt-4 text-gray-600">{{ t('common.loading') }}</p>
      </div>

      <!-- Error -->
      <div v-else-if="error" class="p-6 text-center">
        <p class="text-red-600">{{ error }}</p>
        <button @click="loadAll" class="mt-4 text-sunskog-primary hover:underline">
          {{ t('common.tryAgain') }}
        </button>
      </div>

      <!-- Tab: Warehouse -->
      <div v-else-if="activeTab === 'warehouse'" class="overflow-x-auto">
        <table class="min-w-full divide-y divide-gray-200">
          <thead class="bg-gray-50">
            <tr>
              <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">{{ t('inventory.name') }}</th>
              <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">{{ t('inventory.code') }} / SKU</th>
              <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">{{ t('inventory.serialNumber') }}</th>
              <th class="px-6 py-3 text-center text-xs font-medium text-gray-500 uppercase">{{ t('inventory.quantity') }}</th>
              <th class="px-6 py-3 text-center text-xs font-medium text-gray-500 uppercase">{{ t('inventory.qrCode') }}</th>
              <th class="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase">{{ t('common.actions') }}</th>
            </tr>
          </thead>
          <tbody class="bg-white divide-y divide-gray-200">
            <tr v-if="warehouseItems.length === 0">
              <td colspan="6" class="px-6 py-8 text-center text-gray-500">
                {{ t('inventory.noItems') }}
              </td>
            </tr>
            <tr 
              v-for="item in warehouseItems" 
              :key="item.id"
              class="hover:bg-gray-50"
              :class="{ 'bg-orange-50': isLowStock(item) }"
            >
              <td class="px-6 py-4">
                <div class="text-sm font-medium text-gray-900">{{ item.name }}</div>
                <div v-if="!item.isActive" class="text-xs text-red-500">{{ t('inventory.inactive') }}</div>
              </td>
              <td class="px-6 py-4 text-sm text-gray-900">{{ item.sku || '-' }}</td>
              <td class="px-6 py-4 text-sm text-gray-900">{{ item.serialNumber || '-' }}</td>
              <td class="px-6 py-4 text-center">
                <span 
                  :class="[
                    'px-2 py-1 text-sm font-medium rounded-full',
                    isLowStock(item) ? 'bg-orange-100 text-orange-800' : 'bg-green-100 text-green-800'
                  ]"
                >
                  {{ getItemStock(item.id) }}
                </span>
                <div v-if="item.minStock" class="text-xs text-gray-500 mt-1">min: {{ item.minStock }}</div>
              </td>
              <td class="px-6 py-4 text-center">
                <button @click="showQrCode(item)" class="text-sunskog-primary hover:text-sunskog-hover" :title="t('inventory.showQR')">
                  <svg class="w-6 h-6 mx-auto" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v1m6 11h2m-6 0h-2v4m0-11v3m0 0h.01M12 12h4.01M16 20h4M4 12h4m12 0h.01M5 8h2a1 1 0 001-1V5a1 1 0 00-1-1H5a1 1 0 00-1 1v2a1 1 0 001 1zm12 0h2a1 1 0 001-1V5a1 1 0 00-1-1h-2a1 1 0 00-1 1v2a1 1 0 001 1zM5 20h2a1 1 0 001-1v-2a1 1 0 00-1-1H5a1 1 0 00-1 1v2a1 1 0 001 1z" />
                  </svg>
                </button>
              </td>
              <td class="px-6 py-4 text-right">
                <div class="flex items-center justify-end space-x-2">
                  <button
                    @click="openMovementModal(item, 'in')"
                    class="px-2 py-1 text-xs font-medium text-green-700 bg-green-100 hover:bg-green-200 rounded"
                    :title="t('inventory.receive')"
                  >
                    +{{ t('inventory.receive') }}
                  </button>
                  <button
                    @click="openMovementModal(item, 'out')"
                    class="px-2 py-1 text-xs font-medium text-orange-700 bg-orange-100 hover:bg-orange-200 rounded"
                    :title="t('inventory.issue')"
                  >
                    -{{ t('inventory.issue') }}
                  </button>
                  <button
                    @click="openAssignModal(item)"
                    class="px-2 py-1 text-xs font-medium text-blue-700 bg-blue-100 hover:bg-blue-200 rounded"
                    :title="t('inventory.assign')"
                  >
                    {{ t('inventory.assign') }}
                  </button>
                  <button @click="openItemModal(item)" class="text-gray-600 hover:text-gray-800" :title="t('common.edit')">
                    <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                    </svg>
                  </button>
                  <button @click="deleteItem(item)" class="text-red-600 hover:text-red-800" :title="t('common.delete')">
                    <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                    </svg>
                  </button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Tab: Assigned -->
      <div v-else-if="activeTab === 'assigned'" class="overflow-x-auto">
        <table class="min-w-full divide-y divide-gray-200">
          <thead class="bg-gray-50">
            <tr>
              <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">{{ t('inventory.name') }}</th>
              <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">{{ t('inventory.code') }} / SKU</th>
              <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">{{ t('inventory.assignedTo') }}</th>
              <th class="px-6 py-3 text-center text-xs font-medium text-gray-500 uppercase">{{ t('inventory.quantity') }}</th>
              <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">{{ t('inventory.assignedDate') }}</th>
              <th class="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase">{{ t('common.actions') }}</th>
            </tr>
          </thead>
          <tbody class="bg-white divide-y divide-gray-200">
            <tr v-if="assignments.length === 0">
              <td colspan="6" class="px-6 py-8 text-center text-gray-500">
                {{ t('inventory.noAssignedItems') }}
              </td>
            </tr>
            <tr v-for="a in assignments" :key="a.id" class="hover:bg-gray-50">
              <td class="px-6 py-4">
                <div class="text-sm font-medium text-gray-900">{{ a.itemName }}</div>
              </td>
              <td class="px-6 py-4 text-sm text-gray-900">{{ a.itemSku || '-' }}</td>
              <td class="px-6 py-4">
                <div class="flex items-center">
                  <div class="w-8 h-8 bg-sunskog-primary text-white rounded-full flex items-center justify-center text-sm font-medium mr-3">
                    {{ getInitials(a.employeeName) }}
                  </div>
                  <span class="text-sm text-gray-900">{{ a.employeeName }}</span>
                </div>
              </td>
              <td class="px-6 py-4 text-center text-sm text-gray-900">{{ a.quantity }}</td>
              <td class="px-6 py-4 text-sm text-gray-500">{{ formatDate(a.assignedAt) }}</td>
              <td class="px-6 py-4 text-right">
                <button
                  @click="returnToWarehouse(a)"
                  class="px-3 py-1 text-xs font-medium text-gray-700 bg-gray-100 hover:bg-gray-200 rounded"
                >
                  {{ t('inventory.returnToWarehouse') }}
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- Modal: Přidat/Upravit položku -->
    <div v-if="showItemModal" class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
      <div class="bg-white rounded-lg shadow-xl w-full max-w-lg p-6">
        <h2 class="text-xl font-bold text-sunskog-dark mb-4">
          {{ editingItem ? t('inventory.editItem') : t('inventory.addItem') }}
        </h2>
        
        <form @submit.prevent="saveItem" class="space-y-4">
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('inventory.name') }} *</label>
            <input v-model="itemForm.name" type="text" required class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sunskog-primary" :placeholder="t('inventory.namePlaceholder')" />
          </div>

          <div class="grid grid-cols-2 gap-4">
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">SKU / {{ t('inventory.code') }}</label>
              <input v-model="itemForm.sku" type="text" class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sunskog-primary" placeholder="INV-001" />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('inventory.serialNumber') }}</label>
              <input v-model="itemForm.serialNumber" type="text" class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sunskog-primary" placeholder="SN-12345" />
            </div>
          </div>

          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('inventory.minStock') }}</label>
            <input v-model.number="itemForm.minStock" type="number" min="0" class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sunskog-primary" />
          </div>

          <div class="flex items-center">
            <input v-model="itemForm.isActive" type="checkbox" id="isActive" class="h-4 w-4 text-sunskog-primary focus:ring-sunskog-primary border-gray-300 rounded" />
            <label for="isActive" class="ml-2 text-sm text-gray-700">{{ t('inventory.activeItem') }}</label>
          </div>

          <div class="flex justify-end space-x-3 pt-4">
            <button type="button" @click="showItemModal = false" class="px-4 py-2 text-gray-600 hover:text-gray-800">{{ t('common.cancel') }}</button>
            <button type="submit" :disabled="savingItem" class="px-4 py-2 bg-sunskog-primary hover:bg-sunskog-hover text-white rounded-lg disabled:opacity-50">
              {{ savingItem ? t('common.loading') : t('common.save') }}
            </button>
          </div>
        </form>
      </div>
    </div>

    <!-- Modal: Příjem/Výdej -->
    <div v-if="showMovementModal" class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
      <div class="bg-white rounded-lg shadow-xl w-full max-w-md p-6">
        <h2 class="text-xl font-bold text-sunskog-dark mb-4">
          {{ movementType === 'in' ? t('inventory.receiveToStock') : t('inventory.issueFromStock') }}
        </h2>
        <p class="text-gray-600 mb-4">{{ t('inventory.item') }}: <strong>{{ movementItem?.name }}</strong></p>
        
        <form @submit.prevent="saveMovement" class="space-y-4">
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('inventory.quantity') }} *</label>
            <input v-model.number="movementForm.quantity" type="number" min="1" required class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sunskog-primary" />
          </div>

          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('inventory.note') }}</label>
            <textarea v-model="movementForm.note" rows="2" class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sunskog-primary" :placeholder="t('inventory.notePlaceholder')"></textarea>
          </div>

          <div class="flex justify-end space-x-3 pt-4">
            <button type="button" @click="showMovementModal = false" class="px-4 py-2 text-gray-600 hover:text-gray-800">{{ t('common.cancel') }}</button>
            <button type="submit" :disabled="savingMovement" :class="movementType === 'in' ? 'bg-green-600 hover:bg-green-700' : 'bg-orange-500 hover:bg-orange-600'" class="px-4 py-2 text-white rounded-lg disabled:opacity-50">
              {{ savingMovement ? t('common.loading') : (movementType === 'in' ? t('inventory.receive') : t('inventory.issue')) }}
            </button>
          </div>
        </form>
      </div>
    </div>

    <!-- Modal: Přiřadit zaměstnanci -->
    <div v-if="showAssignModal" class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
      <div class="bg-white rounded-lg shadow-xl w-full max-w-md p-6">
        <h2 class="text-xl font-bold text-sunskog-dark mb-4">{{ t('inventory.assignToEmployee') }}</h2>
        <p class="text-gray-600 mb-4">{{ t('inventory.item') }}: <strong>{{ assignItem?.name }}</strong></p>
        <p class="text-sm text-gray-500 mb-4">{{ t('inventory.available') }}: {{ getItemStock(assignItem?.id) }}</p>
        
        <form @submit.prevent="assignToEmployee" class="space-y-4">
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('inventory.selectEmployee') }} *</label>
            <select v-model="assignForm.employeeId" required class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sunskog-primary">
              <option value="">-- {{ t('common.select') }} --</option>
              <option v-for="emp in employees" :key="emp.id" :value="emp.id">{{ emp.name || emp.email }}</option>
            </select>
          </div>

          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('inventory.quantity') }} *</label>
            <input v-model.number="assignForm.quantity" type="number" min="1" :max="getItemStock(assignItem?.id)" required class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sunskog-primary" />
          </div>

          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('inventory.note') }}</label>
            <textarea v-model="assignForm.note" rows="2" class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sunskog-primary" :placeholder="t('inventory.notePlaceholder')"></textarea>
          </div>

          <div class="flex justify-end space-x-3 pt-4">
            <button type="button" @click="showAssignModal = false" class="px-4 py-2 text-gray-600 hover:text-gray-800">{{ t('common.cancel') }}</button>
            <button type="submit" :disabled="assigning" class="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg disabled:opacity-50">
              {{ assigning ? t('common.loading') : t('inventory.assign') }}
            </button>
          </div>
        </form>
      </div>
    </div>

    <!-- Modal: QR kód -->
    <div v-if="showQrModal" class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
      <div class="bg-white rounded-lg shadow-xl w-full max-w-sm p-6 text-center">
        <h2 class="text-xl font-bold text-sunskog-dark mb-2">{{ t('inventory.qrCode') }}</h2>
        <p class="text-gray-600 mb-4">{{ qrItem?.name }}</p>
        
        <div class="bg-white p-4 rounded-lg border inline-block mb-4">
          <img v-if="qrImageUrl" :src="qrImageUrl" :alt="'QR ' + qrItem?.name" class="mx-auto" style="max-width: 200px" />
          <div v-else class="animate-pulse bg-gray-200 w-48 h-48 mx-auto"></div>
        </div>

        <div class="flex justify-center space-x-3">
          <button v-if="qrImageUrl" @click="printQrCode" class="px-4 py-2 bg-sunskog-primary hover:bg-sunskog-hover text-white rounded-lg flex items-center space-x-2">
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 17h2a2 2 0 002-2v-4a2 2 0 00-2-2H5a2 2 0 00-2 2v4a2 2 0 002 2h2m2 4h6a2 2 0 002-2v-4a2 2 0 00-2-2H9a2 2 0 00-2 2v4a2 2 0 002 2zm8-12V5a2 2 0 00-2-2H9a2 2 0 00-2 2v4h10z" />
            </svg>
            <span>{{ t('common.print') }}</span>
          </button>
          <button @click="showQrModal = false; qrImageUrl = null" class="px-4 py-2 text-gray-600 hover:text-gray-800 border border-gray-300 rounded-lg">
            {{ t('common.close') }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import api from '@/services/api'

const { t } = useI18n()

// State
const loading = ref(true)
const error = ref<string | null>(null)
const items = ref<any[]>([])
const lowStockItems = ref<any[]>([])
const stockMap = ref<Record<string, number>>({})
const employees = ref<any[]>([])
const assignments = ref<any[]>([])
const searchQuery = ref('')
const activeTab = ref<'warehouse' | 'assigned'>('warehouse')

// Modals
const showItemModal = ref(false)
const showMovementModal = ref(false)
const showAssignModal = ref(false)
const showQrModal = ref(false)
const editingItem = ref<any | null>(null)
const movementItem = ref<any | null>(null)
const movementType = ref<'in' | 'out'>('in')
const assignItem = ref<any | null>(null)
const qrItem = ref<any | null>(null)
const qrImageUrl = ref<string | null>(null)

// Loading states
const savingItem = ref(false)
const savingMovement = ref(false)
const assigning = ref(false)

// Forms
const itemForm = reactive({ name: '', sku: '', serialNumber: '', minStock: 0, isActive: true })
const movementForm = reactive({ quantity: 1, note: '' })
const assignForm = reactive({ employeeId: '', quantity: 1, note: '' })

// Computed
const warehouseItems = computed(() => items.value.filter(i => i.isActive))

// Debounce
let searchTimeout: ReturnType<typeof setTimeout> | null = null

// Methods
const loadAll = async () => {
  loading.value = true
  error.value = null
  try {
    await Promise.all([loadItems(), loadLowStock(), loadMovements(), loadEmployees(), loadAssignments()])
  } catch (e: any) {
    error.value = e.response?.data?.message || t('common.loadError')
  } finally {
    loading.value = false
  }
}

const loadItems = async () => {
  const query = searchQuery.value ? `?q=${encodeURIComponent(searchQuery.value)}` : ''
  const data = await api.get<any[]>(`/api/inventory/items${query}`)
  items.value = data
}

const loadLowStock = async () => {
  try {
    const data = await api.get<any[]>('/api/inventory/low-stock')
    lowStockItems.value = data
  } catch (e) { /* ignore */ }
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
  } catch (e) { /* ignore */ }
}

const loadEmployees = async () => {
  try {
    const data = await api.get<any[]>('/api/users')
    employees.value = data
  } catch (e) { /* ignore */ }
}

const loadAssignments = async () => {
  // Načítáme přiřazení z localStorage (frontend-only řešení)
  // V produkci by měl být backend endpoint
  const saved = localStorage.getItem('inventory_assignments')
  if (saved) {
    assignments.value = JSON.parse(saved)
  }
}

const saveAssignments = () => {
  localStorage.setItem('inventory_assignments', JSON.stringify(assignments.value))
}

const debouncedSearch = () => {
  if (searchTimeout) clearTimeout(searchTimeout)
  searchTimeout = setTimeout(() => loadItems(), 300)
}

const isLowStock = (item: any) => lowStockItems.value.some(l => l.id === item.id)

const getItemStock = (itemId: string | undefined) => {
  if (!itemId) return 0
  return stockMap.value[itemId] ?? 0
}

const getInitials = (name: string) => {
  if (!name) return '?'
  return name.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2)
}

const formatDate = (dateStr: string) => {
  if (!dateStr) return '-'
  return new Date(dateStr).toLocaleDateString()
}

const openItemModal = (item: any | null) => {
  editingItem.value = item
  if (item) {
    itemForm.name = item.name
    itemForm.sku = item.sku || ''
    itemForm.serialNumber = item.serialNumber || ''
    itemForm.minStock = item.minStock || 0
    itemForm.isActive = item.isActive
  } else {
    itemForm.name = ''
    itemForm.sku = ''
    itemForm.serialNumber = ''
    itemForm.minStock = 0
    itemForm.isActive = true
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
      isActive: itemForm.isActive
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

const openMovementModal = (item: any, type: 'in' | 'out') => {
  movementItem.value = item
  movementType.value = type
  movementForm.quantity = 1
  movementForm.note = ''
  showMovementModal.value = true
}

const saveMovement = async () => {
  if (!movementItem.value) return
  savingMovement.value = true
  try {
    // Pro výdej posíláme záporné číslo
    const quantity = movementType.value === 'in' ? movementForm.quantity : -movementForm.quantity
    
    await api.post('/api/inventory/movements', {
      itemId: movementItem.value.id,
      quantity: quantity,
      note: movementForm.note || null
    })
    showMovementModal.value = false
    await loadAll()
  } catch (e: any) {
    alert(e.response?.data?.message || t('common.saveError'))
  } finally {
    savingMovement.value = false
  }
}

const openAssignModal = (item: any) => {
  assignItem.value = item
  assignForm.employeeId = ''
  assignForm.quantity = 1
  assignForm.note = ''
  showAssignModal.value = true
}

const assignToEmployee = async () => {
  if (!assignItem.value || !assignForm.employeeId) return
  assigning.value = true
  try {
    const employee = employees.value.find(e => e.id === assignForm.employeeId)
    
    // 1. Odečíst ze skladu (movement se záporným množstvím)
    await api.post('/api/inventory/movements', {
      itemId: assignItem.value.id,
      quantity: -assignForm.quantity,
      note: `Přiděleno: ${employee?.name || employee?.email} - ${assignForm.note || ''}`
    })
    
    // 2. Uložit přiřazení lokálně
    assignments.value.push({
      id: Date.now().toString(),
      itemId: assignItem.value.id,
      itemName: assignItem.value.name,
      itemSku: assignItem.value.sku,
      employeeId: assignForm.employeeId,
      employeeName: employee?.name || employee?.email || t('inventory.unknownEmployee'),
      quantity: assignForm.quantity,
      note: assignForm.note,
      assignedAt: new Date().toISOString()
    })
    saveAssignments()
    
    showAssignModal.value = false
    await loadAll()
  } catch (e: any) {
    alert(e.response?.data?.message || t('common.saveError'))
  } finally {
    assigning.value = false
  }
}

const returnToWarehouse = async (assignment: any) => {
  if (!confirm(`${t('inventory.confirmReturn')} "${assignment.itemName}"?`)) return
  
  try {
    // 1. Přidat zpět na sklad (movement s kladným množstvím)
    await api.post('/api/inventory/movements', {
      itemId: assignment.itemId,
      quantity: assignment.quantity,
      note: `${t('inventory.returnedFromEmployee')}: ${assignment.employeeName}`
    })
    
    // 2. Odstranit přiřazení
    assignments.value = assignments.value.filter(a => a.id !== assignment.id)
    saveAssignments()
    
    await loadAll()
  } catch (e: any) {
    alert(e.response?.data?.message || t('common.saveError'))
  }
}

const showQrCode = async (item: any) => {
  qrItem.value = item
  qrImageUrl.value = null
  showQrModal.value = true
  
  try {
    const response = await fetch(`/api/inventory/${item.id}/qrcode`, {
      headers: { 'Authorization': `Bearer ${localStorage.getItem('token')}` }
    })
    if (response.ok) {
      const blob = await response.blob()
      qrImageUrl.value = URL.createObjectURL(blob)
    }
  } catch (e) {
    console.error('Failed to load QR code', e)
  }
}

const printQrCode = () => {
  if (!qrImageUrl.value || !qrItem.value) return
  const printWindow = window.open('', '_blank')
  if (!printWindow) { alert(t('common.enablePopups')); return }
  
  printWindow.document.write(`
    <!DOCTYPE html>
    <html><head><title>QR - ${qrItem.value.name}</title>
    <style>body{display:flex;flex-direction:column;align-items:center;justify-content:center;min-height:100vh;margin:0;font-family:Arial,sans-serif}img{max-width:300px}h2{margin-bottom:10px}p{color:#666;margin:5px 0}</style>
    </head><body><h2>${qrItem.value.name}</h2><p>${qrItem.value.sku || ''}</p><img src="${qrImageUrl.value}" alt="QR" />
    <script>window.onload=function(){window.print();window.onafterprint=function(){window.close()}}<\/script></body></html>
  `)
  printWindow.document.close()
}

// Lifecycle
onMounted(() => loadAll())
</script>
