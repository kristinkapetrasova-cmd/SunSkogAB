// Uživatel
export interface User {
  id: number
  username: string
  email: string
  role?: UserRole
  roles?: string[]  // Backend může vrátit pole rolí
  fullName: string
}

export enum UserRole {
  Admin = 'Admin',
  Manager = 'Manager',
  Worker = 'Worker'
}

// Výkaz
export interface Timesheet {
  id: number
  userId: number
  userName: string
  date: string
  hours: number
  description: string
  status: TimesheetStatus
  createdAt: string
  updatedAt: string
}

export enum TimesheetStatus {
  Draft = 'Draft',
  Pending = 'Pending',
  Approved = 'Approved',
  Rejected = 'Rejected'
}

// Inventář
export interface InventoryItem {
  id: number
  name: string
  code: string
  quantity: number
  location: string
  description?: string
  qrCode?: string
  createdAt: string
  updatedAt: string
}

// API Response
export interface ApiResponse<T> {
  data: T
  message?: string
  success: boolean
}

// Přihlašovací údaje
export interface LoginCredentials {
  email: string
  password: string
}

// Auth Response
export interface AuthResponse {
  token: string
  user: User
}
