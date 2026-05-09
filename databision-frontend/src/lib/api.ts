import axios from 'axios'

const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL ?? '/api',
  withCredentials: true,
})

let accessToken: string | null = null

export function setAccessToken(token: string | null) {
  accessToken = token
}

export function getAccessToken() {
  return accessToken
}

api.interceptors.request.use((config) => {
  if (accessToken) {
    config.headers.Authorization = `Bearer ${accessToken}`
  }
  return config
})

// Auth endpoints that must NOT be retried on 401
const AUTH_BYPASS_URLS = ['/auth/login', '/auth/refresh', '/auth/logout']

api.interceptors.response.use(
  (res) => res,
  async (error) => {
    const original = error.config

    // Skip retry for auth endpoints — let callers handle the error directly
    const isAuthEndpoint = AUTH_BYPASS_URLS.some((u) =>
      original?.url?.includes(u)
    )

    if (
      error.response?.status === 401 &&
      !original._retry &&
      !isAuthEndpoint
    ) {
      original._retry = true
      try {
        const { data } = await api.post('/auth/refresh')
        setAccessToken(data.data.accessToken)
        original.headers.Authorization = `Bearer ${data.data.accessToken}`
        return api(original)
      } catch {
        setAccessToken(null)
        window.location.href = '/admin/login'
      }
    }

    return Promise.reject(error)
  }
)

export default api
