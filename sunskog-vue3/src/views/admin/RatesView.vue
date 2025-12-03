<template>
  <div class="space-y-6">
    <!-- Header -->
    <div class="flex justify-between items-center">
      <h1 class="text-2xl font-bold text-sunskog-dark">{{ t('rates.title') }}</h1>
    </div>

    <!-- Tabs -->
    <div class="border-b border-gray-200">
      <nav class="flex space-x-8">
        <button
          @click="activeTab = 'current'"
          :class="[
            'py-2 px-1 border-b-2 font-medium text-sm transition',
            activeTab === 'current' 
              ? 'border-sunskog-primary text-sunskog-primary' 
              : 'border-transparent text-gray-500 hover:text-gray-700'
          ]"
        >
          {{ t('rates.currentRates') }}
        </button>
        <button
          v-if="authStore.isManagement || authStore.isAdmin"
          @click="activeTab = 'history'"
          :class="[
            'py-2 px-1 border-b-2 font-medium text-sm transition',
            activeTab === 'history' 
              ? 'border-sunskog-primary text-sunskog-primary' 
              : 'border-transparent text-gray-500 hover:text-gray-700'
          ]"
        >
          {{ t('rates.history') }}
        </button>
      </nav>
    </div>

    <!-- Loading -->
    <div v-if="loading" class="bg-white rounded-lg shadow p-8 text-center">
      <div class="animate-spin w-8 h-8 border-4 border-sunskog-primary border-t-transparent rounded-full mx-auto"></div>
      <p class="mt-4 text-gray-600">{{ t('common.loading') }}</p>
    </div>

    <!-- Current Rates Tab -->
    <div v-else-if="activeTab === 'current'" class="space-y-6">
      <!-- Rates Form -->
      <div class="bg-white rounded-lg shadow p-6">
        <p class="text-gray-600 mb-6">{{ t('rates.description') }}</p>

        <div class="grid grid-cols-1 md:grid-cols-3 gap-6">
          <!-- Hodinová sazba -->
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-2">
              {{ t('rates.hourlyRate') }}
            </label>
            <div class="relative">
              <input
                v-model.number="rates.hourlyRate"
                type="number"
                step="0.01"
                min="0"
                :disabled="isReadOnly"
                :class="[
                  'w-full px-4 py-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sunskog-primary text-lg',
                  isReadOnly ? 'bg-gray-100 cursor-not-allowed' : ''
                ]"
              />
              <span class="absolute right-4 top-1/2 -translate-y-1/2 text-gray-500">
                {{ t('rates.perHour') }}
              </span>
            </div>
          </div>

          <!-- Sazba za km -->
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-2">
              {{ t('rates.kmRate') }}
            </label>
            <div class="relative">
              <input
                v-model.number="rates.kmRate"
                type="number"
                step="0.01"
                min="0"
                :disabled="isReadOnly"
                :class="[
                  'w-full px-4 py-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sunskog-primary text-lg',
                  isReadOnly ? 'bg-gray-100 cursor-not-allowed' : ''
                ]"
              />
              <span class="absolute right-4 top-1/2 -translate-y-1/2 text-gray-500">
                {{ t('rates.perKm') }}
              </span>
            </div>
          </div>

          <!-- Kusová sazba -->
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-2">
              {{ t('rates.pieceRate') }}
            </label>
            <div class="relative">
              <input
                v-model.number="rates.pieceRate"
                type="number"
                step="0.01"
                min="0"
                :disabled="isReadOnly"
                :class="[
                  'w-full px-4 py-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sunskog-primary text-lg',
                  isReadOnly ? 'bg-gray-100 cursor-not-allowed' : ''
                ]"
              />
              <span class="absolute right-4 top-1/2 -translate-y-1/2 text-gray-500">
                {{ t('rates.perPiece') }}
              </span>
            </div>
          </div>
        </div>

        <!-- Platnost od -->
        <div v-if="!isReadOnly" class="mt-6">
          <label class="block text-sm font-medium text-gray-700 mb-2">
            {{ t('rates.validFrom') }}
          </label>
          <input
            v-model="validFrom"
            type="date"
            class="px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sunskog-primary"
          />
          <p class="text-sm text-gray-500 mt-1">{{ t('rates.validFromHint') }}</p>
        </div>

        <!-- Save button -->
        <div v-if="!isReadOnly" class="mt-8 flex justify-end">
          <button
            @click="saveRates"
            :disabled="saving"
            class="bg-sunskog-primary hover:bg-sunskog-hover text-white px-8 py-3 rounded-lg font-semibold transition disabled:opacity-50"
          >
            {{ saving ? t('common.loading') : t('rates.saveRates') }}
          </button>
        </div>

        <!-- Success message -->
        <div v-if="saved" class="mt-4 p-4 bg-green-100 border border-green-400 text-green-700 rounded-lg">
          {{ t('rates.ratesSaved') }}
        </div>
        
        <!-- Error message -->
        <div v-if="error" class="mt-4 p-4 bg-red-100 border border-red-400 text-red-700 rounded-lg">
          {{ error }}
        </div>
      </div>

      <!-- Preview -->
      <div class="bg-white rounded-lg shadow p-6">
        <h2 class="text-lg font-semibold text-sunskog-dark mb-4">{{ t('rates.currentRates') }}</h2>
        
        <div class="overflow-x-auto">
          <table class="min-w-full">
            <thead>
              <tr class="border-b-2 border-gray-300">
                <th class="text-left py-3 px-4 text-gray-700 font-semibold">{{ t('rates.rateType') }}</th>
                <th class="text-right py-3 px-4 text-gray-700 font-semibold">{{ t('rates.amount') }}</th>
              </tr>
            </thead>
            <tbody>
              <tr class="border-b border-gray-200">
                <td class="py-4 px-4 text-gray-800">{{ t('rates.hourlyRate') }}</td>
                <td class="text-right py-4 px-4 font-semibold text-lg text-sunskog-primary">{{ rates.hourlyRate.toFixed(2) }} {{ t('rates.perHour') }}</td>
              </tr>
              <tr class="border-b border-gray-200">
                <td class="py-4 px-4 text-gray-800">{{ t('rates.kmRate') }}</td>
                <td class="text-right py-4 px-4 font-semibold text-lg text-sunskog-primary">{{ rates.kmRate.toFixed(2) }} {{ t('rates.perKm') }}</td>
              </tr>
              <tr class="border-b border-gray-200">
                <td class="py-4 px-4 text-gray-800">{{ t('rates.pieceRate') }}</td>
                <td class="text-right py-4 px-4 font-semibold text-lg text-sunskog-primary">{{ rates.pieceRate.toFixed(2) }} {{ t('rates.perPiece') }}</td>
              </tr>
            </tbody>
          </table>
        </div>
        
        <p v-if="currentValidFrom" class="text-sm text-gray-500 mt-4">
          {{ t('rates.validSince') }}: {{ formatDate(currentValidFrom) }}
        </p>
      </div>
    </div>

    <!-- History Tab -->
    <div v-else-if="activeTab === 'history'" class="bg-white rounded-lg shadow overflow-hidden">
      <div class="p-6 border-b">
        <h2 class="text-lg font-semibold text-sunskog-dark">{{ t('rates.history') }}</h2>
        <p class="text-sm text-gray-500 mt-1">{{ t('rates.historyDescription') }}</p>
      </div>
      
      <div class="overflow-x-auto">
        <table class="min-w-full divide-y divide-gray-200">
          <thead class="bg-gray-50">
            <tr>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">{{ t('rates.period') }}</th>
              <th class="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase">{{ t('rates.hourlyRate') }}</th>
              <th class="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase">{{ t('rates.kmRate') }}</th>
              <th class="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase">{{ t('rates.pieceRate') }}</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">{{ t('rates.changedBy') }}</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">{{ t('rates.changedAt') }}</th>
            </tr>
          </thead>
          <tbody class="bg-white divide-y divide-gray-200">
            <tr v-if="history.length === 0">
              <td colspan="6" class="px-4 py-8 text-center text-gray-500">{{ t('rates.noHistory') }}</td>
            </tr>
            <tr 
              v-for="(rate, index) in history" 
              :key="rate.id"
              :class="{ 'bg-green-50': index === 0 && !rate.validTo }"
            >
              <td class="px-4 py-3">
                <div class="text-sm font-medium text-gray-900">
                  {{ formatDate(rate.validFrom) }}
                  <span class="text-gray-400 mx-1">→</span>
                  <span :class="rate.validTo ? 'text-gray-600' : 'text-green-600 font-semibold'">
                    {{ rate.validTo ? formatDate(rate.validTo) : t('rates.currentlyValid') }}
                  </span>
                </div>
              </td>
              <td class="px-4 py-3 text-right text-sm text-gray-900">{{ rate.hourRate.toFixed(2) }} kr/h</td>
              <td class="px-4 py-3 text-right text-sm text-gray-900">{{ rate.kmRate.toFixed(2) }} kr/km</td>
              <td class="px-4 py-3 text-right text-sm text-gray-900">{{ rate.pieceRate.toFixed(2) }} kr/ks</td>
              <td class="px-4 py-3 text-sm text-gray-600">{{ rate.changedByUserName || '-' }}</td>
              <td class="px-4 py-3 text-sm text-gray-600">{{ formatDateTime(rate.createdAt) }}</td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted, computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth'
