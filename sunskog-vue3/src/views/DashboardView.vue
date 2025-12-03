<template>
  <div class="space-y-6">
    <!-- Welcome Card -->
    <div class="bg-white rounded-lg shadow p-6">
      <h1 class="text-3xl font-bold text-sunskog-dark">
        {{ t('dashboard.welcome') }}, {{ authStore.user?.name || authStore.user?.email }}
      </h1>
      <p class="text-gray-600 mt-2">{{ currentDate }}</p>
    </div>

    <!-- Stats Cards -->
    <div class="grid grid-cols-1 md:grid-cols-3 gap-6">
      <!-- Moje výkazy / Všechny výkazy -->
      <RouterLink 
        :to="timesheetsLink"
        class="bg-white rounded-lg shadow p-6 cursor-pointer transition-all duration-200 hover:shadow-lg hover:-translate-y-1 hover:bg-gray-50"
      >
        <div class="flex items-center justify-between">
          <div>
            <p class="text-gray-600 text-sm">
              {{ authStore.canViewAllTimesheets ? t('dashboard.allTimesheets') : (authStore.isTeamLead ? t('timesheets.teamTimesheets') : t('dashboard.myTimesheets')) }}
            </p>
            <p class="text-3xl font-bold text-sunskog-primary mt-2">{{ stats.totalTimesheets }}</p>
          </div>
          <div class="w-12 h-12 bg-sunskog-primary/10 rounded-full flex items-center justify-center">
            <svg class="w-6 h-6 text-sunskog-primary" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
          </div>
        </div>
      </RouterLink>

      <!-- Čekající na schválení - pro vedoucí/management -->
      <RouterLink 
        v-if="authStore.canApproveTimesheets"
        :to="timesheetsLink"
        class="bg-white rounded-lg shadow p-6 cursor-pointer transition-all duration-200 hover:shadow-lg hover:-translate-y-1 hover:bg-gray-50"
      >
        <div class="flex items-center justify-between">
          <div>
            <p class="text-gray-600 text-sm">{{ t('dashboard.pendingApproval') }}</p>
            <p class="text-3xl font-bold text-orange-500 mt-2">{{ stats.pending }}</p>
          </div>
          <div class="w-12 h-12 bg-orange-100 rounded-full flex items-center justify-center">
            <svg class="w-6 h-6 text-orange-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
          </div>
        </div>
      </RouterLink>

      <!-- Přidělené položky - pro zaměstnance -->
      <RouterLink 
        v-if="authStore.isWorker && !authStore.canApproveTimesheets"
        to="/app/warehouse"
        class="bg-white rounded-lg shadow p-6 cursor-pointer transition-all duration-200 hover:shadow-lg hover:-translate-y-1 hover:bg-gray-50"
      >
        <div class="flex items-center justify-between">
          <div>
            <p class="text-gray-600 text-sm">{{ t('dashboard.myAssignedItems') }}</p>
            <p class="text-3xl font-bold text-blue-500 mt-2">{{ stats.assignedItems }}</p>
          </div>
          <div class="w-12 h-12 bg-blue-100 rounded-full flex items-center justify-center">
            <svg class="w-6 h-6 text-blue-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4" />
            </svg>
          </div>
        </div>
      </RouterLink>

      <!-- Celkem hodin tento měsíc -->
      <div class="bg-white rounded-lg shadow p-6 transition-all duration-200 hover:shadow-lg hover:-translate-y-1 hover:bg-gray-50">
        <div class="flex items-center justify-between">
          <div>
            <p class="text-gray-600 text-sm">{{ t('dashboard.totalHours') }}</p>
            <p class="text-3xl font-bold text-sunskog-dark mt-2">{{ stats.totalHours }}h</p>
          </div>
          <div class="w-12 h-12 bg-gray-100 rounded-full flex items-center justify-center">
            <svg class="w-6 h-6 text-gray-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
            </svg>
          </div>
        </div>
      </div>
    </div>

    <!-- Nízký stav skladu - pro warehouse/management -->
    <div v-if="authStore.canAccessWarehouse && stats.lowStock > 0" class="bg-orange-50 border-l-4 border-orange-500 rounded-lg shadow p-6">
      <div class="flex items-center">
        <svg class="w-8 h-8 text-orange-500 mr-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
        </svg>
        <div>
          <p class="font-semibold text-orange-700">{{ t('warehouse.lowStockAlert') }}</p>
          <p class="text-orange-600">{{ stats.lowStock }} {{ t('warehouse.itemsBelowMin') }}</p>
        </div>
        <RouterLink to="/app/warehouse" class="ml-auto text-orange-700 hover:text-orange-900 font-medium">
          {{ t('timesheets.detail') }} →
        </RouterLink>
      </div>
    </div>

    <!-- Poslední aktivita -->
    <div class="bg-white rounded-lg shadow">
      <div class="p-6 border-b border-gray-200">
        <h2 class="text-xl font-bold text-sunskog-dark">{{ t('dashboard.recentActivity') }}</h2>
      </div>
      <div class="p-6">
        <div v-if="loading" class="text-center py-8">
          <div class="animate-spin w-6 h-6 border-4 border-sunskog-primary border-t-transparent rounded-full mx-auto"></div>
        </div>
        <div v-else-if="recentTimesheets.length === 0" class="text-center py-8 text-gray-500">
          {{ t('timesheets.noTimesheets') }}
        </div>
        <div v-else class="space-y-4">
          <RouterLink 
            v-for="ts in recentTimesheets" 
            :key="ts.id" 
            to="/app/timesheets"
            class="flex items-center space-x-4 p-4 bg-gray-50 rounded-lg hover:bg-gray-100 transition"
          >
            <div class="w-10 h-10 bg-sunskog-primary rounded-full flex items-center justify-center text-white font-bold">
              {{ getInitials(ts.userName || ts.userEmail || authStore.user?.name) }}
            </div>
            <div class="flex-1">
              <p class="font-semibold text-gray-900">
                {{ formatDate(ts.periodStart) }} – {{ formatDate(ts.periodEnd) }}
              </p>
              <p class="text-sm text-gray-600">
                {{ ts.totalHours }}h • {{ ts.totalKm }} km
              </p>
            </div>
            <span :class="statusClass(ts.status)">
              {{ statusLabel(ts.status) }}
            </span>
          </RouterLink>
        </div>
      </div>
    </div>

    <!-- Rychlé akce -->
    <div class="bg-white rounded-lg shadow p-6">
      <h2 class="text-xl font-bold text-sunskog-dark mb-4">{{ t('dashboard.quickActions') }}</h2>
      <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
        <!-- Nový výkaz - ne pro účetní a management -->
        <RouterLink
          v-if="!authStore.isAccountant && !authStore.isManagement"
          to="/app/timesheets"
          class="p-4 border-2 border-sunskog-primary rounded-lg hover:bg-sunskog-primary hover:text-white transition group"
        >
          <svg class="w-8 h-8 mb-2 text-sunskog-primary group-hover:text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
          </svg>
          <p class="font-semibold">{{ t('dashboard.newTimesheet') }}</p>
        </RouterLink>

        <!-- Sklad - pro ty co mají přístup -->
        <RouterLink
          v-if="authStore.canAccessWarehouse"
          to="/app/warehouse"
          class="p-4 border-2 border-sunskog-primary rounded-lg hover:bg-sunskog-primary hover:text-white transition group"
        >
          <svg class="w-8 h-8 mb-2 text-sunskog-primary group-hover:text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4" />
          </svg>
          <p class="font-semibold">{{ t('dashboard.viewWarehouse') }}</p>
        </RouterLink>

        <!-- Reporty - pro admin/management/účetní -->
        <RouterLink
          v-if="authStore.isAdmin || authStore.isManagement || authStore.isAccountant"
          to="/app/admin/reports"
          class="p-4 border-2 border-gray-300 rounded-lg hover:bg-gray-100 transition text-left"
        >
          <svg class="w-8 h-8 mb-2 text-gray-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
          </svg>
          <p class="font-semibold">{{ t('dashboard.downloadReports') }}</p>
        </RouterLink>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { RouterLink } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth'
