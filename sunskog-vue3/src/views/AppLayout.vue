<template>
  <div class="flex h-screen bg-gray-100">
    <!-- Sidebar -->
    <aside
      :class="[
        'bg-sunskog-primary text-white transition-all duration-300 flex flex-col',
        sidebarOpen ? 'w-64' : 'w-20'
      ]"
    >
      <!-- Logo -->
      <div class="p-4 flex items-center justify-between border-b border-red-800">
        <div v-if="sidebarOpen" class="flex items-center">
          <img 
            src="https://img1.wsimg.com/isteam/ip/1375172e-960f-41f2-86bf-79b399fd1b28/blob-723068a.png/:/rs=w:200,h:47,cg:true,m/cr=w:200,h:47/qt=q:95" 
            alt="SunSkog AB" 
            class="h-10"
          />
        </div>
        <button
          @click="toggleSidebar"
          :class="[
            'p-2 hover:bg-red-800 rounded transition',
            !sidebarOpen ? 'mx-auto' : ''
          ]"
        >
          <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16" />
          </svg>
        </button>
      </div>

      <!-- Navigation -->
      <nav class="p-4 space-y-2 flex-1">
        <RouterLink
          v-for="item in navItems"
          :key="item.path"
          :to="item.path"
          class="flex items-center space-x-3 p-3 rounded-lg hover:bg-red-800 transition"
          active-class="bg-red-900"
        >
          <svg class="w-6 h-6 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" :d="item.icon" />
          </svg>
          <span v-if="sidebarOpen">{{ t(item.label) }}</span>
        </RouterLink>
      </nav>

      <!-- User Section - vždy viditelné -->
      <div class="p-4 border-t border-red-800">
        <!-- Jméno a email (jen při rozbaleném menu) -->
        <div v-if="sidebarOpen" class="mb-3">
          <p class="font-semibold truncate">{{ authStore.user?.name || authStore.user?.fullName || t('admin.users.unnamed') }}</p>
          <p class="text-sm text-red-200 truncate">{{ authStore.user?.email }}</p>
        </div>
        
        <!-- Odhlášení - vždy viditelné -->
        <button
          @click="handleLogout"
          :class="[
            'flex items-center p-2 hover:bg-red-800 rounded transition w-full',
            sidebarOpen ? 'space-x-3' : 'justify-center'
          ]"
          :title="t('common.logout')"
        >
          <svg class="w-6 h-6 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1" />
          </svg>
          <span v-if="sidebarOpen">{{ t('common.logout') }}</span>
        </button>
      </div>
    </aside>

    <!-- Main Content -->
    <div class="flex-1 flex flex-col overflow-hidden">
      <!-- Header -->
      <header class="bg-white shadow-sm">
        <div class="flex items-center justify-between px-6 py-4">
          <h2 class="text-2xl font-bold text-sunskog-dark">
            {{ currentPageTitle }}
          </h2>

          <!-- Language Switcher -->
          <div class="flex items-center space-x-2">
            <button
              @click="setLocale('cs')"
              :class="[
                'px-3 py-1 rounded',
                locale === 'cs' ? 'bg-sunskog-primary text-white' : 'text-gray-600 hover:bg-gray-100'
              ]"
            >
              CZ
            </button>
            <button
              @click="setLocale('en')"
              :class="[
                'px-3 py-1 rounded',
                locale === 'en' ? 'bg-sunskog-primary text-white' : 'text-gray-600 hover:bg-gray-100'
              ]"
            >
              EN
            </button>
          </div>
        </div>
      </header>

      <!-- Page Content -->
      <main class="flex-1 overflow-y-auto p-6">
        <RouterView />
      </main>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import { RouterLink, RouterView, useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth'

const route = useRoute()
const router = useRouter()
const { t, locale } = useI18n()
const authStore = useAuthStore()

// Načíst jazyk z localStorage
onMounted(() => {
  const savedLocale = localStorage.getItem('sunskog_locale')
  if (savedLocale) {
    locale.value = savedLocale
  }
})

// Uložit jazyk do localStorage
const setLocale = (lang: string) => {
  locale.value = lang
  localStorage.setItem('sunskog_locale', lang)
}

// Menu je otevřené jen na hlavní stránce
const sidebarOpen = ref(route.name === 'Dashboard')

// Sledovat změny routy a automaticky schovat/zobrazit menu
watch(() => route.name, (newRouteName) => {
  sidebarOpen.value = newRouteName === 'Dashboard'
})

const toggleSidebar = () => {
  sidebarOpen.value = !sidebarOpen.value
}

// Navigation items based on roles
const navItems = computed(() => {
  const items = [
    { path: '/app/dashboard', label: 'nav.home', icon: 'M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6' },
  ]

  // Výkazy - všichni kromě čistého managementu
  if (authStore.canSubmitTimesheets || authStore.canViewAllTimesheets) {
    items.push({ path: '/app/timesheets', label: 'nav.timesheets', icon: 'M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z' })
  }

  // Sklad - jen s oprávněním
  if (authStore.canAccessWarehouse) {
    items.push({ path: '/app/warehouse', label: 'nav.warehouse', icon: 'M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4' })
  }

  // Admin menu
  if (authStore.canManageUsers) {
    items.push({ path: '/app/admin/users', label: 'nav.users', icon: 'M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z' })
  }
  
  // Týmy - admin a management
  if (authStore.canManageUsers || authStore.isManagement) {
    items.push({ path: '/app/admin/teams', label: 'nav.teams', icon: 'M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z' })
  }

  // Sazby - admin (read-only) a management (editovatelné)
  if (authStore.canViewRates) {
    items.push({ path: '/app/admin/rates', label: 'nav.rates', icon: 'M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z' })
  }

  // Reporty - admin, management, účetní
  if (authStore.isAdmin || authStore.isManagement || authStore.isAccountant) {
    items.push({ path: '/app/admin/reports', label: 'nav.reports', icon: 'M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z' })
  }

  return items
})

// Current page title
const currentPageTitle = computed(() => {
  const routeName = route.name as string
  const titleMap: Record<string, string> = {
    'Dashboard': 'nav.home',
    'Timesheets': 'nav.timesheets',
    'Warehouse': 'nav.warehouse',
    'AdminUsers': 'nav.users',
    'AdminRates': 'nav.rates',
    'AdminReports': 'nav.reports'
  }
  return t(titleMap[routeName] || 'nav.home')
})

const handleLogout = () => {
  authStore.logout()
  router.push('/login')
}
</script>