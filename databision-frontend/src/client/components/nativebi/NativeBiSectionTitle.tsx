import type { ReactNode } from 'react'

interface NativeBiSectionTitleProps {
  title: string
  action?: ReactNode
  style?: React.CSSProperties
}

export default function NativeBiSectionTitle({ title, action, style }: NativeBiSectionTitleProps) {
  return (
    <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 14, ...style }}>
      <div style={{
        fontSize: 12, fontWeight: 600,
        color: 'var(--c-text-muted)',
        textTransform: 'uppercase',
        letterSpacing: '0.04em',
      }}>
        {title}
      </div>
      {action && <div>{action}</div>}
    </div>
  )
}
