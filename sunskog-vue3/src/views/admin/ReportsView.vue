<template>
  <div class="space-y-6">
    <!-- Header -->
    <div class="flex justify-between items-center">
      <h1 class="text-2xl font-bold text-sunskog-dark">{{ t('admin.reports.title') }}</h1>
    </div>

    <!-- Export výkazů -->
    <div class="bg-white rounded-lg shadow p-6">
      <h2 class="text-lg font-semibold text-sunskog-dark mb-4">{{ t('admin.reports.exportTimesheets') }}</h2>
      
      <div class="grid grid-cols-1 md:grid-cols-3 gap-4 mb-6">
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('admin.reports.fromDate') }}</label>
          <input
            v-model="filters.from"
            type="date"
            class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sunskog-primary"
          />
        </div>
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('admin.reports.toDate') }}</label>
          <input
            v-model="filters.to"
            type="date"
            class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sunskog-primary"
          />
        </div>
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('timesheets.status') }}</label>
          <select
            v-model="filters.status"
            class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sunskog-primary"
          >
            <option value="">{{ t('admin.reports.allStatuses') }}</option>
            <option value="0">{{ t('timesheets.statusDraft') }}</option>
            <option value="1">{{ t('timesheets.statusPending') }}</option>
            <option value="2">{{ t('timesheets.statusApproved') }}</option>
            <option value="3">{{ t('admin.reports.statusReturned') }}</option>
          </select>
        </div>
      </div>

      <div class="flex space-x-3">
        <button
          @click="applyFilter"
          class="bg-gray-600 hover:bg-gray-700 text-white px-6 py-2 rounded-lg flex items-center space-x-2"
        >
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 4a1 1 0 011-1h16a1 1 0 011 1v2.586a1 1 0 01-.293.707l-6.414 6.414a1 1 0 00-.293.707V17l-4 4v-6.586a1 1 0 00-.293-.707L3.293 7.293A1 1 0 013 6.586V4z" />
          </svg>
          <span>{{ t('admin.reports.applyFilter') }}</span>
        </button>
        <button
          @click="previewReport"
          :disabled="loading"
          class="bg-blue-600 hover:bg-blue-700 text-white px-6 py-2 rounded-lg flex items-center space-x-2 disabled:opacity-50"
        >
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
          </svg>
          <span>{{ loading ? t('common.loading') : t('admin.reports.preview') }}</span>
        </button>
        <button
          @click="exportToCsv"
          :disabled="exporting"
          class="bg-sunskog-primary hover:bg-sunskog-hover text-white px-6 py-2 rounded-lg flex items-center space-x-2 disabled:opacity-50"
        >
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 10v6m0 0l-3-3m3 3l3-3m2 8H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
          </svg>
          <span>{{ exporting ? t('common.loading') : t('admin.reports.exportCsv') }}</span>
        </button>
      </div>
    </div>

    <!-- Náhled reportu -->
    <div v-if="reportData.length > 0" class="bg-white rounded-lg shadow overflow-hidden">
      <div class="p-4 border-b border-gray-200 flex justify-between items-center">
        <h2 class="text-lg font-semibold text-sunskog-dark">{{ t('admin.reports.preview') }} ({{ reportData.length }} {{ t('admin.reports.records') }})</h2>
        <button
          @click="reportData = []"
          class="text-gray-500 hover:text-gray-700"
        >
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
          </svg>
        </button>
      </div>
      <div class="overflow-x-auto">
        <table class="min-w-full divide-y divide-gray-200">
          <thead class="bg-gray-50">
            <tr>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">{{ t('admin.reports.employee') }}</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">{{ t('timesheets.period') }}</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">{{ t('timesheets.status') }}</th>
              <th class="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase">{{ t('timesheets.hours') }}</th>
              <th class="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase">{{ t('timesheets.km') }}</th>
              <th class="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase">{{ t('timesheets.total') }}</th>
            </tr>
          </thead>
          <tbody class="bg-white divide-y divide-gray-200">
            <tr v-for="row in reportData" :key="row.id" class="hover:bg-gray-50">
              <td class="px-4 py-3 text-sm text-gray-900">{{ row.userName || row.userEmail }}</td>
              <td class="px-4 py-3 text-sm text-gray-900">{{ formatDate(row.periodStart) }} – {{ formatDate(row.periodEnd) }}</td>
              <td class="px-4 py-3">
                <span :class="statusClass(row.status)" class="px-2 py-1 text-xs font-medium rounded-full">
                  {{ statusLabel(row.status) }}
                </span>
              </td>
              <td class="px-4 py-3 text-sm text-gray-900 text-right">{{ row.totalHours }}h</td>
              <td class="px-4 py-3 text-sm text-gray-900 text-right">{{ row.totalKm }} km</td>
              <td class="px-4 py-3 text-sm font-semibold text-gray-900 text-right">{{ formatCurrency(row.totalPay) }}</td>
            </tr>
          </tbody>
          <tfoot class="bg-gray-50">
            <tr>
              <td colspan="3" class="px-4 py-3 text-sm font-semibold text-gray-900">{{ t('timesheets.total') }}</td>
              <td class="px-4 py-3 text-sm font-semibold text-gray-900 text-right">{{ totalHours }}h</td>
              <td class="px-4 py-3 text-sm font-semibold text-gray-900 text-right">{{ totalKm }} km</td>
              <td class="px-4 py-3 text-sm font-bold text-gray-900 text-right">{{ formatCurrency(totalPay) }}</td>
            </tr>
          </tfoot>
        </table>
      </div>
    </div>

    <!-- Statistiky -->
    <div class="bg-white rounded-lg shadow p-6">
      <h2 class="text-lg font-semibold text-sunskog-dark mb-4">{{ t('admin.reports.statistics') }}</h2>
      
      <div class="grid grid-cols-1 md:grid-cols-4 gap-4">
        <div class="bg-gray-50 rounded-lg p-4 text-center">
          <p class="text-sm text-gray-500">{{ t('admin.reports.totalTimesheets') }}</p>
          <p class="text-3xl font-bold text-sunskog-dark">{{ stats.totalTimesheets }}</p>
        </div>
        <div class="bg-blue-50 rounded-lg p-4 text-center">
          <p class="text-sm text-blue-600">{{ t('dashboard.pendingApproval') }}</p>
          <p class="text-3xl font-bold text-blue-700">{{ stats.pending }}</p>
        </div>
        <div class="bg-green-50 rounded-lg p-4 text-center">
          <p class="text-sm text-green-600">{{ t('timesheets.statusApproved') }}</p>
          <p class="text-3xl font-bold text-green-700">{{ stats.approved }}</p>
        </div>
        <div class="bg-orange-50 rounded-lg p-4 text-center">
          <p class="text-sm text-orange-600">{{ t('admin.reports.statusReturned') }}</p>
          <p class="text-3xl font-bold text-orange-700">{{ stats.returned }}</p>
        </div>
      </div>
    </div>

    <!-- Rychlé akce -->
    <div class="bg-white rounded-lg shadow p-6">
      <h2 class="text-lg font-semibold text-sunskog-dark mb-4">{{ t('dashboard.quickActions') }}</h2>
      
      <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
        <button
          @click="exportThisMonth"
          class="p-4 border-2 border-sunskog-primary rounded-lg hover:bg-sunskog-primary hover:text-white transition group text-left"
        >
          <svg class="w-8 h-8 mb-2 text-sunskog-primary group-hover:text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
          </svg>
          <p class="font-semibold">{{ t('admin.reports.exportThisMonth') }}</p>
          <p class="text-sm text-gray-500 group-hover:text-white/80">{{ t('admin.reports.allApproved') }}</p>
        </button>

        <button
          @click="exportLastMonth"
          class="p-4 border-2 border-gray-300 rounded-lg hover:bg-gray-100 transition text-left"
        >
          <svg class="w-8 h-8 mb-2 text-gray-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
          </svg>
          <p class="font-semibold">{{ t('admin.reports.exportLastMonth') }}</p>
          <p class="text-sm text-gray-500">{{ t('admin.reports.allApproved') }}</p>
        </button>

        <button
          @click="exportAll"
          class="p-4 border-2 border-gray-300 rounded-lg hover:bg-gray-100 transition text-left"
        >
          <svg class="w-8 h-8 mb-2 text-gray-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4" />
          </svg>
          <p class="font-semibold">{{ t('admin.reports.exportAll') }}</p>
          <p class="text-sm text-gray-500">{{ t('admin.reports.completeExport') }}</p>
        </button>
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
const loading = ref(false)
const exporting = ref(false)
const reportData = ref<any[]>([])

