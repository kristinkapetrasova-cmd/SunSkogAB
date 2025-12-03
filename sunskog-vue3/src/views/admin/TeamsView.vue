<template>
  <div class="space-y-6">
    <!-- Header -->
    <div class="flex justify-between items-center">
      <h1 class="text-2xl font-bold text-sunskog-dark">{{ t('teams.title') }}</h1>
      <button
        @click="openTeamModal()"
        class="bg-sunskog-primary hover:bg-sunskog-hover text-white px-4 py-2 rounded-lg flex items-center space-x-2"
      >
        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
        </svg>
        <span>{{ t('teams.addTeam') }}</span>
      </button>
    </div>

    <!-- Loading -->
    <div v-if="loading" class="bg-white rounded-lg shadow p-8 text-center">
      <div class="animate-spin w-8 h-8 border-4 border-sunskog-primary border-t-transparent rounded-full mx-auto"></div>
      <p class="mt-4 text-gray-600">{{ t('common.loading') }}</p>
    </div>

    <!-- Teams List -->
    <div v-else class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
      <div
        v-for="team in teams"
        :key="team.id"
        class="bg-white rounded-lg shadow hover:shadow-lg transition p-6 cursor-pointer"
        @click="openTeamDetail(team)"
      >
        <div class="flex justify-between items-start mb-4">
          <div class="flex-1">
            <h3 class="text-lg font-bold text-sunskog-dark">{{ team.name }}</h3>
          </div>
          <button
            @click.stop="deleteTeam(team)"
            class="text-red-600 hover:text-red-800 ml-2"
          >
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
            </svg>
          </button>
        </div>
        
        <div class="flex items-center justify-between text-sm">
          <div class="flex items-center text-gray-600">
            <svg class="w-4 h-4 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z" />
            </svg>
            {{ team.memberCount }} {{ t('teams.members') }}
          </div>
          <div v-if="team.leadUserName" class="flex items-center text-sunskog-primary">
            <svg class="w-4 h-4 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 3v4M3 5h4M6 17v4m-2-2h4m5-16l2.286 6.857L21 12l-5.714 2.143L13 21l-2.286-6.857L5 12l5.714-2.143L13 3z" />
            </svg>
            {{ team.leadUserName }}
          </div>
        </div>
      </div>

      <!-- Empty state -->
      <div v-if="teams.length === 0" class="col-span-full bg-white rounded-lg shadow p-8 text-center">
        <svg class="w-16 h-16 mx-auto text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z" />
        </svg>
        <p class="mt-4 text-gray-600">{{ t('teams.noTeams') }}</p>
      </div>
    </div>

    <!-- Modal: Add/Edit Team -->
    <div v-if="showTeamModal" class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
      <div class="bg-white rounded-lg shadow-xl w-full max-w-md p-6">
        <div class="flex justify-between items-center mb-4">
          <h2 class="text-xl font-bold text-sunskog-dark">
            {{ editingTeam ? t('teams.editTeam') : t('teams.addTeam') }}
          </h2>
          <button @click="showTeamModal = false" class="text-gray-500 hover:text-gray-700">
            <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        <form @submit.prevent="saveTeam" class="space-y-4">
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">{{ t('teams.name') }}</label>
            <input
              v-model="teamForm.name"
              type="text"
              required
              class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sunskog-primary"
            />
          </div>

          <div class="flex justify-end space-x-3">
            <button
              type="button"
              @click="showTeamModal = false"
              class="px-4 py-2 border border-gray-300 rounded-lg text-gray-700 hover:bg-gray-50"
            >
              {{ t('common.cancel') }}
            </button>
            <button
              type="submit"
              class="px-4 py-2 bg-sunskog-primary hover:bg-sunskog-hover text-white rounded-lg"
            >
              {{ t('common.save') }}
            </button>
          </div>
        </form>
      </div>
    </div>

    <!-- Modal: Team Detail -->
    <div v-if="showDetailModal && selectedTeam" class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
      <div class="bg-white rounded-lg shadow-xl w-full max-w-2xl p-6 max-h-[90vh] overflow-y-auto">
        <div class="flex justify-between items-center mb-6">
          <div>
            <h2 class="text-2xl font-bold text-sunskog-dark">{{ selectedTeam.name }}</h2>
          </div>
          <button @click="showDetailModal = false" class="text-gray-500 hover:text-gray-700">
            <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        <!-- Add Member Form -->
        <div class="bg-gray-50 rounded-lg p-4 mb-6">
          <h3 class="font-semibold text-gray-900 mb-3">{{ t('teams.addMember') }}</h3>
          <div class="flex space-x-2">
            <select
              v-model="memberForm.userId"
              class="flex-1 px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sunskog-primary"
            >
              <option value="">{{ t('teams.selectUser') }}</option>
              <option v-for="user in allUsers" :key="user.id" :value="user.id">
                {{ user.name }}
              </option>
            </select>
            <button
              type="button"
              @click="addMember"
              :disabled="!memberForm.userId"
              class="px-4 py-2 bg-sunskog-primary hover:bg-sunskog-hover text-white rounded-lg disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {{ t('teams.add') }}
            </button>
          </div>
        </div>

        <!-- Members List -->
        <div>
          <h3 class="font-semibold text-gray-900 mb-3">{{ t('teams.members') }} ({{ members.length }})</h3>
          <div class="space-y-2">
            <div
              v-for="member in members"
              :key="member.id"
              class="flex items-center justify-between p-3 bg-gray-50 rounded-lg hover:bg-gray-100"
            >
              <div class="flex items-center space-x-3">
                <div class="w-10 h-10 bg-sunskog-primary rounded-full flex items-center justify-center text-white font-bold">
                  {{ getInitials(member.userName) }}
                </div>
                <div>
                  <p class="font-medium text-gray-900">{{ member.userName }}</p>
                  <p class="text-sm text-gray-500">{{ member.email }}</p>
                </div>
                <span v-if="isLeader(member.userId)" class="px-2 py-1 bg-yellow-100 text-yellow-800 text-xs font-medium rounded-full">
                  {{ t('teams.leader') }}
                </span>
              </div>
              <div class="flex items-center space-x-2">
                <button
                  @click="removeMember(member)"
                  class="text-red-600 hover:text-red-800"
                  :title="t('teams.remove')"
                >
                  <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                  </svg>
                </button>
              </div>
            </div>

            <div v-if="members.length === 0" class="text-center py-8 text-gray-500">
              {{ t('teams.noMembers') }}
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Modal: Přesun uživatele -->
    <div v-if="showTransferModal" class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
      <div class="bg-white rounded-lg shadow-xl w-full max-w-md p-6">
        <div class="flex justify-between items-center mb-4">
          <h2 class="text-xl font-bold text-sunskog-dark">{{ t('teams.transferUser') }}</h2>
          <button @click="showTransferModal = false" class="text-gray-500 hover:text-gray-700">
            <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        <div class="mb-6">
          <p class="text-gray-700">
            {{ t('teams.userInOtherTeam', { name: transferInfo.userName, team: transferInfo.currentTeamName }) }}
          </p>
          <p class="text-gray-600 mt-2">
            {{ t('teams.transferQuestion') }}
          </p>
        </div>

        <div class="flex justify-end space-x-3">
          <button
            type="button"
            @click="showTransferModal = false"
            class="px-4 py-2 border border-gray-300 rounded-lg text-gray-700 hover:bg-gray-50"
          >
            {{ t('common.cancel') }}
          </button>
          <button
            type="button"
            @click="confirmTransfer"
            class="px-4 py-2 bg-sunskog-primary hover:bg-sunskog-hover text-white rounded-lg"
          >
            {{ t('teams.transfer') }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted, computed } from 'vue'
