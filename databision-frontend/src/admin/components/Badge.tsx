import React from 'react'

type BadgeVariant = 'success' | 'warning' | 'danger' | 'neutral' | 'info'

interface BadgeProps {
  label: string
  variant?: BadgeVariant
}

const variantMap: Record<string, BadgeVariant> = {
  Active: 'success',
  Suspended: 'warning',
  Inactive: 'neutral',
  SuperAdmin: 'info',
  CompanyAdmin: 'neutral',
  Viewer: 'neutral',
  true: 'success',
  false: 'danger',
}

export default function Badge({ label, variant = 'neutral' }: BadgeProps) {
  return <span className={`db-badge db-badge--${variant}`}>{label}</span>
}

// eslint-disable-next-line react-refresh/only-export-components
export function statusBadge(status: string): React.ReactElement {
  return <Badge label={status} variant={variantMap[status] ?? 'neutral'} />
}
