import React from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { getCompanyBrandingClient, updateCompanyBrandingClient } from '../../api/clientApi'
import { applyTheme } from '../../../lib/theme'
import type { BrandingConfig } from '../../../types'

export default function BrandingSettingsPage() {
  const queryClient = useQueryClient()

  const { data: branding, isLoading } = useQuery({
    queryKey: ['client', 'branding'],
    queryFn: getCompanyBrandingClient,
  })

  const [localBrand, setLocalBrand] = React.useState<Partial<BrandingConfig>>({})
  const [isDirty, setIsDirty] = React.useState(false)

  React.useEffect(() => {
    if (branding) {
      // eslint-disable-next-line react-hooks/set-state-in-effect
      setLocalBrand(branding)
    }
  }, [branding])

  const mutation = useMutation({
    mutationFn: updateCompanyBrandingClient,
    onSuccess: (data) => {
      queryClient.setQueryData(['client', 'branding'], data)
      setIsDirty(false)
      alert('Apariencia guardada exitosamente.')
    },
    onError: () => {
      alert('Error al guardar la apariencia.')
    }
  })

  const handleChange = (key: keyof BrandingConfig, value: string) => {
    const updated = { ...localBrand, [key]: value }
    setLocalBrand(updated)
    setIsDirty(true)
    // Apply live preview
    applyTheme(updated as BrandingConfig)
  }

  const handleSave = () => {
    mutation.mutate(localBrand)
  }

  const handleReset = () => {
    if (branding) {
      setLocalBrand(branding)
      applyTheme(branding)
      setIsDirty(false)
    }
  }

  if (isLoading) return <div style={{ padding: 24 }}>Cargando apariencia...</div>

  return (
    <div className="cp-report-view">
      <div className="cp-report-header" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <div>
          <h1 className="cp-report-title">Apariencia</h1>
          <p className="cp-report-desc">Personaliza los colores de tu portal BI</p>
        </div>
        <div style={{ display: 'flex', gap: '12px' }}>
          {isDirty && (
            <button className="db-btn db-btn--ghost" onClick={handleReset} disabled={mutation.isPending}>
              Descartar
            </button>
          )}
          <button className="db-btn db-btn--primary" onClick={handleSave} disabled={!isDirty || mutation.isPending}>
            {mutation.isPending ? 'Guardando...' : 'Guardar cambios'}
          </button>
        </div>
      </div>
      
      <div className="cp-report-content" style={{ padding: '24px' }}>
        <div className="db-card db-card--form" style={{ maxWidth: '600px' }}>
          <div className="db-form-grid">
            <div className="db-field" style={{ gridColumn: '1 / -1' }}>
              <label className="db-label">Nombre a mostrar</label>
              <input 
                type="text" 
                className="db-input" 
                value={localBrand.companyDisplayName || ''} 
                onChange={e => handleChange('companyDisplayName', e.target.value)} 
              />
            </div>

            <ColorPickerField 
              label="Color Primario (Botones principales)" 
              value={localBrand.primaryColor || '#2563EB'} 
              onChange={val => handleChange('primaryColor', val)} 
            />
            
            <ColorPickerField 
              label="Color Secundario" 
              value={localBrand.secondaryColor || '#64748B'} 
              onChange={val => handleChange('secondaryColor', val)} 
            />
            
            <ColorPickerField 
              label="Color de Énfasis (Accent)" 
              value={localBrand.accentColor || '#0EA5E9'} 
              onChange={val => handleChange('accentColor', val)} 
            />

            <ColorPickerField 
              label="Color de la Barra Lateral (Sidebar)" 
              value={localBrand.sidebarColor || '#0F172A'} 
              onChange={val => handleChange('sidebarColor', val)} 
            />

            <ColorPickerField 
              label="Color de Fondo" 
              value={localBrand.backgroundColor || '#F1F5F9'} 
              onChange={val => handleChange('backgroundColor', val)} 
            />
          </div>
        </div>
      </div>
    </div>
  )
}

function ColorPickerField({ label, value, onChange }: { label: string, value: string, onChange: (v: string) => void }) {
  return (
    <div className="db-field">
      <label className="db-label">{label}</label>
      <div style={{ display: 'flex', gap: '8px' }}>
        <input 
          type="color" 
          style={{ width: '40px', height: '40px', padding: 0, border: '1px solid var(--color-border)', borderRadius: '4px', cursor: 'pointer' }}
          value={value} 
          onChange={e => onChange(e.target.value)} 
        />
        <input 
          type="text" 
          className="db-input" 
          value={value} 
          onChange={e => onChange(e.target.value)} 
          pattern="^#[0-9a-fA-F]{6}$"
          style={{ flex: 1, fontFamily: 'monospace' }}
        />
      </div>
    </div>
  )
}
