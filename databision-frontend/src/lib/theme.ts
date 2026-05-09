import type { BrandingConfig } from '../types'

export function applyTheme(branding: BrandingConfig) {
  const root = document.documentElement

  root.style.setProperty('--brand-primary', branding.primaryColor)
  root.style.setProperty('--brand-secondary', branding.secondaryColor)
  root.style.setProperty('--brand-accent', branding.accentColor)
  root.style.setProperty('--brand-sidebar', branding.sidebarColor)
  root.style.setProperty('--color-background', branding.backgroundColor)

  if (branding.faviconUrl) {
    const link = document.querySelector<HTMLLinkElement>("link[rel~='icon']")
    if (link) link.href = branding.faviconUrl
  }
}

export function resolveSlug(): string | null {
  const host = window.location.hostname

  if (host === 'localhost' || host === '127.0.0.1') {
    const params = new URLSearchParams(window.location.search)
    return params.get('tenant')
  }

  const parts = host.split('.')
  if (parts.length < 3) return null
  const sub = parts[0]
  return sub === 'admin' ? null : sub
}

export function isAdminHost(): boolean {
  const host = window.location.hostname
  if (host === 'localhost' || host === '127.0.0.1') {
    const params = new URLSearchParams(window.location.search)
    return params.get('app') === 'admin' || window.location.pathname.startsWith('/admin')
  }
  return host.startsWith('admin.')
}