import api from '@/services/api'

const { t, locale } = useI18n()
const authStore = useAuthStore()

const loading = ref(true)
const recentTimesheets = ref<any[]>([])
const stats = reactive({
  totalTimesheets: 0,
  pending: 0,
  totalHours: 0,
  assignedItems: 0,
  lowStock: 0
})

const currentDate = computed(() => {
  const localeCode = locale.value === 'cs' ? 'cs-CZ' : 'en-US'
  return new Date().toLocaleDateString(localeCode, {
    weekday: 'long',
    year: 'numeric',
    month: 'long',
    day: 'numeric'
  })
})

// Pro TeamLeada přesměrovat na záložku týmu
const timesheetsLink = computed(() => {
  if (authStore.isTeamLead && !authStore.canViewAllTimesheets) {
    return '/app/timesheets?tab=team'
  }
  return '/app/timesheets'
})

const formatDate = (dateStr: string) => {
  if (!dateStr) return '-'
  const localeCode = locale.value === 'cs' ? 'cs-CZ' : 'en-US'
  return new Date(dateStr).toLocaleDateString(localeCode)
}

const getInitials = (name: string) => {
  if (!name) return '?'
  return name.split(' ').map(n => n[0]).join('').toUpperCase().substring(0, 2)
}

const statusClass = (status: string) => {
  const classes: Record<string, string> = {
    'Draft': 'px-3 py-1 bg-gray-100 text-gray-800 rounded-full text-sm font-medium',
    'Submitted': 'px-3 py-1 bg-blue-100 text-blue-800 rounded-full text-sm font-medium',
    'Approved': 'px-3 py-1 bg-green-100 text-green-800 rounded-full text-sm font-medium',
    'Returned': 'px-3 py-1 bg-orange-100 text-orange-800 rounded-full text-sm font-medium'
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

const loadDashboard = async () => {
  loading.value = true
  try {
    // Načíst výkazy
    const timesheets = await api.get<any[]>('/api/timesheets')
    const userId = authStore.user?.id
    
    // Pro TeamLeada načíst členy týmu
    let teamMemberIds: string[] = []
    if (authStore.isTeamLead && !authStore.canViewAllTimesheets) {
      try {
        const myTeamResponse = await api.get<any>('/api/my-team')
        if (myTeamResponse.team) {
          teamMemberIds = myTeamResponse.members?.map((m: any) => m.userId) || []
          if (myTeamResponse.team.leadUserId && !teamMemberIds.includes(myTeamResponse.team.leadUserId)) {
            teamMemberIds.push(myTeamResponse.team.leadUserId)
          }
          if (!teamMemberIds.includes(userId!)) {
            teamMemberIds.push(userId!)
          }
        }
      } catch (e) {
        console.error('Failed to load team', e)
      }
    }
    
    // Filtrovat podle role
    const myTimesheets = timesheets.filter(ts => ts.userId === userId)
    const teamTimesheets = teamMemberIds.length > 0 
      ? timesheets.filter(ts => teamMemberIds.includes(ts.userId))
      : myTimesheets
    const pendingTimesheets = authStore.isTeamLead && !authStore.canViewAllTimesheets
      ? teamTimesheets.filter(ts => ts.status === 'Submitted' && ts.userId !== userId)
      : timesheets.filter(ts => ts.status === 'Submitted' && ts.userId !== userId)
    
    // Statistiky
    if (authStore.canViewAllTimesheets) {
      stats.totalTimesheets = timesheets.length
    } else if (authStore.isTeamLead) {
      stats.totalTimesheets = teamTimesheets.length
    } else {
      stats.totalTimesheets = myTimesheets.length
    }
    
    stats.pending = pendingTimesheets.length
    
    // Celkem hodin tento měsíc
    const now = new Date()
    const relevantTimesheets = authStore.canViewAllTimesheets 
      ? timesheets 
      : (authStore.isTeamLead ? teamTimesheets : myTimesheets)
    const thisMonth = relevantTimesheets.filter(ts => {
      const start = new Date(ts.periodStart)
      return start.getMonth() === now.getMonth() && start.getFullYear() === now.getFullYear()
    })
    stats.totalHours = thisMonth.reduce((sum, ts) => sum + (ts.totalHours || 0), 0)
    
    // Poslední výkazy
    recentTimesheets.value = relevantTimesheets
      .sort((a, b) => new Date(b.periodStart).getTime() - new Date(a.periodStart).getTime())
      .slice(0, 5)
    
    // Nízký stav skladu
    if (authStore.canAccessWarehouse) {
      try {
        const lowStock = await api.get<any[]>('/api/inventory/low-stock')
        stats.lowStock = lowStock.length
      } catch { stats.lowStock = 0 }
    }
    
    // Přidělené položky
    if (authStore.isWorker) {
      const saved = localStorage.getItem('inventory_assignments')
      if (saved) {
        const assignments = JSON.parse(saved)
        stats.assignedItems = assignments.filter((a: any) => a.employeeId === userId).length
      }
    }
  } catch (e) {
    console.error('Failed to load dashboard', e)
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  loadDashboard()
})
</script>