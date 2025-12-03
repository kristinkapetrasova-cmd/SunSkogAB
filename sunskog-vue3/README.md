# SunSkog Vue 3 - Migrace z Reactu

Tento projekt je kompletní Vue 3 migrace původní React aplikace SunSkog.

## 🚀 Jak začít

### 1. Instalace závislostí

```bash
npm install
```

### 2. Spuštění dev serveru

```bash
npm run dev
```

Aplikace poběží na `http://localhost:3000`

### 3. Build pro produkci

```bash
npm run build
```

## 📁 Struktura projektu

```
sunskog-vue3/
├── src/
│   ├── assets/          # CSS, obrázky
│   ├── components/      # Znovupoužitelné komponenty
│   ├── views/           # Stránky
│   │   ├── admin/       # Admin stránky
│   │   ├── AppLayout.vue
│   │   ├── DashboardView.vue
│   │   ├── LoginView.vue
│   │   └── ...
│   ├── stores/          # Pinia stores (state management)
│   ├── services/        # API služby
│   ├── router/          # Vue Router konfigurace
│   ├── types/           # TypeScript typy
│   ├── locales/         # Překlady (CS/EN)
│   ├── App.vue
│   └── main.ts
├── package.json
├── vite.config.ts
├── tsconfig.json
└── tailwind.config.js
```

## 🎯 Co je hotové

✅ **Základní struktura**
- Vite + TypeScript konfigurace
- Vue Router s protected routes
- Pinia store pro autentizaci
- Tailwind CSS s SunSkog barvami
- i18n pro češtinu/angličtinu

✅ **Komponenty**
- LoginView - přihlašovací stránka
- AppLayout - hlavní layout s navigací
- DashboardView - hlavní stránka

✅ **Služby**
- API service s axios
- Auth service
- Error handling
- Token management

## 📝 Co je třeba převést

❌ **Stránky k migraci:**
1. TimesheetsView - výkazy
2. InventoryView - inventář + QR kódy
3. Admin/UsersView - správa uživatelů
4. Admin/ReportsView - reporty

## 🔄 Jak migrovat React komponentu na Vue

### React → Vue syntaxe

**1. State management:**

React:
```jsx
const [count, setCount] = useState(0)
setCount(count + 1)
```

Vue:
```vue
<script setup>
import { ref } from 'vue'
const count = ref(0)
count.value++
</script>
```

**2. useEffect → Vue lifecycle:**

React:
```jsx
useEffect(() => {
  fetchData()
}, [])
```

Vue:
```vue
<script setup>
import { onMounted } from 'vue'
onMounted(() => {
  fetchData()
})
</script>
```

**3. Props:**

React:
```jsx
function MyComponent({ title, count }) {
  return <div>{title}: {count}</div>
}
```

Vue:
```vue
<template>
  <div>{{ title }}: {{ count }}</div>
</template>

<script setup lang="ts">
defineProps<{
  title: string
  count: number
}>()
</script>
```

**4. Events:**

React:
```jsx
<button onClick={() => handleClick()}>Click</button>
```

Vue:
```vue
<button @click="handleClick">Click</button>
```

**5. Context → Pinia:**

React:
```jsx
const { user } = useAuth()
```

Vue:
```vue
<script setup>
import { useAuthStore } from '@/stores/auth'
const authStore = useAuthStore()
const user = authStore.user
</script>
```

## 🎨 Design system

Barvy jsou již nakonfigurovány v Tailwind:
- `sunskog-green` - #8AA626
- `sunskog-blue` - #3B7EA1
- `sunskog-dark` - #1A1A2E
- `sunskog-yellow` - #F4C430

Použití:
```vue
<div class="bg-sunskog-green text-white">
  Zelené pozadí
</div>
```

## 🔌 API integrace

API služba je připravená v `src/services/api.ts`:

```typescript
import api from '@/services/api'

// GET request
const data = await api.get('/endpoint')

// POST request
await api.post('/endpoint', { data })

// PUT request
await api.put('/endpoint', { data })

// DELETE request
await api.delete('/endpoint')
```

Token se automaticky přidává do každého požadavku.

## 🌍 Lokalizace

Překlady jsou v `src/locales/`:
- `cs.json` - čeština
- `en.json` - angličtina

Použití v komponentě:
```vue
<script setup>
import { useI18n } from 'vue-i18n'
const { t, locale } = useI18n()
</script>

<template>
  <p>{{ t('common.save') }}</p>
  <button @click="locale = 'en'">EN</button>
</template>
```

## 🧪 Testování

Pro otestování přihlášení použijte:
- Username: `admin`
- Password: `admin123`

(Po připojení backendu)

## 📚 Další kroky

1. **Převést TimesheetsView** - začni touto stránkou
2. **Převést InventoryView** - zahrnuje QR kódy
3. **Převést Admin stránky**
4. **Přidat testy**

## 🆘 Pomoc

Pokud potřebuješ pomoc s převodem konkrétní komponenty:
1. Podívej se na příklady v `DashboardView.vue`
2. Koukni do oficiální Vue dokumentace
3. Porovnej React a Vue syntaxi výše

## 🔗 Odkazy

- [Vue 3 Dokumentace](https://vuejs.org/)
- [Pinia](https://pinia.vuejs.org/)
- [Vue Router](https://router.vuejs.org/)
- [Vite](https://vitejs.dev/)
