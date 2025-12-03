import { createApp } from 'vue'
import { createPinia } from 'pinia'
import { createI18n } from 'vue-i18n'
import router from './router'
import App from './App.vue'
import './assets/main.css'

// Import lokalizačních souborů
import cs from './locales/cs.json'
import en from './locales/en.json'

// Vytvoření i18n instance
const i18n = createI18n({
  legacy: false,
  locale: localStorage.getItem('language') || 'cs',
  fallbackLocale: 'en',
  messages: {
    cs,
    en
  }
})

const app = createApp(App)

app.use(createPinia())
app.use(router)
app.use(i18n)

app.mount('#app')