import api from '@/services/api'

const { t, locale } = useI18n()
const authStore = useAuthStore()

// Tabs
const activeTab = ref('current')

// Read-only pro všechny kromě Management
const isReadOnly = computed(() => !authStore.canManageRates)

// State
const loading = ref(false)
const saving = ref(false)
const saved = ref(false)
const error = ref<string | null>(null)

const rates = reactive({
  hourlyRate: 150,
  kmRate: 2.5,
  pieceRate: 10
})

const validFrom = ref(new Date().toISOString().split('T')[0])
const currentValidFrom = ref<string | null>(null)

interface RateHistory {
  id: string
  hourRate: number
  kmRate: number
  pieceRate: number
  validFrom: string
  validTo: string | null
  createdAt: string
  changedByUserId: string | null
  changedByUserName: string | null
}

const history = ref<RateHistory[]>([])

// Načíst aktuální sazby
const loadCurrentRates = async () => {
  try {
    const data = await api.get<any>('/api/rates/current')
    rates.hourlyRate = data.hourRate
    rates.kmRate = data.kmRate
    rates.pieceRate = data.pieceRate
    currentValidFrom.value = data.validFrom
    
    // Uložit do localStorage pro offline použití
    localStorage.setItem('sunskog_rates', JSON.stringify({
      hourlyRate: data.hourRate,
      kmRate: data.kmRate,
      pieceRate: data.pieceRate
    }))
  } catch (e) {
    console.error('Failed to load rates from API, using localStorage fallback')
    // Fallback na localStorage
    const savedRates = localStorage.getItem('sunskog_rates')
    if (savedRates) {
      const parsed = JSON.parse(savedRates)
      rates.hourlyRate = parsed.hourlyRate || 150
      rates.kmRate = parsed.kmRate || 2.5
      rates.pieceRate = parsed.pieceRate || 10
    }
  }
}