const filters = reactive({
  from: '',
  to: '',
  status: ''
})

const stats = reactive({
  totalTimesheets: 0,
  pending: 0,
  approved: 0,
  returned: 0
})

// Computed
const totalHours = computed(() => reportData.value.reduce((sum, r) => sum + (r.totalHours || 0), 0))
const totalKm = computed(() => reportData.value.reduce((sum, r) => sum + (r.totalKm || 0), 0))
const totalPay = computed(() => reportData.value.reduce((sum, r) => sum + (r.totalPay || 0), 0))

// Methods
const formatDate = (dateStr: string) => {
  if (!dateStr) return '-'
  return new Date(dateStr).toLocaleDateString()
}

const formatCurrency = (amount: number) => {
  return `${(amount || 0).toFixed(2).replace('.', ',')} kr`
}

const statusClass = (status: string) => {
  const classes: Record<string, string> = {
    'Draft': 'bg-gray-100 text-gray-800',
    'Submitted': 'bg-blue-100 text-blue-800',
    'Approved': 'bg-green-100 text-green-800',
    'Returned': 'bg-orange-100 text-orange-800'
  }
  return classes[status] || 'bg-gray-100 text-gray-800'
}

const statusLabel = (status: string) => {
  const labels: Record<string, string> = {
    'Draft': t('timesheets.statusDraft'),
    'Submitted': t('timesheets.statusPending'),
    'Approved': t('timesheets.statusApproved'),
    'Returned': t('admin.reports.statusReturned')
  }
  return labels[status] || status
}

