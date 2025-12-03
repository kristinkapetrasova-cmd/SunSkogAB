import axios, { type AxiosInstance, type AxiosRequestConfig, type AxiosResponse } from 'axios'

class ApiService {
  private api: AxiosInstance

  constructor() {
    this.api = axios.create({
      baseURL: 'https://sunskog-api-h6bxbkbfdba6bbfq.westeurope-01.azurewebsites.net',
      headers: {
        'Content-Type': 'application/json',
      },
    })

    // Request interceptor pro přidání tokenu
    this.api.interceptors.request.use(
      (config) => {
        const token = localStorage.getItem('token')
        if (token) {
          config.headers.Authorization = `Bearer ${token}`
        }
        return config
      },
      (error) => Promise.reject(error)
    )

    // Response interceptor pro error handling
    this.api.interceptors.response.use(
      (response) => response,
      (error) => {
        // NEPŘESMĚROVÁVAT při chybě na login endpointu (špatné heslo)
        const isLoginRequest = error.config?.url?.includes('/auth/login')
        
        if (error.response?.status === 401 && !isLoginRequest) {
          // Token vypršel - přesměrování na login (ale NE při samotném loginu)
          localStorage.removeItem('token')
          localStorage.removeItem('user')
          window.location.href = '/login'
        }
        return Promise.reject(error)
      }
    )
  }

  async get<T>(url: string, config?: AxiosRequestConfig): Promise<T> {
    const response: AxiosResponse<T> = await this.api.get(url, config)
    return response.data
  }

  async post<T>(url: string, data?: any, config?: AxiosRequestConfig): Promise<T> {
    const response: AxiosResponse<T> = await this.api.post(url, data, config)
    return response.data
  }

  async put<T>(url: string, data?: any, config?: AxiosRequestConfig): Promise<T> {
    const response: AxiosResponse<T> = await this.api.put(url, data, config)
    return response.data
  }

  async delete<T>(url: string, config?: AxiosRequestConfig): Promise<T> {
    const response: AxiosResponse<T> = await this.api.delete(url, config)
    return response.data
  }
}

export default new ApiService()