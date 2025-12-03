# Příklad převodu: TimesheetsView

Tento soubor ukazuje, jak převést React komponentu TimesheetsView na Vue 3.

## React verze (původní)

```tsx
// src/pages/TimesheetsPage.tsx
import React, { useState, useEffect } from 'react'
import { useAuth } from '../contexts/AuthContext'
import api from '../services/api'

interface Timesheet {
  id: number
  date: string
  hours: number
  description: string
  status: 'Draft' | 'Pending' | 'Approved' | 'Rejected'
}

export function TimesheetsPage() {
  const { user } = useAuth()
  const [timesheets, setTimesheets] = useState<Timesheet[]>([])
  const [loading, setLoading] = useState(true)
  const [showModal, setShowModal] = useState(false)
  const [editingTimesheet, setEditingTimesheet] = useState<Timesheet | null>(null)

  useEffect(() => {
    fetchTimesheets()
  }, [])

  const fetchTimesheets = async () => {
    try {
      setLoading(true)
      const response = await api.get('/timesheets')
      setTimesheets(response.data)
    } catch (error) {
      console.error('Error fetching timesheets:', error)
    } finally {
      setLoading(false)
    }
  }

  const handleEdit = (timesheet: Timesheet) => {
    setEditingTimesheet(timesheet)
    setShowModal(true)
  }

  const handleSubmit = async (timesheet: Timesheet) => {
    try {
      if (editingTimesheet) {
        await api.put(`/timesheets/${timesheet.id}`, timesheet)
      } else {
        await api.post('/timesheets', timesheet)
      }
      await fetchTimesheets()
      setShowModal(false)
      setEditingTimesheet(null)
    } catch (error) {
      console.error('Error saving timesheet:', error)
    }
  }

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'Approved': return 'bg-green-100 text-green-800'
      case 'Pending': return 'bg-yellow-100 text-yellow-800'
      case 'Rejected': return 'bg-red-100 text-red-800'
      default: return 'bg-gray-100 text-gray-800'
    }
  }

  if (loading) {
    return <div className="p-6">Načítání...</div>
  }

  return (
    <div className="p-6">
      <div className="flex justify-between items-center mb-6">
        <h1 className="text-2xl font-bold">Moje výkazy</h1>
        <button
          onClick={() => setShowModal(true)}
          className="bg-green-600 text-white px-4 py-2 rounded"
        >
          Přidat výkaz
        </button>
      </div>

      <div className="bg-white rounded-lg shadow">
        <table className="w-full">
          <thead>
            <tr className="border-b">
              <th className="p-4 text-left">Datum</th>
              <th className="p-4 text-left">Hodiny</th>
              <th className="p-4 text-left">Popis</th>
              <th className="p-4 text-left">Status</th>
              <th className="p-4 text-left">Akce</th>
            </tr>
          </thead>
          <tbody>
            {timesheets.map(timesheet => (
              <tr key={timesheet.id} className="border-b hover:bg-gray-50">
                <td className="p-4">{timesheet.date}</td>
                <td className="p-4">{timesheet.hours}h</td>
                <td className="p-4">{timesheet.description}</td>
                <td className="p-4">
                  <span className={`px-3 py-1 rounded-full text-sm ${getStatusColor(timesheet.status)}`}>
                    {timesheet.status}
                  </span>
                </td>
                <td className="p-4">
                  <button
                    onClick={() => handleEdit(timesheet)}
                    className="text-blue-600 hover:underline"
                  >
                    Upravit
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {showModal && (
        <TimesheetModal
          timesheet={editingTimesheet}
          onSubmit={handleSubmit}
          onClose={() => {
            setShowModal(false)
            setEditingTimesheet(null)
          }}
        />
      )}
    </div>
  )
}
```

## Vue 3 verze (cíl)