const loadStats = async () => {
  try {
    const data = await api.get<any[]>('/api/timesheets')
    stats.totalTimesheets = data.length
    stats.pending = data.filter(t => t.status === 'Submitted').length
    stats.approved = data.filter(t => t.status === 'Approved').length
    stats.returned = data.filter(t => t.status === 'Returned').length
  } catch (e) {
    // Ignore - stats are not critical
  }
}

const buildQueryString = () => {
  const params = new URLSearchParams()
  if (filters.from) params.append('from', filters.from)
  if (filters.to) params.append('to', filters.to)
  if (filters.status) params.append('status', filters.status)
  return params.toString() ? `?${params.toString()}` : ''
}

const previewReport = async () => {
  loading.value = true
  try {
    const query = buildQueryString()
    const data = await api.get<any[]>(`/api/timesheets${query}`)
    reportData.value = data
  } catch (e: any) {
    alert(e.response?.data?.message || t('common.loadError'))
  } finally {
    loading.value = false
  }
}

const applyFilter = () => {
  previewReport()
}

const generateCsvContent = () => {
  if (reportData.value.length === 0) return ''
  
  const headers = [t('admin.reports.employee'), t('timesheets.period'), t('timesheets.status'), t('timesheets.hours'), t('timesheets.km'), t('timesheets.total')]
  const rows = reportData.value.map(row => [
    row.userName || row.userEmail || '',
    `${formatDate(row.periodStart)} - ${formatDate(row.periodEnd)}`,
    statusLabel(row.status),
    row.totalHours || 0,
    row.totalKm || 0,
    row.totalPay || 0
  ])
  
  const csvContent = [
    headers.join(';'),
    ...rows.map(r => r.join(';'))
  ].join('\n')
  
  return csvContent
}

const exportToCsv = async () => {
  // Pokud nemáme data, nejprve načteme
  if (reportData.value.length === 0) {
    await previewReport()
  }
  
  if (reportData.value.length === 0) {
    alert(t('admin.reports.noData'))
    return
  }
  
  exporting.value = true
  try {
    const csvContent = generateCsvContent()
    const blob = new Blob(['\ufeff' + csvContent], { type: 'text/csv;charset=utf-8;' })
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `report_${filters.from || 'all'}_${filters.to || 'all'}.csv`
    document.body.appendChild(a)
    a.click()
    document.body.removeChild(a)
    URL.revokeObjectURL(url)
  } finally {
    exporting.value = false
  }
}

const exportThisMonth = async () => {
  const now = new Date()
  filters.from = new Date(now.getFullYear(), now.getMonth(), 1).toISOString().split('T')[0]
  filters.to = new Date(now.getFullYear(), now.getMonth() + 1, 0).toISOString().split('T')[0]
  filters.status = '2' // Approved
  await previewReport()
  await exportToCsv()
}

const exportLastMonth = async () => {
  const now = new Date()
  filters.from = new Date(now.getFullYear(), now.getMonth() - 1, 1).toISOString().split('T')[0]
  filters.to = new Date(now.getFullYear(), now.getMonth(), 0).toISOString().split('T')[0]
  filters.status = '2' // Approved
  await previewReport()
  await exportToCsv()
}

const exportAll = async () => {
  filters.from = ''
  filters.to = ''
  filters.status = ''
  await previewReport()
  await exportToCsv()
}

// Lifecycle
onMounted(() => {
  loadStats()
})
</script>