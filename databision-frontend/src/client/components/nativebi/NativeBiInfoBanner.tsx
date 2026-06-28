interface NativeBiInfoBannerProps {
  variant?: 'info' | 'warning' | 'error' | 'success'
  icon?: string
  title?: string
  message: string
  compact?: boolean
}

const VARIANT: Record<string, { bg: string; border: string; color: string; icon: string }> = {
  info:    { bg: '#F0F9FF', border: '#BAE6FD', color: '#0369A1', icon: 'ℹ️' },
  warning: { bg: '#FFFBEB', border: '#FDE68A', color: '#92400E', icon: '⚠️' },
  error:   { bg: '#FEF2F2', border: '#FECACA', color: '#991B1B', icon: '🔴' },
  success: { bg: '#F0FDF4', border: '#BBF7D0', color: '#166534', icon: '✅' },
}

// ── NativeBiOnboardingBanner ──────────────────────────────────────────────────

interface NativeBiOnboardingBannerProps {
  visible: boolean
}

export function NativeBiOnboardingBanner({ visible }: NativeBiOnboardingBannerProps) {
  if (!visible) return null
  return (
    <div
      style={{
        padding: '20px 24px',
        backgroundColor: 'var(--c-surface, #FFFFFF)',
        border: '1px solid var(--c-border, #E2E8F0)',
        borderRadius: 8,
        display: 'flex',
        flexDirection: 'column',
        gap: 12,
      }}
    >
      <div style={{ fontSize: 15, fontWeight: 700, color: 'var(--c-text, #0F172A)' }}>
        Bienvenido a DataBision
      </div>
      <div style={{ fontSize: 13.5, color: 'var(--c-text-muted, #64748B)', lineHeight: 1.6 }}>
        Aún no hay datos sincronizados desde SAP.
        Para ver tus indicadores, ejecuta la primera
        sincronización desde el servidor del extractor.
      </div>
      <div
        style={{
          padding: '10px 14px',
          backgroundColor: '#F8FAFC',
          border: '1px solid var(--c-border, #E2E8F0)',
          borderRadius: 6,
          fontFamily: 'monospace',
          fontSize: 13,
          color: 'var(--c-text, #0F172A)',
          overflowX: 'auto',
          whiteSpace: 'nowrap',
        }}
      >
        <code>DataBision.Extractor.exe --run-once --send</code>
      </div>
      <div>
        <a
          href="#"
          style={{
            fontSize: 13,
            color: 'var(--brand-primary, #2563EB)',
            textDecoration: 'none',
            fontWeight: 500,
          }}
        >
          Ver documentación →
        </a>
      </div>
    </div>
  )
}

// ── NativeBiInfoBanner ────────────────────────────────────────────────────────

export default function NativeBiInfoBanner({
  variant = 'info',
  icon,
  title,
  message,
  compact = false,
}: NativeBiInfoBannerProps) {
  const style = VARIANT[variant]
  return (
    <div style={{
      padding: compact ? '8px 14px' : '12px 18px',
      backgroundColor: style.bg,
      border: `1px solid ${style.border}`,
      borderRadius: 6,
      display: 'flex',
      gap: 10,
      alignItems: compact ? 'center' : 'flex-start',
    }}>
      <span style={{ fontSize: 15, lineHeight: 1.4, flexShrink: 0 }}>{icon ?? style.icon}</span>
      <div>
        {title && <div style={{ fontSize: 13, fontWeight: 600, color: style.color, marginBottom: 2 }}>{title}</div>}
        <div style={{ fontSize: compact ? 12.5 : 13, color: style.color, lineHeight: 1.5 }}>{message}</div>
      </div>
    </div>
  )
}
