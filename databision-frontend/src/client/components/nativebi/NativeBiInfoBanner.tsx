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
