import { create } from 'zustand'
import type { BrandingConfig } from '../types'

interface TenantState {
  slug: string | null
  branding: BrandingConfig | null
  setTenant: (slug: string, branding: BrandingConfig) => void
}

const CACHE_KEY = 'databision_tenant'

export const useTenantStore = create<TenantState>((set) => {
  const cached = localStorage.getItem(CACHE_KEY)
  const initial = cached ? JSON.parse(cached) : null

  return {
    slug: initial?.slug ?? null,
    branding: initial?.branding ?? null,

    setTenant(slug, branding) {
      localStorage.setItem(CACHE_KEY, JSON.stringify({ slug, branding }))
      set({ slug, branding })
    },
  }
})
