import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/login',
      name: 'Login',
      component: () => import('@/views/LoginView.vue'),
      meta: { requiresAuth: false }
    },
    {
      path: '/app',
      name: 'App',
      component: () => import('@/views/AppLayout.vue'),
      meta: { requiresAuth: true },
      children: [
        {
          path: '',
          redirect: '/app/dashboard'
        },
        {
          path: 'dashboard',
          name: 'Dashboard',
          component: () => import('@/views/DashboardView.vue')
        },
        {
          path: 'timesheets',
          name: 'Timesheets',
          component: () => import('@/views/TimesheetsView.vue')
        },
        {
          path: 'warehouse',
          name: 'Warehouse',
          component: () => import('@/views/WarehouseView.vue'),
          meta: { requiresWarehouse: true }
        },
        // Zachovat starou cestu pro zpětnou kompatibilitu
        {
          path: 'inventory',
          redirect: '/app/warehouse'
        },
        {
          path: 'admin',
          name: 'Admin',
          children: [
            {
              path: 'users',
              name: 'AdminUsers',
              component: () => import('@/views/admin/UsersView.vue'),
              meta: { requiresAdmin: true }
            },
            {
              path: 'teams',
              name: 'AdminTeams',
              component: () => import('@/views/admin/TeamsView.vue'),
              meta: { requiresTeamManagement: true }
            },
            {
              path: 'rates',
              name: 'AdminRates',
              component: () => import('@/views/admin/RatesView.vue'),
              meta: { requiresManagement: true }
            },
            {
              path: 'reports',
              name: 'AdminReports',
              component: () => import('@/views/admin/ReportsView.vue'),
              meta: { requiresReports: true }
            }
          ]
        }
      ]
    },
    {
      path: '/',
      redirect: '/app/dashboard'
    }
  ]
})

// Navigation guard pro autentizaci a oprávnění
router.beforeEach((to, from, next) => {
  const authStore = useAuthStore()
  const requiresAuth = to.matched.some(record => record.meta.requiresAuth)
  const requiresAdmin = to.matched.some(record => record.meta.requiresAdmin)
  const requiresManagement = to.matched.some(record => record.meta.requiresManagement)
  const requiresTeamManagement = to.matched.some(record => record.meta.requiresTeamManagement)
  const requiresWarehouse = to.matched.some(record => record.meta.requiresWarehouse)
  const requiresReports = to.matched.some(record => record.meta.requiresReports)

  if (requiresAuth && !authStore.isAuthenticated) {
    next('/login')
  } else if (requiresAdmin && !authStore.isAdmin) {
    next('/app/dashboard')
  } else if (requiresTeamManagement && !(authStore.isAdmin || authStore.isManagement)) {
    next('/app/dashboard')
  } else if (requiresManagement && !(authStore.isManagement || authStore.isAdmin)) {
    next('/app/dashboard')
  } else if (requiresWarehouse && !authStore.canAccessWarehouse) {
    next('/app/dashboard')
  } else if (requiresReports && !(authStore.isAdmin || authStore.isManagement || authStore.isAccountant)) {
    next('/app/dashboard')
  } else if (to.path === '/login' && authStore.isAuthenticated) {
    next('/app/dashboard')
  } else {
    next()
  }
})

export default router