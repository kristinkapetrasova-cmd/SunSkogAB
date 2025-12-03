<template>
  <div class="space-y-6">
    <!-- Header -->
    <div class="flex justify-between items-center">
      <h1 class="text-2xl font-bold text-sunskog-dark">{{ t('admin.users.title') }}</h1>
      <button
        @click="openAddModal"
        class="bg-sunskog-primary hover:bg-sunskog-hover text-white px-4 py-2 rounded-lg flex items-center space-x-2 transition"
      >
        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
        </svg>
        <span>{{ t('admin.users.addUser') }}</span>
      </button>
    </div>

    <!-- Loading -->
    <div v-if="loading" class="bg-white rounded-lg shadow p-8 text-center">
      <div class="animate-spin w-8 h-8 border-4 border-sunskog-primary border-t-transparent rounded-full mx-auto"></div>
      <p class="mt-4 text-gray-600">{{ t('common.loading') }}</p>
    </div>

    <!-- Error -->
    <div v-else-if="error" class="bg-red-50 border border-red-200 rounded-lg p-6 text-center">
      <p class="text-red-600">{{ error }}</p>
      <button @click="loadUsers" class="mt-4 text-sunskog-primary hover:underline">
        {{ t('common.tryAgain') }}
      </button>
    </div>

    <!-- Users table -->
    <div v-else class="bg-white rounded-lg shadow overflow-hidden">
      <table class="min-w-full divide-y divide-gray-200">
        <thead class="bg-gray-50">
          <tr>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
              {{ t('admin.users.user') }}
            </th>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
              {{ t('admin.users.role') }}
            </th>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
              {{ t('admin.users.team') }}
            </th>
            <th class="px-6 py-3 text-center text-xs font-medium text-gray-500 uppercase tracking-wider">
              {{ t('admin.users.status') }}
            </th>
            <th class="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
              {{ t('common.actions') }}
            </th>
          </tr>
        </thead>
        <tbody class="bg-white divide-y divide-gray-200">
          <tr v-if="users.length === 0">
            <td colspan="5" class="px-6 py-8 text-center text-gray-500">
              {{ t('admin.users.noUsers') }}
            </td>
          </tr>
          <tr 
            v-for="user in users" 
            :key="user.id"
            class="hover:bg-gray-50"
            :class="{ 'bg-red-50': user.isLockedOut }"
          >
            <td class="px-6 py-4">
              <div class="flex items-center">
                <div class="w-10 h-10 bg-sunskog-primary rounded-full flex items-center justify-center text-white font-bold mr-3">
                  {{ getInitials(user.name) }}
                </div>
                <div>
                  <div class="text-sm font-medium text-gray-900">{{ user.name || t('admin.users.unnamed') }}</div>
                  <div class="text-xs text-gray-500">{{ user.email }}</div>
                </div>
              </div>
            </td>
            <td class="px-6 py-4">
              <div class="flex flex-wrap gap-1">
                <span 
                  v-for="role in user.roles" 
                  :key="role"
                  :class="roleClass(role)"
                  class="px-2 py-0.5 text-xs font-medium rounded-full"
                >
                  {{ t('roles.' + role) }}
                </span>
              </div>
            </td>
            <td class="px-6 py-4 text-sm text-gray-900">
              <div v-if="user.teamName">
                <span class="font-medium">{{ user.teamName }}</span>
                <span v-if="user.teamRole" class="text-gray-500 text-xs ml-1">({{ user.teamRole }})</span>
              </div>
              <span v-else class="text-gray-400">—</span>
            </td>
            <td class="px-6 py-4 text-center">
              <span 
                :class="user.isLockedOut ? 'bg-red-100 text-red-800' : 'bg-green-100 text-green-800'"
                class="px-2 py-1 text-xs font-medium rounded-full"
              >
                {{ user.isLockedOut ? t('admin.users.locked') : t('admin.users.active') }}
              </span>
            </td>
            <td class="px-6 py-4 text-right space-x-2">
              <button
                @click="openEditModal(user)"
                class="text-blue-600 hover:text-blue-800"
                :title="t('common.edit')"
              >
                <svg class="w-5 h-5 inline" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                </svg>
              </button>
              <button
                @click="openResetPasswordModal(user)"
                class="text-orange-600 hover:text-orange-800"
                :title="t('admin.users.resetPassword')"
              >
                <svg class="w-5 h-5 inline" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 7a2 2 0 012 2m4 0a6 6 0 01-7.743 5.743L11 17H9v2H7v2H4a1 1 0 01-1-1v-2.586a1 1 0 01.293-.707l5.964-5.964A6 6 0 1121 9z" />
                </svg>
              </button>
              <button
                @click="toggleLockout(user)"
                :class="user.isLockedOut ? 'text-green-600 hover:text-green-800' : 'text-red-600 hover:text-red-800'"
                :title="user.isLockedOut ? t('admin.users.unlock') : t('admin.users.lock')"
              >
                <svg v-if="user.isLockedOut" class="w-5 h-5 inline" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 11V7a4 4 0 118 0m-4 8v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2z" />
                </svg>
                <svg v-else class="w-5 h-5 inline" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
                </svg>
              </button>
              <button
                @click="deleteUser(user)"
                class="text-red-600 hover:text-red-800"
                :title="t('admin.users.deleteUser')"
              >
                <svg class="w-5 h-5 inline" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                </svg>
              </button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Modal: Přidat uživatele -->
    <div v-if="showAddModal" class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
      <div class="bg-white rounded-lg shadow-xl w-full max-w-lg p-6">
        <h2 class="text-xl font-bold text-sunskog-dark mb-4">{{ t('admin.users.addUser') }}</h2>
        
        <form @submit.prevent="addUser" class="space-y-4">
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('admin.users.email') }} *</label>
            <input
              v-model="addForm.email"
              type="email"
              required
              class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sunskog-primary"
              placeholder="email@example.com"
            />
          </div>

          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('admin.users.fullName') }} *</label>
            <input
              v-model="addForm.name"
              type="text"
              required
              class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sunskog-primary"
              :placeholder="t('admin.users.fullName')"
            />
          </div>

          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('admin.users.password') }} *</label>
            <input
              v-model="addForm.password"
              type="password"
              required
              minlength="6"
              class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sunskog-primary"
              :placeholder="t('admin.users.minChars')"
            />
          </div>

          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('admin.users.team') }}</label>
            <select
              v-model="addForm.teamId"
              class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sunskog-primary"
            >
              <option :value="null">{{ t('admin.users.noTeam') }}</option>
              <option v-for="team in teams" :key="team.id" :value="team.id">
                {{ team.name }}
              </option>
            </select>
          </div>

          <div>
            <label class="block text-sm font-medium text-gray-700 mb-2">{{ t('admin.users.role') }}</label>
            <div class="grid grid-cols-2 gap-2">
              <label 
                v-for="role in availableRoles" 
                :key="role"
                class="flex items-center space-x-2 p-2 border rounded-lg cursor-pointer hover:bg-gray-50"
                :class="{ 'border-sunskog-primary bg-sunskog-primary/5': addForm.roles.includes(role) }"
              >
                <input
                  type="checkbox"
                  :value="role"
                  v-model="addForm.roles"
                  class="h-4 w-4 text-sunskog-primary focus:ring-sunskog-primary border-gray-300 rounded"
                />
                <span class="text-sm">{{ t('roles.' + role) }}</span>
              </label>
            </div>
          </div>

          <div class="flex justify-end space-x-3 pt-4">
            <button
              type="button"
              @click="showAddModal = false"
              class="px-4 py-2 text-gray-600 hover:text-gray-800"
            >
              {{ t('common.cancel') }}
            </button>
            <button
              type="submit"
              :disabled="adding"
              class="px-4 py-2 bg-sunskog-primary hover:bg-sunskog-hover text-white rounded-lg disabled:opacity-50"
            >
              {{ adding ? t('common.loading') : t('admin.users.addUser') }}
            </button>
          </div>
        </form>
      </div>
    </div>

    <!-- Modal: Upravit uživatele -->
    <div v-if="showEditModal" class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
      <div class="bg-white rounded-lg shadow-xl w-full max-w-lg p-6">
        <h2 class="text-xl font-bold text-sunskog-dark mb-4">{{ t('admin.users.editUser') }}</h2>
        <p class="text-gray-600 mb-4">{{ editingUser?.email }}</p>
        
        <form @submit.prevent="saveUser" class="space-y-4">
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('admin.users.fullName') }}</label>
            <input
              v-model="editForm.name"
              type="text"
              class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sunskog-primary"
            />
          </div>

          <div>
            <label class="block text-sm font-medium text-gray-700 mb-2">{{ t('admin.users.role') }}</label>
            <div class="grid grid-cols-2 gap-2">
              <label 
                v-for="role in availableRoles" 
                :key="role"
                class="flex items-center space-x-2 p-2 border rounded-lg cursor-pointer hover:bg-gray-50"
                :class="{ 'border-sunskog-primary bg-sunskog-primary/5': editForm.roles.includes(role) }"
              >
                <input
                  type="checkbox"
                  :value="role"
                  v-model="editForm.roles"
                  class="h-4 w-4 text-sunskog-primary focus:ring-sunskog-primary border-gray-300 rounded"
                />
                <span class="text-sm">{{ t('roles.' + role) }}</span>
              </label>
            </div>
          </div>

          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('admin.users.team') }}</label>
            <select
              v-model="editForm.teamId"
              class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sunskog-primary"
            >
              <option :value="null">{{ t('admin.users.noTeam') }}</option>
              <option v-for="team in teams" :key="team.id" :value="team.id">
                {{ team.name }}
              </option>
            </select>
            <p v-if="editingUser?.teamName && !editForm.teamId" class="text-sm text-orange-600 mt-1">
              {{ t('admin.users.willRemoveFromTeam', { team: editingUser.teamName }) }}
            </p>
          </div>

          <div class="flex justify-end space-x-3 pt-4">
            <button
              type="button"
              @click="showEditModal = false"
              class="px-4 py-2 text-gray-600 hover:text-gray-800"
            >
              {{ t('common.cancel') }}
            </button>
            <button
              type="submit"
              :disabled="saving"
              class="px-4 py-2 bg-sunskog-primary hover:bg-sunskog-hover text-white rounded-lg disabled:opacity-50"
            >
              {{ saving ? t('common.loading') : t('common.save') }}
            </button>
          </div>
        </form>
      </div>
    </div>

    <!-- Modal: Reset hesla -->
    <div v-if="showResetModal" class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
      <div class="bg-white rounded-lg shadow-xl w-full max-w-md p-6">
        <h2 class="text-xl font-bold text-sunskog-dark mb-4">{{ t('admin.users.resetPassword') }}</h2>
        <p class="text-gray-600 mb-4">{{ t('admin.users.user') }}: <strong>{{ resetUser?.email }}</strong></p>
        
        <form @submit.prevent="resetPassword" class="space-y-4">
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('admin.users.newPassword') }} *</label>
            <input
              v-model="newPassword"
              type="password"
              required
              minlength="6"
              class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sunskog-primary"
              :placeholder="t('admin.users.minChars')"
            />
          </div>

          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('admin.users.confirmPassword') }} *</label>
            <input
              v-model="confirmPassword"
              type="password"
              required
              minlength="6"
              class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sunskog-primary"
            />
            <p v-if="newPassword && confirmPassword && newPassword !== confirmPassword" class="text-red-500 text-sm mt-1">
              {{ t('admin.users.passwordMismatch') }}
            </p>
          </div>

          <div class="flex justify-end space-x-3 pt-4">
            <button
              type="button"
              @click="showResetModal = false"
              class="px-4 py-2 text-gray-600 hover:text-gray-800"
            >
              {{ t('common.cancel') }}
            </button>
            <button
              type="submit"
              :disabled="resetting || !newPassword || newPassword !== confirmPassword"
              class="px-4 py-2 bg-orange-500 hover:bg-orange-600 text-white rounded-lg disabled:opacity-50"
            >
              {{ resetting ? t('common.loading') : t('admin.users.resetPassword') }}
            </button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import api from '@/services/api'

