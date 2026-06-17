interface MiniBarItem {
  label: string
  sublabel?: string
  value: number
  pct: number        // 0–100
  color?: string
  badgeText?: string
  badgeColor?: string
}

interface NativeBiMiniBarListProps {
  items: MiniBarItem[]
  formatValue?: (n: number) => string
  title?: string
  maxItems?: number
}

function defaultFmt(n: number): string {
  return n.toLocaleString('es-CL', { maximumFractionDigits: 0 })
}

export default function NativeBiMiniBarList({
  items,
  formatValue = defaultFmt,
  title,
  maxItems = 8,
}: NativeBiMiniBarListProps) {
  const visible = items.slice(0, maxItems)

  return (
    <div>
      {title && (
        <div style={{
          fontSize: 11.5,
          fontWeight: 600,
          color: 'var(--c-text-muted)',
          textTransform: 'uppercase',
          letterSpacing: '0.05em',
          marginBottom: 10,
        }}>
          {title}
        </div>
      )}
      <div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
        {visible.map((item, i) => (
          <div key={i} style={{ display: 'flex', alignItems: 'center', gap: 8, minHeight: 28 }}>
            {/* Rank */}
            <span style={{
              fontSize: 11,
              fontWeight: 700,
              color: 'var(--c-text-faint)',
              minWidth: 18,
              textAlign: 'right',
              fontVariantNumeric: 'tabular-nums',
            }}>
              {i + 1}
            </span>

            {/* Label + bar */}
            <div style={{ flex: 1, minWidth: 0 }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 3 }}>
                <div style={{ minWidth: 0 }}>
                  <span style={{
                    fontSize: 13,
                    fontWeight: 500,
                    color: 'var(--c-text)',
                    display: 'block',
                    overflow: 'hidden',
                    textOverflow: 'ellipsis',
                    whiteSpace: 'nowrap',
                  }}>
                    {item.label}
                  </span>
                  {item.sublabel && (
                    <span style={{ fontSize: 11, color: 'var(--c-text-faint)' }}>{item.sublabel}</span>
                  )}
                </div>
                <div style={{ display: 'flex', alignItems: 'center', gap: 6, flexShrink: 0 }}>
                  {item.badgeText && (
                    <span style={{
                      fontSize: 11,
                      fontWeight: 600,
                      padding: '1px 6px',
                      borderRadius: 3,
                      backgroundColor: item.badgeColor ?? '#E2E8F0',
                      color: item.badgeColor ? '#fff' : 'var(--c-text-muted)',
                    }}>
                      {item.badgeText}
                    </span>
                  )}
                  <span style={{
                    fontSize: 12.5,
                    fontWeight: 600,
                    fontVariantNumeric: 'tabular-nums',
                    color: 'var(--c-text)',
                  }}>
                    {formatValue(item.value)}
                  </span>
                  <span style={{
                    fontSize: 11,
                    color: 'var(--c-text-faint)',
                    minWidth: 34,
                    textAlign: 'right',
                    fontVariantNumeric: 'tabular-nums',
                  }}>
                    {item.pct.toFixed(1)}%
                  </span>
                </div>
              </div>
              {/* Bar */}
              <div style={{
                height: 4,
                backgroundColor: 'var(--c-border)',
                borderRadius: 2,
                overflow: 'hidden',
              }}>
                <div style={{
                  width: `${Math.min(item.pct, 100)}%`,
                  height: '100%',
                  backgroundColor: item.color ?? 'var(--brand-primary, #2563EB)',
                  borderRadius: 2,
                  transition: 'width 400ms ease',
                }} />
              </div>
            </div>
          </div>
        ))}
      </div>
    </div>
  )
}