import { useI18n } from 'vue-i18n'
import api from '@/services/api'

const { t } = useI18n()

const loading = ref(false)
const teams = ref<any[]>([])
const allUsers = ref<any[]>([])

const showTeamModal = ref(false)
const showDetailModal = ref(false)
const showTransferModal = ref(false)
const editingTeam = ref<any | null>(null)
const selectedTeam = ref<any | null>(null)
const teamDetail = ref<any | null>(null)

const teamForm = reactive({
  name: ''
})

const memberForm = reactive({
  userId: ''
})

const transferInfo = reactive({
  userId: '',
  userName: '',
  currentTeamId: '',
  currentTeamName: ''
})

// Computed pro členy - normalizuje data z API
const members = computed(() => {
  if (!teamDetail.value) return []
  // API vrací "members" (camelCase z ASP.NET Core)
  const rawMembers = teamDetail.value.members || teamDetail.value.Members || []
  console.log('Raw members:', rawMembers)
  return rawMembers
})

// Kontrola zda je uživatel vedoucí
const isLeader = (userId: string) => {
  return teamDetail.value?.leadUserId === userId
}

const loadTeams = async () => {
  loading.value = true
  try {
    const response = await api.get('/api/teams')
    console.log('Teams response:', response)
    teams.value = response
  } catch (e: any) {
    console.error('Error loading teams:', e)
    alert(e.response?.data?.message || t('common.loadError'))
  } finally {
    loading.value = false
  }
}