const { t } = useI18n()

// State
const loading = ref(true)
const error = ref<string | null>(null)
const users = ref<any[]>([])
const teams = ref<any[]>([])

// Modals
const showAddModal = ref(false)
const showEditModal = ref(false)
const showResetModal = ref(false)
const editingUser = ref<any | null>(null)
const resetUser = ref<any | null>(null)

// Loading states
const adding = ref(false)
const saving = ref(false)
const resetting = ref(false)

// Forms
const addForm = reactive({
  email: '',
  name: '',
  password: '',
  roles: ['Worker'] as string[],
  teamId: null as string | null
})

const editForm = reactive({
  name: '',
  roles: [] as string[],
  teamId: null as string | null
})

const newPassword = ref('')
const confirmPassword = ref('')

// Available roles
const availableRoles = [
  'Admin',
  'Management',
  'TeamLead',
  'Accountant',
  'Warehouse',
  'Worker'
]

// Methods
const loadUsers = async () => {
  loading.value = true
  error.value = null
  try {
    const data = await api.get<any[]>('/api/users')
    users.value = data
  } catch (e: any) {
    error.value = e.response?.data?.error || t('common.loadError')
  } finally {
    loading.value = false
  }
}

const loadTeams = async () => {
  try {
    const data = await api.get<any[]>('/api/teams')
    console.log('Loaded teams:', data)
    teams.value = data
  } catch (e: any) {
    console.error('Failed to load teams', e)
  }
}

