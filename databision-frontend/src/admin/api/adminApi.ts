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
  analyticsCompanyId?: string | null
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

// ── Native BI Advanced Config ──────────────────────────────────────────────────

export interface NativeBiFilterConfig {
  companyId: number
  filterKey: string
  label: string | null
  isEnabled: boolean
  isAdvanced: boolean
  displayOrder: number
  defaultValue: string | null
}

export interface UpsertNativeBiFilterConfig {
  label?: string | null
  isEnabled: boolean
  isAdvanced: boolean
  displayOrder: number
  defaultValue?: string | null
}

export interface NativeBiItemUdfFilterConfig {
  companyId: number
  udfFieldName: string
  label: string | null
  isEnabled: boolean
  isMultiSelect: boolean
  displayOrder: number
}

export interface UpsertNativeBiItemUdfFilterConfig {
  label?: string | null
  isEnabled: boolean
  isMultiSelect: boolean
  displayOrder: number
}

export interface NativeBiDimensionConfig {
  companyId: number
  dimensionNumber: number
  label: string | null
  isEnabled: boolean
}

export interface UpsertNativeBiDimensionConfig {
  label?: string | null
  isEnabled: boolean
}

export async function getNativeBiFilters(companyId: number): Promise<NativeBiFilterConfig[]> {
  const { data } = await api.get<ApiResponse<NativeBiFilterConfig[]>>(`/admin/companies/${companyId}/native-bi/filters`)
  return data.data
}

export async function upsertNativeBiFilter(
  companyId: number,
  filterKey: string,
  payload: UpsertNativeBiFilterConfig
): Promise<NativeBiFilterConfig> {
  const { data } = await api.put<ApiResponse<NativeBiFilterConfig>>(
    `/admin/companies/${companyId}/native-bi/filters/${encodeURIComponent(filterKey)}`,
    payload
  )
  return data.data
}

export async function getNativeBiItemUdfFilters(companyId: number): Promise<NativeBiItemUdfFilterConfig[]> {
  const { data } = await api.get<ApiResponse<NativeBiItemUdfFilterConfig[]>>(`/admin/companies/${companyId}/native-bi/item-udf-filters`)
  return data.data
}

export async function upsertNativeBiItemUdfFilter(
  companyId: number,
  udfFieldName: string,
  payload: UpsertNativeBiItemUdfFilterConfig
): Promise<NativeBiItemUdfFilterConfig> {
  const { data } = await api.put<ApiResponse<NativeBiItemUdfFilterConfig>>(
    `/admin/companies/${companyId}/native-bi/item-udf-filters/${encodeURIComponent(udfFieldName)}`,
    payload
  )
  return data.data
}

export async function getNativeBiDimensions(companyId: number): Promise<NativeBiDimensionConfig[]> {
  const { data } = await api.get<ApiResponse<NativeBiDimensionConfig[]>>(`/admin/companies/${companyId}/native-bi/dimensions`)
  return data.data
}

export async function upsertNativeBiDimension(
  companyId: number,
  dimensionNumber: number,
  payload: UpsertNativeBiDimensionConfig
): Promise<NativeBiDimensionConfig> {
  const { data } = await api.put<ApiResponse<NativeBiDimensionConfig>>(
    `/admin/companies/${companyId}/native-bi/dimensions/${dimensionNumber}`,
    payload
  )
  return data.data
}

// ── Account Classification Rules ───────────────────────────────────────────────

export const STATEMENT_LINES = [
  'revenue', 'cogs', 'opex', 'other_income', 'other_expense',
  'financial', 'tax', 'depreciation', 'amortization',
  'current_assets', 'non_current_assets',
  'current_liabilities', 'non_current_liabilities',
  'equity', 'unclassified',
] as const

export type StatementLine = typeof STATEMENT_LINES[number]

export interface AccountClassificationRule {
  id: number
  companyId: string
  accountCode: string | null
  formatCode: string | null
  statementLine: string
  createdAt: string
  updatedAt: string
}

export interface UpsertAccountClassificationRule {
  accountCode?: string | null
  formatCode?: string | null
  statementLine: string
}

export interface AccountClassificationTemplateSuggestion {
  accountCode: string | null
  formatCode: string | null
  suggestedStatementLine: string
  accountType: string
  accountName: string | null
  reason: string
}

export async function getAccountClassificationRules(companyId: number): Promise<AccountClassificationRule[]> {
  const { data } = await api.get<ApiResponse<AccountClassificationRule[]>>(
    `/admin/companies/${companyId}/native-bi/account-classification-rules`
  )
  return data.data
}

