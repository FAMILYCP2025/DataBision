import { useNavigate } from 'react-router-dom'
import { useSyncStatus } from '../../hooks/useNativeBiSync'
import { useClientAuthStore } from '../../store/useClientAuthStore'
import type { SyncStatusLevel } from '../../types/nativeBi'

function statusColor(level: SyncStatusLevel) {
  switch (level) {
    case 'ok':      return 'var(--c-success, #16A34A)'
    case 'warning': return 'var(--c-warning, #D97706)'
    case 'error':   return 'var(--c-danger, #DC2626)'
    default:        return 'var(--c-text-faint, #94A3B8)'
  }
}

function statusLabel(level: SyncStatusLevel) {
  switch (level) {
    case 'ok':      return 'Sync OK'
    case 'warning': return 'Sync: alerta'
    case 'error':   return 'Sync: error'
    default:        return 'Sin datos sync'
  }
}

function fmtRelative(isoUtc: string | null): string {
  if (!isoUtc) return 'Nunca sincronizado'
  const mins = Math.floor((Date.now() - new Date(isoUtc).getTime()) / 60_000)
  if (mins < 1)  return 'hace menos de 1 min'
  if (mins < 60) return `hace ${mins} min`
  const hrs = Math.floor(mins / 60)
  if (hrs < 24)  return `hace ${hrs}h`
  return `hace ${Math.floor(hrs / 24)}d`
}

export default function SyncStatusWidget() {
  const navigate = useNavigate()
  const user = useClientAuthStore((s) => s.user)
  const isAdmin = user?.role === 'CompanyAdmin'
  const { data, isLoading } = useSyncStatus()

  if (isLoading) {
    return <div className="cp-skeleton" style={{ height: 28, width: 96, borderRadius: 100 }} />
  }

  const level = data?.overallStatus ?? 'unknown'
  const color = statusColor(level)
  const tooltip = `Última actualización: ${fmtRelative(data?.lastTransformAtUtc ?? null)}`

  return (
    <button
      title={tooltip}
      onClick={() => isAdmin && navigate('/client/bi/diagnostics')}
      style={{
        display: 'inline-flex',
        alignItems: 'center',
        gap: 6,
        padding: '4px 12px',
        background: 'transparent',
        border: `1px solid ${color}`,
        borderRadius: 100,
        color,
        fontSize: 12,
        fontWeight: 600,
        cursor: isAdmin ? 'pointer' : 'default',
        fontFamily: 'inherit',
        whiteSpace: 'nowrap',
        flexShrink: 0,
      }}
    >
      <span
        style={{
          width: 7,
          height: 7,
          borderRadius: '50%',
          background: color,
          flexShrink: 0,
        }}
      />
      {statusLabel(level)}
    </button>
  )
}