const getInitials = (name: string) => {
  if (!name) return '?'
  return name.split(' ').map(n => n[0]).join('').toUpperCase().substring(0, 2)
}

const roleClass = (role: string) => {
  const classes: Record<string, string> = {
    'Admin': 'bg-red-100 text-red-800',
    'Management': 'bg-purple-100 text-purple-800',
    'TeamLead': 'bg-blue-100 text-blue-800',
    'Accountant': 'bg-green-100 text-green-800',
    'Warehouse': 'bg-orange-100 text-orange-800',
    'Worker': 'bg-gray-100 text-gray-800'
  }
  return classes[role] || 'bg-gray-100 text-gray-800'
}

const openAddModal = () => {
  addForm.email = ''
  addForm.name = ''
  addForm.password = ''
  addForm.roles = ['Worker']
  addForm.teamId = null
  showAddModal.value = true
}

const addUser = async () => {
  adding.value = true
  try {
    await api.post('/api/users', {
      email: addForm.email,
      name: addForm.name,
      password: addForm.password,
      roles: addForm.roles,
      teamId: addForm.teamId
    })
    showAddModal.value = false
    await loadUsers()
  } catch (e: any) {
    alert(e.response?.data?.error || e.response?.data?.message || t('common.saveError'))
  } finally {
    adding.value = false
  }
}

