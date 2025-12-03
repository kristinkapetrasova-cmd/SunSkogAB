import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { authService } from '@/services/auth.service'
import type { User, LoginCredentials } from '@/types'

export const useAuthStore = defineStore('auth', () => {
  // State
  const user = ref<User | null>(authService.getCurrentUser())
  const token = ref<string | null>(authService.getToken())
  const loading = ref(false)
  const error = ref<string | null>(null)

  // Helper pro kontrolu role
  const hasRole = (role: string): boolean => {
    if (!user.value) return false
    if (user.value.roles && Array.isArray(user.value.roles)) {
      return user.value.roles.includes(role)
    }
    return user.value.role === role
  }

  // Getters
  const isAuthenticated = computed(() => !!token.value)
  
  // Role checks
  const isAdmin = computed(() => hasRole('Admin'))
  const isManagement = computed(() => hasRole('Management') || hasRole('Admin'))
  const isTeamLead = computed(() => hasRole('TeamLead') || hasRole('Admin'))
  const isAccountant = computed(() => hasRole('Accountant') || hasRole('Admin'))
  const isWarehouse = computed(() => hasRole('Warehouse') || hasRole('Admin'))
  const isWorker = computed(() => hasRole('Worker') || hasRole('User'))
  
  // Kombinované kontroly
  const canManageUsers = computed(() => isAdmin.value)
  const canManageRates = computed(() => hasRole('Management')) // Pouze Management může editovat sazby, Admin má náhled
  const canViewRates = computed(() => hasRole('Management') || hasRole('Admin')) // Oba mohou vidět stránku sazeb
  const canApproveTimesheets = computed(() => isTeamLead.value || isManagement.value)
  const canViewAllTimesheets = computed(() => isAccountant.value || isManagement.value) // TeamLead vidí jen svůj tým
  const canEditTimesheets = computed(() => !isAccountant.value || isAdmin.value) // Účetní nemůže upravovat
  const canAccessWarehouse = computed(() => isWarehouse.value || isTeamLead.value || isManagement.value || isWorker.value) // Worker vidí přidělené položky
  const canManageWarehouse = computed(() => isWarehouse.value || isManagement.value) // Přidávat/odebírat položky
  const canSubmitTimesheets = computed(() => !isManagement.value || isAdmin.value) // Management nezadává výkazy

  // Actions
  async function login(credentials: LoginCredentials) {
    loading.value = true
    error.value = null
    
    try {
      const response = await authService.login(credentials)
      user.value = response.user
      token.value = response.token
      return true
    } catch (err: any) {
      error.value = err.response?.data?.message || 'auth.loginError'
      return false
    } finally {
      loading.value = false
    }
  }

  function logout() {
    authService.logout()
    user.value = null
    token.value = null
  }

  function clearError() {
    error.value = null
  }

  return {
    // State
    user,
    token,
    loading,
    error,
    // Getters
    isAuthenticated,
    isAdmin,
    isManagement,
    isTeamLead,
    isAccountant,
    isWarehouse,
    isWorker,
    canManageUsers,
    canManageRates,
    canViewRates,
    canApproveTimesheets,
    canViewAllTimesheets,
    canEditTimesheets,
    canAccessWarehouse,
    canManageWarehouse,
    canSubmitTimesheets,
    // Actions
    login,
    logout,
    clearError,
    hasRole
  }
})