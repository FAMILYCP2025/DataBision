import type { ReactNode } from 'react'

interface KpiCardProps {
  label: string
  value: string | number
  subLabel?: string
  icon?: ReactNode
  loading?: boolean
}

export default function KpiCard({ label, value, subLabel, icon, loading }: KpiCardProps) {
  return (
    <div className="db-stat-card">
      {loading ? (
        <>
          <div className="cp-skeleton" style={{ height: 13, width: '55%' }} />
          <div className="cp-skeleton" style={{ height: 28, width: '75%', marginTop: 10 }} />
        </>
      ) : (
        <>
          <span className="db-stat-label" style={{ display: 'flex', alignItems: 'center', gap: 5 }}>
            {icon}
            {label}
          </span>
          <span
            className="db-stat-value"
            style={{ fontSize: 24, fontVariantNumeric: 'tabular-nums' }}
          >
            {value}
          </span>
          {subLabel && (
            <span style={{ fontSize: 11.5, color: 'var(--c-text-faint)' }}>{subLabel}</span>
          )}
        </>
      )}
    </div>
  )
}
