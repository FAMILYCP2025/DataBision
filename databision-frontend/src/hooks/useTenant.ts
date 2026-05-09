import { useEffect } from 'react'
import { useQuery } from '@tanstack/react-query'
import api from '../lib/api'
import { applyTheme, resolveSlug } from '../lib/theme'
import { useTenantStore } from '../stores/tenantStore'
import type { BrandingConfig, ApiResponse } from '../types'

export function useTenant() {
  const { branding, setTenant } = useTenantStore()
  const slug = resolveSlug()

  const query = useQuery({
    queryKey: ['tenant-config', slug],
    queryFn: async () => {
      const res = await api.get<ApiResponse<BrandingConfig>>('/tenant/config')
      return res.data.data
    },
    enabled: slug !== null,
    staleTime: 5 * 60 * 1000,
  })

  useEffect(() => {
    if (query.data && slug) {
      setTenant(slug, query.data)
      applyTheme(query.data)
    }
  }, [query.data, slug, setTenant])

  // Apply cached theme immediately to prevent flash
  useEffect(() => {
    if (branding) applyTheme(branding)
  }, [])

  return { branding: query.data ?? branding, slug, isLoading: query.isLoading }
}
