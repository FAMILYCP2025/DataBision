import type { SyncStatusLevel } from '../../types/nativeBi'

interface Props {
  status: SyncStatusLevel
  label?: string
}

const VARIANT: Record<SyncStatusLevel, string> = {
  ok: 'db-badge--success',
  warning: 'db-badge--warning',
  error: 'db-badge--danger',
  unknown: 'db-badge--neutral',
}

const LABEL: Record<SyncStatusLevel, string> = {
  ok: 'OK',
  warning: 'Advertencia',
  error: 'Error',
  unknown: 'Desconocido',
}

export default function NativeBiStatusBadge({ status, label }: Props) {
  const variant = VARIANT[status] ?? 'db-badge--neutral'
  const text = label ?? LABEL[status] ?? status
  return (
    <span className={`db-badge ${variant}`} title={`Estado: ${text}`}>
      {text}
    </span>
  )
}
