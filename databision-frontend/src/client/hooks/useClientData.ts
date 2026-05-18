import { useQuery } from '@tanstack/react-query'
import { getModules, getReportsByModule, getClientBranding, getReportById, getEmbedConfig } from '../api/clientApi'
import { useClientAuthStore } from '../store/useClientAuthStore'

export function useClientModules() {
  return useQuery({
    queryKey: ['client-modules'],
    queryFn: getModules,
    staleTime: 5 * 60_000,
  })
}

export function useModuleReports(moduleSlug: string | undefined) {
  return useQuery({
    queryKey: ['client-module-reports', moduleSlug],
    queryFn: () => getReportsByModule(moduleSlug!),
    enabled: !!moduleSlug,
    staleTime: 5 * 60_000,
  })
}

export function useReportById(moduleSlug: string | undefined, reportId: number | undefined) {
  return useQuery({
    queryKey: ['client-report', moduleSlug, reportId],
    queryFn: () => getReportById(moduleSlug!, reportId!),
    enabled: !!moduleSlug && !!reportId,
    staleTime: 5 * 60_000,
  })
}

export function useEmbedConfig(reportId: number | undefined) {
  return useQuery({
    queryKey: ['embed-config', reportId],
    queryFn: () => getEmbedConfig(reportId!),
    enabled: !!reportId,
    staleTime: 5 * 60_000,
    retry: false,
  })
}

export function useClientBranding() {
  const tenant = useClientAuthStore((s) => s.tenant)
  return useQuery({
    queryKey: ['client-branding', tenant],
    queryFn: () => getClientBranding(tenant),
    staleTime: 10 * 60_000,
  })
}
