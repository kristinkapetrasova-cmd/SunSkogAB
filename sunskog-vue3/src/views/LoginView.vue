<template>
  <div class="min-h-screen flex items-center justify-center bg-sunskog-primary">
    <div class="bg-white p-8 rounded-lg shadow-2xl w-full max-w-md">
      <!-- Logo -->
      <div class="text-center mb-8">
        <img 
          src="https://img1.wsimg.com/isteam/ip/1375172e-960f-41f2-86bf-79b399fd1b28/blob-723068a.png/:/rs=w:300,h:71,cg:true,m/cr=w:300,h:71/qt=q:95" 
          alt="SunSkog AB" 
          class="mx-auto mb-4 h-16"
        />
        <p class="text-gray-600 mt-2">{{ t('auth.login') }}</p>
      </div>

      <!-- Error Message - zůstane viditelná 30 sekund -->
      <div 
        v-if="localError" 
        class="mb-4 p-3 bg-red-100 border border-red-400 text-red-700 rounded flex justify-between items-center"
      >
        <span>{{ localError }}</span>
        <button @click="localError = ''" class="text-red-700 hover:text-red-900">
          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
          </svg>
        </button>
      </div>

      <!-- Login Form -->
      <form @submit.prevent="handleLogin" class="space-y-6">
        <div>
          <label for="email" class="block text-sm font-medium text-gray-700 mb-2">
            {{ t('auth.email') }}
          </label>
          <input
            id="email"
            v-model="credentials.email"
            type="email"
            required
            class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sunskog-primary focus:border-transparent"
            :placeholder="t('auth.email')"
          />
        </div>

        <div>
          <label for="password" class="block text-sm font-medium text-gray-700 mb-2">
            {{ t('auth.password') }}
          </label>
          <input
            id="password"
            v-model="credentials.password"
            type="password"
            required
            class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sunskog-primary focus:border-transparent"
            :placeholder="t('auth.password')"
          />
        </div>

        <button
          type="submit"
          :disabled="authStore.loading"
          class="w-full bg-sunskog-primary hover:bg-sunskog-hover text-white font-semibold py-3 rounded-lg transition duration-200 disabled:opacity-50 disabled:cursor-not-allowed"
        >
          <span v-if="!authStore.loading">{{ t('auth.loginButton') }}</span>
          <span v-else>{{ t('common.loading') }}</span>
        </button>
      </form>

      <!-- Language Switcher -->
      <div class="mt-6 flex justify-center space-x-4">
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
  </div>
</template>

<script setup lang="ts">
import { ref, onUnmounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth'

const router = useRouter()
const { t, locale } = useI18n()
const authStore = useAuthStore()

const credentials = ref({
  email: '',
  password: ''
})

const localError = ref('')

// Uložit jazyk do localStorage
const setLocale = (lang: string) => {
  locale.value = lang
  localStorage.setItem('sunskog_locale', lang)
}

// Načíst jazyk při startu
const savedLocale = localStorage.getItem('sunskog_locale')
if (savedLocale) {
  locale.value = savedLocale
}

const handleLogin = async () => {
  localError.value = ''
  
  const success = await authStore.login(credentials.value)
  
  if (success) {
    router.push('/app/dashboard')
  } else {
    // Zobrazit chybu v aktuálním jazyce - ZŮSTANE dokud není úspěšné přihlášení
    localError.value = authStore.error === 'auth.loginError' 
      ? t('auth.loginError') 
      : (authStore.error || t('auth.loginError'))
    
    // Vyčistit chybu v store
    authStore.clearError()
  }
}

onUnmounted(() => {
  // Už nepotřebujeme timeout
})
</script>
