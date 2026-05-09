export interface Company {
  id: number
  name: string
  slug: string
  status: 'Active' | 'Suspended' | 'Inactive'
  planName: string
  userLimit: number
  currentUsers: number
  createdAt: string
}

export interface User {
  id: number
  email: string
  firstName: string
  lastName: string
  role: 'SuperAdmin' | 'CompanyAdmin' | 'Viewer'
  isActive: boolean
  createdAt: string
  lastLoginAt: string | null
}

export interface Module {
  id: number
  name: string
  slug: string
  icon: string | null
}

export interface Report {
  id: number
  name: string
  description: string | null
}

export interface BrandingConfig {
  companyDisplayName: string
  logoUrl: string | null
  faviconUrl: string | null
  primaryColor: string
  secondaryColor: string
  accentColor: string
  backgroundColor: string
  sidebarColor: string
}

export interface EmbedToken {
  embedUrl: string
  accessToken: string
  tokenId: string
  expiry: string
}

export interface AuthUser {
  id: number
  name: string
  email: string
  role: string
  companyId: number | null
}

export interface ApiResponse<T> {
  data: T
}

export interface ApiError {
  error: string
  message: string
}