// Načíst historii sazeb
const loadHistory = async () => {
  if (!authStore.isManagement && !authStore.isAdmin) return
  
  try {
    const data = await api.get<RateHistory[]>('/api/rates/history')
    history.value = data
  } catch (e) {
    console.error('Failed to load rates history', e)
  }
}

// Uložit nové sazby
const saveRates = async () => {
  saving.value = true
  saved.value = false
  error.value = null
  
  try {
    await api.post('/api/rates', {
      hourRate: rates.hourlyRate,
      kmRate: rates.kmRate,
      pieceRate: rates.pieceRate,
      validFrom: validFrom.value || null
    })
    
    // Uložit do localStorage
    localStorage.setItem('sunskog_rates', JSON.stringify({
      hourlyRate: rates.hourlyRate,
      kmRate: rates.kmRate,
      pieceRate: rates.pieceRate
    }))
    
    saved.value = true
    
    // Reload history
    await loadHistory()
    await loadCurrentRates()
    
    // Skrýt zprávu po 3 sekundách
    setTimeout(() => {
      saved.value = false
    }, 3000)
  } catch (e: any) {
    error.value = e.response?.data?.message || t('common.saveError')
  } finally {
    saving.value = false
  }
}

// Formátování data
const formatDate = (dateStr: string) => {
  if (!dateStr) return '-'
  const date = new Date(dateStr)
  return date.toLocaleDateString(locale.value === 'cs' ? 'cs-CZ' : 'en-GB')
}

const formatDateTime = (dateStr: string) => {
  if (!dateStr) return '-'
  const date = new Date(dateStr)
  return date.toLocaleString(locale.value === 'cs' ? 'cs-CZ' : 'en-GB', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  })
}

// Lifecycle
onMounted(async () => {
  loading.value = true
  try {
    await loadCurrentRates()
    await loadHistory()
  } finally {
    loading.value = false
  }
})
</script>