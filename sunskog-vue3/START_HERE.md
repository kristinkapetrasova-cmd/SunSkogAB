# 🎉 SunSkog Vue 3 - Připraveno k použití!

## ✅ Co je hotové

### 📦 Kompletní projekt struktura
- ✅ Vite + TypeScript konfigurace
- ✅ Vue Router s protected routes
- ✅ Pinia store (state management)
- ✅ Tailwind CSS s SunSkog barvami
- ✅ i18n (CZ/EN)
- ✅ API služby s axios
- ✅ Auth system

### 🎨 Hotové komponenty
- ✅ **LoginView** - plně funkční přihlašovací stránka
- ✅ **AppLayout** - hlavní layout s navigací a menu
- ✅ **DashboardView** - hlavní stránka s kartami a statistikami

### 📄 Dokumentace
- ✅ **README.md** - kompletní dokumentace projektu
- ✅ **MIGRATION_GUIDE.md** - detailní průvodce převodem React → Vue
- ✅ **EXAMPLE_TIMESHEETS.md** - konkrétní příklad převodu komponenty

---

## 🚀 Jak začít

### 1. Zkopíruj projekt
Zkopíruj celou složku `sunskog-vue3` do tvého pracovního adresáře:
```
C:\Users\krist\Desktop\SunSkogAB\sunskog-vue3\
```

### 2. Nainstaluj závislosti
```bash
cd sunskog-vue3
npm install
```

### 3. Spusť development server
```bash
npm run dev
```

Aplikace poběží na `http://localhost:3000`

---

## 📋 Další kroky - Co převést

### Fáze 1: Timesheets (Výkazy) ⏱️
**Priorita: VYSOKÁ**

Soubory k převodu:
- `frontend/src/pages/TimesheetsPage.tsx` → `src/views/TimesheetsView.vue`
- `frontend/src/components/TimesheetForm.tsx` → `src/components/TimesheetModal.vue`

**Postup:**
1. Otevři `EXAMPLE_TIMESHEETS.md` - tam máš kompletní příklad
2. Vytvoř `TimesheetsView.vue` podle příkladu
3. Vytvoř `TimesheetModal.vue` pro formulář
4. Připoj API endpointy
5. Otestuj funkcionalitu

**Odhadovaný čas:** 3-4 hodiny

---

### Fáze 2: Inventory (Inventář + QR kódy) 📦
**Priorita: VYSOKÁ**

Soubory k převodu:
- `frontend/src/pages/InventoryPage.tsx` → `src/views/InventoryView.vue`
- `frontend/src/components/QRCodeGenerator.tsx` → `src/components/QRCodeGenerator.vue`

**Co je specifické:**
- QR kódy: použij knihovnu `qrcode` (už je v package.json)
- Generování QR kódu pro každou položku
- Modal pro zobrazení QR kódu

**Příklad použití qrcode v Vue:**
```vue
<script setup lang="ts">
import QRCode from 'qrcode'
import { ref } from 'vue'

const qrCodeUrl = ref('')

const generateQR = async (text: string) => {
  qrCodeUrl.value = await QRCode.toDataURL(text)
}
</script>

<template>
  <img :src="qrCodeUrl" alt="QR Code" />
</template>
```

**Odhadovaný čas:** 3-4 hodiny

---

### Fáze 3: Admin - Users (Správa uživatelů) 👥
**Priorita: STŘEDNÍ**

Soubory k převodu:
- `frontend/src/pages/admin/AdminUsersPage.tsx` → `src/views/admin/UsersView.vue`

**Co zahrnuje:**
- Seznam uživatelů
- Přidání nového uživatele
- Úprava uživatele
- Smazání uživatele
- Role management

**Odhadovaný čas:** 2-3 hodiny

---

### Fáze 4: Admin - Reports (Reporty) 📊
**Priorita: STŘEDNÍ**

Soubory k převodu:
- `frontend/src/pages/admin/ReportsPage.tsx` → `src/views/admin/ReportsView.vue`

