import api from '../../lib/api'
import type { ApiResponse, BrandingConfig, Module, Report, User } from '../../types'

// ── Auth ──────────────────────────────────────────────────────────────────────

export interface ClientLoginPayload {
  email: string
  password: string
  tenant?: string
}

export interface ClientLoginResponse {
  accessToken: string
  expiresIn: number
  user: {
    id: number
    name: string
    email: string
    role: string
    companyId: number | null
    companyName?: string
    companySlug?: string
  }
}

export async function clientLogin(payload: ClientLoginPayload): Promise<ClientLoginResponse> {
  const tenantParam = payload.tenant ? `?tenant=${payload.tenant}` : ''
  const { data } = await api.post<ApiResponse<ClientLoginResponse>>(`/auth/login${tenantParam}`, {
    email: payload.email,
    password: payload.password,
  })
  return data.data
}

export async function clientLogout(): Promise<void> {
  await api.post('/auth/logout')
}

// ── Branding ─────────────────────────────────────────────────────────────────
// Real source of truth: GET /api/tenant/config (public, resolved by subdomain
// in prod and by ?tenant= query param in local dev via TenantMiddleware).

const DEFAULT_BRANDING: BrandingConfig = {
  companyDisplayName: 'Portal BI',
  logoUrl: null,
  faviconUrl: null,
  primaryColor: '#2563EB',
  secondaryColor: '#64748B',
  accentColor: '#0EA5E9',
  backgroundColor: '#F1F5F9',
  sidebarColor: '#0F172A',
}

export async function getClientBranding(slug: string | null): Promise<BrandingConfig> {
  const tenantParam = slug ? `?tenant=${slug}` : ''
  try {
    const { data } = await api.get<ApiResponse<BrandingConfig>>(`/tenant/config${tenantParam}`)
    return { ...DEFAULT_BRANDING, ...data.data }
  } catch {
    return DEFAULT_BRANDING
  }
}

// ── Modules ───────────────────────────────────────────────────────────────────

export interface ModuleWithReports extends Module {
  reportCount: number
  description?: string
}

export async function getModules(): Promise<ModuleWithReports[]> {
  const { useClientAuthStore } = await import('../store/useClientAuthStore')
  const tenant = useClientAuthStore.getState().tenant
  const tenantParam = tenant ? `?tenant=${tenant}` : ''

  const { data } = await api.get<ApiResponse<ModuleWithReports[]>>(`/modules${tenantParam}`)
  return data.data
}

// ── Reports per module ────────────────────────────────────────────────────────

export interface ClientReport extends Report {
  moduleSlug: string
  type: 'powerbi' | 'table' | 'chart'
  lastUpdated?: string
  embedUrl?: string
}

export async function getReportsByModule(moduleSlug: string): Promise<ClientReport[]> {
  const { useClientAuthStore } = await import('../store/useClientAuthStore')
  const tenant = useClientAuthStore.getState().tenant
  const tenantParam = tenant ? `?tenant=${tenant}` : ''

  const { data } = await api.get<ApiResponse<ClientReport[]>>(`/modules/${moduleSlug}/reports${tenantParam}`)
  
  return data.data.map((r: any) => ({
    id: r.id,
    name: r.name,
    description: r.description,
    embedUrl: r.embedUrl,
    lastUpdated: r.lastUpdated,
    moduleSlug,
    type: 'powerbi', 
  }))
}

export async function getReportById(moduleSlug: string, reportId: number): Promise<ClientReport | null> {
  const reports = await getReportsByModule(moduleSlug)
  return reports.find((r) => r.id === reportId) ?? null
}

// ── Embed config ──────────────────────────────────────────────────────────────

export interface ReportEmbedConfigDto {
  id: number
  moduleId: number
  name: string
  description?: string
  embedUrl: string
  workspaceId: string
  reportId: string
  datasetId: string
  isConfigured: boolean
}