```vue
<!-- src/views/TimesheetsView.vue -->
<template>
  <div class="p-6">
    <div v-if="loading" class="text-center">
      Načítání...
    </div>

    <div v-else>
      <!-- Header -->
      <div class="flex justify-between items-center mb-6">
        <h1 class="text-2xl font-bold text-sunskog-dark">
          {{ t('timesheets.myTimesheets') }}
        </h1>
        <button
          @click="showModal = true"
          class="bg-sunskog-green hover:bg-green-600 text-white px-4 py-2 rounded transition"
        >
          {{ t('timesheets.addTimesheet') }}
        </button>
      </div>

      <!-- Table -->
      <div class="bg-white rounded-lg shadow overflow-hidden">
        <table class="w-full">
          <thead>
            <tr class="bg-gray-50 border-b">
              <th class="p-4 text-left">{{ t('timesheets.date') }}</th>
              <th class="p-4 text-left">{{ t('timesheets.hours') }}</th>
              <th class="p-4 text-left">{{ t('timesheets.description') }}</th>
              <th class="p-4 text-left">{{ t('timesheets.status') }}</th>
              <th class="p-4 text-left">{{ t('timesheets.actions') }}</th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="timesheet in timesheets"
              :key="timesheet.id"
              class="border-b hover:bg-gray-50 transition"
            >
              <td class="p-4">{{ formatDate(timesheet.date) }}</td>
              <td class="p-4">{{ timesheet.hours }}h</td>
              <td class="p-4">{{ timesheet.description }}</td>
              <td class="p-4">
                <span
                  :class="[
                    'px-3 py-1 rounded-full text-sm font-medium',
                    getStatusColor(timesheet.status)
                  ]"
                >
                  {{ t(`timesheets.status${timesheet.status}`) }}
                </span>
              </td>
              <td class="p-4">
                <button
                  @click="handleEdit(timesheet)"
                  class="text-sunskog-blue hover:underline"
                >
                  {{ t('common.edit') }}
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- Modal -->
    <TimesheetModal
      v-if="showModal"
      :timesheet="editingTimesheet"
      @submit="handleSubmit"
      @close="handleCloseModal"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth'
import api from '@/services/api'
import TimesheetModal from '@/components/TimesheetModal.vue'
import type { Timesheet } from '@/types'

const { t } = useI18n()
const authStore = useAuthStore()

// State
const timesheets = ref<Timesheet[]>([])
const loading = ref(true)
const showModal = ref(false)
const editingTimesheet = ref<Timesheet | null>(null)

// Methods
const fetchTimesheets = async () => {
  try {
    loading.value = true
    const response = await api.get<Timesheet[]>('/timesheets')
    timesheets.value = response
  } catch (error) {
    console.error('Error fetching timesheets:', error)
  } finally {
    loading.value = false
  }
}

const handleEdit = (timesheet: Timesheet) => {
  editingTimesheet.value = timesheet
  showModal.value = true
}

const handleSubmit = async (timesheet: Timesheet) => {
  try {
    if (editingTimesheet.value) {
      await api.put(`/timesheets/${timesheet.id}`, timesheet)
    } else {
      await api.post('/timesheets', timesheet)
    }
    await fetchTimesheets()
    handleCloseModal()
  } catch (error) {
    console.error('Error saving timesheet:', error)
  }
}

const handleCloseModal = () => {
  showModal.value = false
  editingTimesheet.value = null
}

const getStatusColor = (status: string) => {
  const colors: Record<string, string> = {
    Approved: 'bg-green-100 text-green-800',
    Pending: 'bg-yellow-100 text-yellow-800',
    Rejected: 'bg-red-100 text-red-800',
    Draft: 'bg-gray-100 text-gray-800'
  }
  return colors[status] || colors.Draft
}

const formatDate = (date: string) => {
  return new Date(date).toLocaleDateString('cs-CZ')
}

// Lifecycle
onMounted(() => {
  fetchTimesheets()
})
</script>
```

## Klíčové změny

### 1. Import statements
```typescript
// React
import React, { useState, useEffect } from 'react'
import { useAuth } from '../contexts/AuthContext'

// Vue
import { ref, onMounted } from 'vue'
import { useAuthStore } from '@/stores/auth'
```

### 2. State management
```typescript
// React
const [timesheets, setTimesheets] = useState<Timesheet[]>([])

// Vue
const timesheets = ref<Timesheet[]>([])
timesheets.value = [...] // přístup přes .value
```

### 3. Lifecycle
```typescript
// React
useEffect(() => {
  fetchTimesheets()
}, [])

// Vue
onMounted(() => {
  fetchTimesheets()
})
```

### 4. Template syntaxe
```jsx
// React JSX
<button onClick={() => setShowModal(true)}>

// Vue template
<button @click="showModal = true">
```

### 5. Conditional rendering
```jsx
// React
{loading ? <div>Loading...</div> : <div>Content</div>}

// Vue
<div v-if="loading">Loading...</div>
<div v-else>Content</div>
```

### 6. List rendering
```jsx
// React
{timesheets.map(timesheet => (
  <tr key={timesheet.id}>...</tr>
))}

// Vue
<tr v-for="timesheet in timesheets" :key="timesheet.id">
  ...
</tr>
```

## Co dělat dál?

1. Začni s `TimesheetsView.vue`
2. Vytvoř `TimesheetModal.vue` komponentu
3. Připoj API endpointy
4. Otestuj funkcionalitu
5. Pokračuj na další stránku
