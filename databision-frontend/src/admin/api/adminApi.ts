import api from '../../lib/api'
import type { Company, User, ApiResponse } from '../../types'

// ── Auth ──────────────────────────────────────────────────────────────────────

export interface LoginPayload {
  email: string
  password: string
}

export interface LoginResponse {
  accessToken: string
  expiresIn: number
  user: {
    id: number
    name: string
    email: string
    role: string
    companyId: number | null
  }
}

export async function login(payload: LoginPayload): Promise<LoginResponse> {
  const { data } = await api.post<ApiResponse<LoginResponse>>('/auth/login', payload)
  return data.data
}

export async function logout(): Promise<void> {
  await api.post('/auth/logout')
}

// ── Companies ─────────────────────────────────────────────────────────────────

export async function getCompanies(): Promise<Company[]> {
  const { data } = await api.get<ApiResponse<Company[]>>('/admin/companies')
  return data.data
}

export interface CreateCompanyPayload {
  name: string
  slug: string
  planName?: string
  userLimit?: number
}

export async function createCompany(payload: CreateCompanyPayload): Promise<Company> {
  const { data } = await api.post<ApiResponse<Company>>('/admin/companies', payload)
  return data.data
}

export interface UpdateCompanyPayload {
  name?: string
  status?: string
  planName?: string
  userLimit?: number
}

export async function updateCompany(id: number, payload: UpdateCompanyPayload): Promise<Company> {
  const { data } = await api.put<ApiResponse<Company>>(`/admin/companies/${id}`, payload)
  return data.data
}

// ── Users (per company) ───────────────────────────────────────────────────────

export async function getCompanyUsers(companyId: number): Promise<User[]> {
  const { data } = await api.get<ApiResponse<User[]>>(`/admin/companies/${companyId}/users`)
  return data.data
}

export interface CreateUserPayload {
  email: string
  firstName: string
  lastName: string
  password: string
  role?: string
}

export async function createCompanyUser(companyId: number, payload: CreateUserPayload): Promise<User> {
  const { data } = await api.post<ApiResponse<User>>(
    `/admin/companies/${companyId}/users`,
    payload
  )
  return data.data
}

export interface UpdateUserPayload {
  firstName: string
  lastName: string
  role: string
}

export async function updateCompanyUser(companyId: number, userId: number, payload: UpdateUserPayload): Promise<User> {
  const { data } = await api.put<ApiResponse<User>>(`/admin/companies/${companyId}/users/${userId}`, payload)
  return data.data
}

export async function updateCompanyUserStatus(companyId: number, userId: number, isActive: boolean): Promise<User> {
  const { data } = await api.patch<ApiResponse<User>>(`/admin/companies/${companyId}/users/${userId}/status`, { isActive })
  return data.data
}

// ── Reports (per company) ──────────────────────────────────────────────────────

export interface Module {
  id: number
  name: string
  slug: string
  icon: string
  reportCount: number
}

export interface ReportAdmin {
  id: number
  name: string
  description: string | null
  workspaceId: string
  reportId: string
  datasetId: string
  embedUrl: string
  isActive: boolean
}

export async function getCompanyModules(companyId: number): Promise<Module[]> {
  const { data } = await api.get<ApiResponse<Module[]>>(`/admin/companies/${companyId}/modules`)
  return data.data
}

export async function getCompanyReports(companyId: number, moduleId: number): Promise<ReportAdmin[]> {
  const { data } = await api.get<ApiResponse<ReportAdmin[]>>(`/admin/companies/${companyId}/modules/${moduleId}/reports`)
  return data.data
}

export interface SaveReportPayload {
  name: string
  description: string
  workspaceId: string
  reportId: string
  datasetId: string
  embedUrl: string
  isActive: boolean
}

export async function createCompanyReport(companyId: number, moduleId: number, payload: SaveReportPayload): Promise<ReportAdmin> {
  const { data } = await api.post<ApiResponse<ReportAdmin>>(`/admin/companies/${companyId}/modules/${moduleId}/reports`, payload)
  return data.data
}

export async function updateCompanyReport(companyId: number, reportId: number, payload: SaveReportPayload): Promise<ReportAdmin> {
  const { data } = await api.put<ApiResponse<ReportAdmin>>(`/admin/companies/${companyId}/reports/${reportId}`, payload)
  return data.data
}

export async function updateCompanyReportStatus(companyId: number, reportId: number, isActive: boolean): Promise<ReportAdmin> {
  const { data } = await api.patch<ApiResponse<ReportAdmin>>(`/admin/companies/${companyId}/reports/${reportId}/status`, { isActive })
  return data.data
}