export async function createAccountClassificationRule(
  companyId: number,
  payload: UpsertAccountClassificationRule
): Promise<AccountClassificationRule> {
  const { data } = await api.post<ApiResponse<AccountClassificationRule>>(
    `/admin/companies/${companyId}/native-bi/account-classification-rules`,
    payload
  )
  return data.data
}

export async function updateAccountClassificationRule(
  companyId: number,
  ruleId: number,
  payload: UpsertAccountClassificationRule
): Promise<AccountClassificationRule> {
  const { data } = await api.put<ApiResponse<AccountClassificationRule>>(
    `/admin/companies/${companyId}/native-bi/account-classification-rules/${ruleId}`,
    payload
  )
  return data.data
}

export async function deleteAccountClassificationRule(companyId: number, ruleId: number): Promise<void> {
  await api.delete(`/admin/companies/${companyId}/native-bi/account-classification-rules/${ruleId}`)
}

export async function getAccountClassificationTemplate(companyId: number): Promise<AccountClassificationTemplateSuggestion[]> {
  const { data } = await api.post<{ data: AccountClassificationTemplateSuggestion[] }>(
    `/admin/companies/${companyId}/native-bi/account-classification-rules/import-template`
  )
  return data.data
}

// ── Native BI Connection Profiles ─────────────────────────────────────────────

export interface NativeBiConnectionProfile {
  id: number
  companyId: number
  profileName: string
  environmentName: string
  serviceLayerBaseUrl: string
  companyDb: string
  sapUserName: string
  secretRefHint: string
  isActive: boolean
  ignoreSslErrors: boolean
  timeoutSeconds: number
  fetchConcurrency: number
  createdAt: string
  updatedAt: string
}

export interface CreateNativeBiConnectionProfilePayload {
  profileName: string
  environmentName: string
  serviceLayerBaseUrl: string
  companyDb: string
  sapUserName: string
  secretRef: string
  isActive: boolean
  ignoreSslErrors: boolean
  timeoutSeconds: number
  fetchConcurrency: number
}

export interface UpdateNativeBiConnectionProfilePayload {
  profileName: string
  environmentName: string
  serviceLayerBaseUrl: string
  companyDb: string
  sapUserName: string
  secretRef?: string
  isActive: boolean
  ignoreSslErrors: boolean
  timeoutSeconds: number
  fetchConcurrency: number
}

export interface TestNativeBiConnectionResult {
  success: boolean
  latencyMs: number
  checkedAt: string
  serviceLayerBaseUrlMasked: string
  companyDb: string
  message: string
  capabilities: {
    loginOk: boolean
    chartOfAccountsOk: boolean
    journalEntriesOk: boolean
  }
}

export async function getNativeBiConnectionProfiles(companyId: number): Promise<NativeBiConnectionProfile[]> {
  const { data } = await api.get<ApiResponse<NativeBiConnectionProfile[]>>(
    `/admin/companies/${companyId}/native-bi/connection-profiles`
  )
  return data.data
}

export async function createNativeBiConnectionProfile(
  companyId: number,
  payload: CreateNativeBiConnectionProfilePayload
): Promise<NativeBiConnectionProfile> {
  const { data } = await api.post<ApiResponse<NativeBiConnectionProfile>>(
    `/admin/companies/${companyId}/native-bi/connection-profiles`,
    payload
  )
  return data.data
}

export async function updateNativeBiConnectionProfile(
  companyId: number,
  profileId: number,
  payload: UpdateNativeBiConnectionProfilePayload
): Promise<NativeBiConnectionProfile> {
  const { data } = await api.put<ApiResponse<NativeBiConnectionProfile>>(
    `/admin/companies/${companyId}/native-bi/connection-profiles/${profileId}`,
    payload
  )
  return data.data
}

export async function deleteNativeBiConnectionProfile(companyId: number, profileId: number): Promise<void> {
  await api.delete(`/admin/companies/${companyId}/native-bi/connection-profiles/${profileId}`)
}

export async function testNativeBiConnectionProfile(
  companyId: number,
  profileId: number
): Promise<TestNativeBiConnectionResult> {
  const { data } = await api.post<ApiResponse<TestNativeBiConnectionResult>>(
    `/admin/companies/${companyId}/native-bi/connection-profiles/${profileId}/test`
  )
  return data.data
}