const openEditModal = (user: any) => {
  editingUser.value = user
  editForm.name = user.name || ''
  editForm.roles = [...(user.roles || [])]
  // Důležité: pokud user nemá teamId, nastavíme null (ne prázdný string)
  editForm.teamId = user.teamId || null
  showEditModal.value = true
}

const saveUser = async () => {
  if (!editingUser.value) return
  saving.value = true
  try {
    console.log('Saving user with teamId:', editForm.teamId)
    await api.put(`/api/users/${editingUser.value.id}`, {
      name: editForm.name || null,
      roles: editForm.roles,
      teamId: editForm.teamId  // null = bez týmu
    })
    showEditModal.value = false
    await loadUsers()
  } catch (e: any) {
    alert(e.response?.data?.error || t('common.saveError'))
  } finally {
    saving.value = false
  }
}

const toggleLockout = async (user: any) => {
  const action = user.isLockedOut ? t('admin.users.unlock') : t('admin.users.lock')
  if (!confirm(`${action} ${t('admin.users.user').toLowerCase()} ${user.email}?`)) return
  
  try {
    await api.put(`/api/users/${user.id}`, {
      lockout: !user.isLockedOut
    })
    await loadUsers()
  } catch (e: any) {
    alert(e.response?.data?.error || t('common.saveError'))
  }
}

const openResetPasswordModal = (user: any) => {
  resetUser.value = user
  newPassword.value = ''
  confirmPassword.value = ''
  showResetModal.value = true
}

const resetPassword = async () => {
  if (!resetUser.value || newPassword.value !== confirmPassword.value) return
  resetting.value = true
  try {
    await api.post(`/api/users/${resetUser.value.id}/reset-password`, {
      newPassword: newPassword.value
    })
    alert(t('admin.users.passwordChanged'))
    showResetModal.value = false
  } catch (e: any) {
    alert(e.response?.data?.error || t('common.saveError'))
  } finally {
    resetting.value = false
  }
}

const deleteUser = async (user: any) => {
  if (!confirm(`${t('admin.users.confirmDeleteUser')} ${user.email}?`)) return
  try {
    await api.delete(`/api/users/${user.id}`)
    await loadUsers()
  } catch (e: any) {
    alert(e.response?.data?.error || t('common.deleteError'))
  }
}

// Lifecycle
onMounted(() => {
  loadUsers()
  loadTeams()
})
</script>