export async function getEmbedConfig(reportId: number): Promise<ReportEmbedConfigDto> {
  const { useClientAuthStore } = await import('../store/useClientAuthStore')
  const tenant = useClientAuthStore.getState().tenant
  const tenantParam = tenant ? `?tenant=${tenant}` : ''
  const { data } = await api.get<ApiResponse<ReportEmbedConfigDto>>(`/reports/${reportId}/embed-config${tenantParam}`)
  return data.data
}

// ── CompanyAdmin Management ───────────────────────────────────────────────────

export async function getCompanyUsersClient(): Promise<User[]> {
  const { useClientAuthStore } = await import('../store/useClientAuthStore')
  const tenant = useClientAuthStore.getState().tenant
  const tenantParam = tenant ? `?tenant=${tenant}` : ''
  const { data } = await api.get<ApiResponse<User[]>>(`/company/users${tenantParam}`)
  return data.data
}

export async function createCompanyUserClient(payload: { email: string; firstName: string; lastName: string; password: string; role?: string }): Promise<User> {
  const { useClientAuthStore } = await import('../store/useClientAuthStore')
  const tenant = useClientAuthStore.getState().tenant
  const tenantParam = tenant ? `?tenant=${tenant}` : ''
  const { data } = await api.post<ApiResponse<User>>(`/company/users${tenantParam}`, payload)
  return data.data
}

export async function updateCompanyUserClient(userId: number, payload: { firstName: string; lastName: string; role: string }): Promise<User> {
  const { useClientAuthStore } = await import('../store/useClientAuthStore')
  const tenant = useClientAuthStore.getState().tenant
  const tenantParam = tenant ? `?tenant=${tenant}` : ''
  const { data } = await api.put<ApiResponse<User>>(`/company/users/${userId}${tenantParam}`, payload)
  return data.data
}

export async function updateCompanyUserStatusClient(userId: number, isActive: boolean): Promise<User> {
  const { useClientAuthStore } = await import('../store/useClientAuthStore')
  const tenant = useClientAuthStore.getState().tenant
  const tenantParam = tenant ? `?tenant=${tenant}` : ''
  const { data } = await api.patch<ApiResponse<User>>(`/company/users/${userId}/status${tenantParam}`, { isActive })
  return data.data
}

export interface ClientPermissionItem {
  moduleSlug: string
  reportId: number | null
  enabled: boolean
}

export interface UserPermissionGroup {
  userId: number
  email: string
  permissions: ClientPermissionItem[]
}

export async function getCompanyPermissionsClient(): Promise<UserPermissionGroup[]> {
  const { useClientAuthStore } = await import('../store/useClientAuthStore')
  const tenant = useClientAuthStore.getState().tenant
  const tenantParam = tenant ? `?tenant=${tenant}` : ''
  const { data } = await api.get<ApiResponse<UserPermissionGroup[]>>(`/company/permissions${tenantParam}`)
  return data.data
}

export async function updateCompanyPermissionsClient(payload: { userId: number; permissions: ClientPermissionItem[] }): Promise<void> {
  const { useClientAuthStore } = await import('../store/useClientAuthStore')
  const tenant = useClientAuthStore.getState().tenant
  const tenantParam = tenant ? `?tenant=${tenant}` : ''
  await api.put(`/company/permissions${tenantParam}`, payload)
}

export async function getCompanyBrandingClient(): Promise<BrandingConfig> {
  const { useClientAuthStore } = await import('../store/useClientAuthStore')
  const tenant = useClientAuthStore.getState().tenant
  return getClientBranding(tenant)
}

export async function updateCompanyBrandingClient(payload: Partial<BrandingConfig>): Promise<BrandingConfig> {
  const { useClientAuthStore } = await import('../store/useClientAuthStore')
  const tenant = useClientAuthStore.getState().tenant
  const tenantParam = tenant ? `?tenant=${tenant}` : ''
  const { data } = await api.put<ApiResponse<BrandingConfig>>(`/company/branding${tenantParam}`, payload)
  return data.data
}

