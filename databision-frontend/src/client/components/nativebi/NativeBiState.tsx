interface SkeletonProps {
  rows?: number
  height?: number
}

export function NbLoadingSkeleton({ rows = 5, height = 44 }: SkeletonProps) {
  return (
    <div style={{ padding: 24, display: 'flex', flexDirection: 'column', gap: 10 }}>
      {Array.from({ length: rows }).map((_, i) => (
        <div key={i} className="cp-skeleton" style={{ height, borderRadius: 6 }} />
      ))}
    </div>
  )
}

interface ErrorProps {
  message?: string
  onRetry?: () => void
}

export function NbErrorState({ message = 'Error al cargar los datos.', onRetry }: ErrorProps) {
  return (
    <div
      className="db-alert db-alert--error"
      style={{ margin: 16, display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 12 }}
      role="alert"
    >
      <span>{message}</span>
      {onRetry && (
        <button className="db-btn db-btn--ghost db-btn--sm" onClick={onRetry}>
          Reintentar
        </button>
      )}
    </div>
  )
}

import type { ReactElement } from 'react'

interface EmptyProps {
  message?: string
  icon?: 'chart' | 'table' | 'search'
}

const ICONS: Record<string, ReactElement> = {
  chart: (
    <svg width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
      <line x1="18" y1="20" x2="18" y2="10" /><line x1="12" y1="20" x2="12" y2="4" /><line x1="6" y1="20" x2="6" y2="14" />
    </svg>
  ),
  table: (
    <svg width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
      <rect x="3" y="3" width="18" height="18" rx="2" /><path d="M3 9h18M3 15h18M9 3v18" />
    </svg>
  ),
  search: (
    <svg width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
      <circle cx="11" cy="11" r="8" /><line x1="21" y1="21" x2="16.65" y2="16.65" />
    </svg>
  ),
}

export function NbEmptyState({ message = 'Sin datos disponibles.', icon = 'table' }: EmptyProps) {
  return (
    <div className="cp-empty-state" style={{ padding: '32px 16px' }}>
      <div className="cp-empty-icon" style={{ color: 'var(--c-text-faint)' }}>
        {ICONS[icon]}
      </div>
      <p style={{ color: 'var(--c-text-muted)', fontSize: 13.5, margin: '8px 0 0' }}>{message}</p>
    </div>
  )
}