const loadUsers = async () => {
  try {
    const response = await api.get<any[]>('/api/employees')
    allUsers.value = response.map(u => ({
      id: u.id,
      name: u.name || u.email,
      email: u.email
    }))
  } catch (e: any) {
    console.error('Failed to load users', e)
  }
}

const openTeamModal = (team?: any) => {
  editingTeam.value = team || null
  teamForm.name = team?.name || ''
  showTeamModal.value = true
}

const openTeamDetail = async (team: any) => {
  selectedTeam.value = team
  showDetailModal.value = true
  try {
    const response = await api.get(`/api/teams/${team.id}`)
    console.log('Team detail response:', response)
    teamDetail.value = response
  } catch (e: any) {
    console.error('Error loading team detail:', e)
    alert(e.response?.data?.message || t('common.loadError'))
  }
}

const saveTeam = async () => {
  try {
    if (editingTeam.value) {
      await api.put(`/api/teams/${editingTeam.value.id}`, teamForm)
    } else {
      await api.post('/api/teams', teamForm)
    }
    showTeamModal.value = false
    await loadTeams()
  } catch (e: any) {
    alert(e.response?.data?.message || t('common.saveError'))
  }
}

const deleteTeam = async (team: any) => {
  if (!confirm(t('teams.confirmDelete', { name: team.name }))) return
  
  try {
    console.log('Deleting team:', team.id)
    await api.delete(`/api/teams/${team.id}`)
    await loadTeams()
  } catch (e: any) {
    console.error('Delete team error:', e.response?.data || e)
    alert(e.response?.data?.error || e.response?.data?.message || t('common.deleteError'))
  }
}

const addMember = async () => {
  if (!selectedTeam.value || !memberForm.userId) return
  
  try {
    await api.post(`/api/teams/${selectedTeam.value.id}/members`, {
      userId: memberForm.userId
    })
    memberForm.userId = ''
    await openTeamDetail(selectedTeam.value)
    await loadTeams()
  } catch (e: any) {
    console.error('Add member error:', e.response?.data)
    const errorData = e.response?.data
    
    if (errorData?.error === 'UserAlreadyInTeam') {
      alert(t('teams.userAlreadyInTeam'))
    } else if (errorData?.error === 'UserInOtherTeam') {
      // Uživatel je v jiném týmu - nabídnout přesun
      const user = allUsers.value.find(u => u.id === memberForm.userId)
      transferInfo.userId = memberForm.userId
      transferInfo.userName = user?.name || ''
      transferInfo.currentTeamId = errorData.currentTeamId
      transferInfo.currentTeamName = errorData.currentTeamName
      showTransferModal.value = true
    } else {
      alert(errorData?.message || t('teams.addMemberError'))
    }
  }
}

const confirmTransfer = async () => {
  if (!selectedTeam.value || !transferInfo.userId) return
  
  try {
    await api.put(`/api/teams/${selectedTeam.value.id}/members/${transferInfo.userId}/transfer`, {})
    showTransferModal.value = false
    memberForm.userId = ''
    await openTeamDetail(selectedTeam.value)
    await loadTeams()
  } catch (e: any) {
    console.error('Transfer error:', e)
    alert(e.response?.data?.message || t('teams.transferError'))
  }
}

const removeMember = async (member: any) => {
  if (!selectedTeam.value) return
  if (!confirm(t('teams.confirmRemoveMember'))) return
  
  try {
    console.log('Removing member:', member)
    console.log('Team ID:', selectedTeam.value.id)
    console.log('User ID:', member.userId)
    // Používáme userId (ne membershipId)
    await api.delete(`/api/teams/${selectedTeam.value.id}/members/${member.userId}`)
    await openTeamDetail(selectedTeam.value)
    await loadTeams()
  } catch (e: any) {
    console.error('Remove member error:', e.response?.data || e)
    alert(e.response?.data?.error || e.response?.data?.message || t('teams.removeMemberError'))
  }
}

const getInitials = (name: string) => {
  if (!name) return '?'
  return name.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2)
}

onMounted(async () => {
  await loadTeams()
  await loadUsers()
})
</script>