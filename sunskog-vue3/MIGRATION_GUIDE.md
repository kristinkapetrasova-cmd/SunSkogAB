# 🔄 Průvodce migrací React → Vue 3

Tento dokument poskytuje podrobný návod pro převod React komponent na Vue 3.

## 📋 Obsah

1. [Základní syntaxe](#základní-syntaxe)
2. [Komponenty](#komponenty)
3. [State Management](#state-management)
4. [Lifecycle hooks](#lifecycle-hooks)
5. [API volání](#api-volání)
6. [Formuláře](#formuláře)
7. [Časté chyby](#časté-chyby)

---

## Základní syntaxe

### JSX vs Template

**React (JSX):**
```jsx
function MyComponent() {
  const name = "Jan"
  return (
    <div className="container">
      <h1>Ahoj {name}</h1>
      {isVisible && <p>Viditelný text</p>}
      <ul>
        {items.map(item => (
          <li key={item.id}>{item.name}</li>
        ))}
      </ul>
    </div>
  )
}
```

**Vue (Template):**
```vue
<template>
  <div class="container">
    <h1>Ahoj {{ name }}</h1>
    <p v-if="isVisible">Viditelný text</p>
    <ul>
      <li v-for="item in items" :key="item.id">
        {{ item.name }}
      </li>
    </ul>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'

const name = ref('Jan')
const isVisible = ref(true)
const items = ref([...])
</script>
```

### Event handling

**React:**
```jsx
<button onClick={() => handleClick()}>Klikni</button>
<input onChange={(e) => setText(e.target.value)} />
```

**Vue:**
```vue
<button @click="handleClick">Klikni</button>
<input @input="text = $event.target.value" />
<!-- nebo s v-model -->
<input v-model="text" />
```

---

## Komponenty

### Props

**React:**
```tsx
// ParentComponent.tsx
<ChildComponent title="Test" count={10} onUpdate={handleUpdate} />

// ChildComponent.tsx
interface Props {
  title: string
  count: number
  onUpdate: (value: number) => void
}

function ChildComponent({ title, count, onUpdate }: Props) {
  return (
    <div>
      <h2>{title}</h2>
      <p>Count: {count}</p>
      <button onClick={() => onUpdate(count + 1)}>+</button>
    </div>
  )
}
```

**Vue:**
```vue
<!-- ParentComponent.vue -->
<ChildComponent 
  title="Test" 
  :count="10" 
  @update="handleUpdate" 
/>

<!-- ChildComponent.vue -->
<template>
  <div>
    <h2>{{ title }}</h2>
    <p>Count: {{ count }}</p>
    <button @click="emit('update', count + 1)">+</button>
  </div>
</template>

<script setup lang="ts">
interface Props {
  title: string
  count: number
}

const props = defineProps<Props>()
const emit = defineEmits<{
  update: [value: number]
}>()
</script>
```

---

## State Management

### Lokální state

**React:**
```tsx
const [count, setCount] = useState(0)
const [user, setUser] = useState<User | null>(null)
const [items, setItems] = useState<Item[]>([])

setCount(count + 1)
setUser({ name: 'Jan', age: 30 })
setItems([...items, newItem])
```

**Vue:**
```vue
<script setup lang="ts">
import { ref } from 'vue'

const count = ref(0)
const user = ref<User | null>(null)
const items = ref<Item[]>([])

count.value++
user.value = { name: 'Jan', age: 30 }
items.value.push(newItem)
</script>
```

### Computed values

**React:**
```tsx
const doubled = useMemo(() => count * 2, [count])
const fullName = useMemo(() => 
  `${firstName} ${lastName}`, 
  [firstName, lastName]
)
```

**Vue:**
```vue
<script setup lang="ts">
import { ref, computed } from 'vue'

const count = ref(0)
const doubled = computed(() => count.value * 2)

const firstName = ref('Jan')
const lastName = ref('Novák')
const fullName = computed(() => `${firstName.value} ${lastName.value}`)
</script>
```

### Global state (Context vs Pinia)

**React (Context):**
```tsx
// AuthContext.tsx
const AuthContext = createContext<AuthContextType | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null)
  
  const login = async (credentials: LoginCredentials) => {
    const response = await api.post('/auth/login', credentials)
    setUser(response.user)
  }
  
  return (
    <AuthContext.Provider value={{ user, login }}>
      {children}
    </AuthContext.Provider>
  )
}

export const useAuth = () => {
  const context = useContext(AuthContext)
  if (!context) throw new Error('useAuth must be used within AuthProvider')
  return context
}

// V komponentě:
const { user, login } = useAuth()
```

**Vue (Pinia):**
```typescript
// stores/auth.ts
import { defineStore } from 'pinia'
import { ref } from 'vue'

export const useAuthStore = defineStore('auth', () => {
  const user = ref<User | null>(null)
  
  async function login(credentials: LoginCredentials) {
    const response = await api.post('/auth/login', credentials)
    user.value = response.user
  }
  
  return { user, login }
})

// V komponentě:
const authStore = useAuthStore()
const user = authStore.user
authStore.login(credentials)
```

---

## Lifecycle hooks

**React:**
```tsx
useEffect(() => {
  // componentDidMount
  console.log('Komponenta namontována')
  
  return () => {
    // componentWillUnmount
    console.log('Komponenta odmontována')
  }
}, [])

useEffect(() => {
  // componentDidUpdate (když se změní prop)
  console.log('Prop se změnil:', prop)
}, [prop])
```

**Vue:**
```vue
<script setup lang="ts">
import { onMounted, onUnmounted, watch } from 'vue'

onMounted(() => {
  console.log('Komponenta namontována')
})

onUnmounted(() => {
  console.log('Komponenta odmontována')
})

watch(() => props.prop, (newValue) => {
  console.log('Prop se změnil:', newValue)
})
</script>
```

---

## API volání

**React:**
```tsx
function UserList() {
  const [users, setUsers] = useState<User[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  
  useEffect(() => {
    const fetchUsers = async () => {
      try {
        setLoading(true)
        const response = await api.get('/users')
        setUsers(response.data)
      } catch (err) {
        setError('Chyba při načítání')
      } finally {
        setLoading(false)
      }
    }
    
    fetchUsers()
  }, [])
  
  if (loading) return <div>Načítání...</div>
  if (error) return <div>{error}</div>
  
  return (
    <ul>
      {users.map(user => (
        <li key={user.id}>{user.name}</li>
      ))}
    </ul>
  )
}
```

**Vue:**
```vue
<template>
  <div v-if="loading">Načítání...</div>
  <div v-else-if="error">{{ error }}</div>
  <ul v-else>
    <li v-for="user in users" :key="user.id">
      {{ user.name }}
    </li>
  </ul>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import api from '@/services/api'

const users = ref<User[]>([])
const loading = ref(true)
const error = ref<string | null>(null)

const fetchUsers = async () => {
  try {
    loading.value = true
    const response = await api.get('/users')
    users.value = response.data
  } catch (err) {
    error.value = 'Chyba při načítání'
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  fetchUsers()
})
</script>
```

---

## Formuláře

**React:**
```tsx
function LoginForm() {
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  
  const handleSubmit = (e: FormEvent) => {
    e.preventDefault()
    login({ username, password })
  }
  
  return (
    <form onSubmit={handleSubmit}>
      <input
        value={username}
        onChange={(e) => setUsername(e.target.value)}
      />
      <input
        type="password"
        value={password}
        onChange={(e) => setPassword(e.target.value)}
      />
      <button type="submit">Přihlásit</button>
    </form>
  )
}
```

**Vue:**
```vue
<template>
  <form @submit.prevent="handleSubmit">
    <input v-model="username" />
    <input v-model="password" type="password" />
    <button type="submit">Přihlásit</button>
  </form>
</template>

<script setup lang="ts">
import { ref } from 'vue'

const username = ref('')
const password = ref('')

const handleSubmit = () => {
  login({ 
    username: username.value, 
    password: password.value 
  })
}
</script>
```

---

## Časté chyby

### ❌ Zapomenutí .value

```typescript
// ŠPATNĚ
const count = ref(0)
count++        // Vue to nepozná
```

```typescript
// SPRÁVNĚ
const count = ref(0)
count.value++  // Vue to sleduje
```

### ❌ Mutace props

```vue
<!-- ŠPATNĚ -->
<script setup>
const props = defineProps<{ count: number }>()
props.count++  // CHYBA! Props jsou readonly
</script>
```

```vue
<!-- SPRÁVNĚ -->
<script setup>
const props = defineProps<{ count: number }>()
const localCount = ref(props.count)
localCount.value++  // OK
</script>
```

### ❌ Template refs

```vue
<!-- ŠPATNĚ -->
<input ref="myInput">
<!-- v setup: const myInput = ref() nefunguje správně -->
```

```vue
<!-- SPRÁVNĚ -->
<template>
  <input ref="myInput">
</template>

<script setup>
const myInput = ref<HTMLInputElement | null>(null)

onMounted(() => {
  myInput.value?.focus()
})
</script>
```

---

## 📚 Užitečné zdroje

- [Vue 3 Docs](https://vuejs.org/)
- [Composition API](https://vuejs.org/api/composition-api-setup.html)
- [Pinia](https://pinia.vuejs.org/)
- [Vue Router](https://router.vuejs.org/)

---

## ✅ Checklist pro migraci komponenty

- [ ] Převést JSX na template syntaxi
- [ ] useState → ref nebo reactive
- [ ] useEffect → onMounted, watch
- [ ] Props správně definované
- [ ] Events emitují správně
- [ ] Context API nahrazen Pinia
- [ ] API volání fungují
- [ ] Formuláře používají v-model
- [ ] Styling (className → class)
- [ ] TypeScript typy OK
