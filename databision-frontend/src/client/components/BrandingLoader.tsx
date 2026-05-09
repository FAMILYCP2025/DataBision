import { useEffect } from 'react'
import { applyTheme } from '../../lib/theme'
import { useClientBranding } from '../hooks/useClientData'

/**
 * Silently applies tenant branding (CSS variables) once branding data loads.
 * Mount this inside the authenticated client shell.
 */
export default function BrandingLoader() {
  const { data: branding } = useClientBranding()

  useEffect(() => {
    if (branding) {
      applyTheme(branding)
    }
  }, [branding])

  return null
}