**Co zahrnuje:**
- Generování reportů
- Filtrování dat
- Export do CSV/PDF

**Odhadovaný čas:** 2-3 hodiny

---

## 🛠️ Jak převést komponentu - Quick Guide

### Krok 1: Vytvoř nový .vue soubor
```vue
<template>
  <!-- HTML zde -->
</template>

<script setup lang="ts">
// TypeScript logika zde
</script>
```

### Krok 2: Převeď state
```typescript
// React
const [count, setCount] = useState(0)

// Vue
const count = ref(0)
count.value++ // přístup přes .value
```

### Krok 3: Převeď lifecycle
```typescript
// React
useEffect(() => {
  fetchData()
}, [])

// Vue
onMounted(() => {
  fetchData()
})
```

### Krok 4: Převeď template
```jsx
// React JSX
<button onClick={() => handleClick()}>
  {loading ? 'Loading...' : 'Click'}
</button>

// Vue template
<button @click="handleClick">
  {{ loading ? 'Loading...' : 'Click' }}
</button>
```

---

## 📚 Užitečné soubory

1. **README.md** - kompletní dokumentace
2. **MIGRATION_GUIDE.md** - detailní převodní příručka
3. **EXAMPLE_TIMESHEETS.md** - konkrétní příklad

---

## 🎯 Doporučené pořadí práce

1. **TEĎKA:** Nastuduj `MIGRATION_GUIDE.md`
2. **TEĎKA:** Podívej se na `EXAMPLE_TIMESHEETS.md`
3. **TEĎKA:** Spusť projekt (`npm install` → `npm run dev`)
4. **DNES:** Převeď TimesheetsView
5. **DNES/ZÍTRA:** Převeď InventoryView
6. **PŘÍŠTÍ TÝDEN:** Převeď Admin stránky

---

## 💡 Tipy pro úspěch

### ✅ DO:
- Postupuj po malých krocích
- Testuj každou změnu
- Používej TypeScript (chytá chyby)
- Sleduj konzoli pro chyby
- Ptej se, když něco není jasné

### ❌ DON'T:
- Nepřepisuj všechno najednou
- Neškrtej TypeScript typy
- Nepřeskakuj testování
- Nezapomínej na `.value` u ref()

---

## 🆘 Když něco nefunguje

### Běžné chyby:

**1. Zapomenutí .value**
```typescript
// ❌ ŠPATNĚ
const count = ref(0)
count++

// ✅ SPRÁVNĚ
const count = ref(0)
count.value++
```

**2. Props jsou readonly**
```vue
<!-- ❌ ŠPATNĚ -->
<script setup>
const props = defineProps<{ count: number }>()
props.count++ // CHYBA!
</script>

<!-- ✅ SPRÁVNĚ -->
<script setup>
const props = defineProps<{ count: number }>()
const localCount = ref(props.count)
localCount.value++
</script>
```

**3. Import cesty**
```typescript
// ❌ ŠPATNĚ (React)
import api from '../services/api'

// ✅ SPRÁVNĚ (Vue)
import api from '@/services/api'
```

---

## 📞 Co teď?

**1. Otevři projekt v editoru (VS Code)**
```bash
cd C:\Users\krist\Desktop\SunSkogAB\sunskog-vue3
code .
```

**2. Nainstaluj závislosti**
```bash
npm install
```

**3. Spusť dev server**
```bash
npm run dev
```

**4. Začni s převodem první komponenty**
- Otevři `EXAMPLE_TIMESHEETS.md`
- Vytvoř `TimesheetsView.vue`
- Postupuj podle příkladu

---

## 🎊 Hotovo!

Máš kompletní Vue 3 projekt připravený k použití. Teď už jen postupně převeď komponenty z Reactu.

**Hodně štěstí! 🚀**

Pokud budeš potřebovat pomoc s konkrétní komponentou, klidně se ptej!
