interface NativeBiHealthScoreProps {
  score: number   // 0–100
  size?: 'sm' | 'md' | 'lg'
}

function scoreColor(s: number): string {
  if (s >= 80) return '#16A34A'
  if (s >= 50) return '#D97706'
  return '#DC2626'
}

function scoreLabel(s: number): string {
  if (s >= 80) return 'Saludable'
  if (s >= 50) return 'Advertencia'
  return 'Crítico'
}

export default function NativeBiHealthScore({ score, size = 'md' }: NativeBiHealthScoreProps) {
  const color  = scoreColor(score)
  const label  = scoreLabel(score)
  const radius = size === 'lg' ? 36 : size === 'md' ? 28 : 22
  const stroke = size === 'lg' ? 6 : 5
  const circumference = 2 * Math.PI * radius
  const offset = circumference * (1 - Math.min(score, 100) / 100)

  return (
    <div style={{ display: 'flex', alignItems: 'center', gap: 14 }}>
      <svg
        width={(radius + stroke) * 2}
        height={(radius + stroke) * 2}
        style={{ transform: 'rotate(-90deg)', flexShrink: 0 }}
      >
        <circle
          cx={radius + stroke}
          cy={radius + stroke}
          r={radius}
          fill="none"
          stroke="var(--c-border)"
          strokeWidth={stroke}
        />
        <circle
          cx={radius + stroke}
          cy={radius + stroke}
          r={radius}
          fill="none"
          stroke={color}
          strokeWidth={stroke}
          strokeDasharray={circumference}
          strokeDashoffset={offset}
          strokeLinecap="round"
          style={{ transition: 'stroke-dashoffset 600ms ease' }}
        />
      </svg>
      <div>
        <div style={{ fontSize: size === 'lg' ? 28 : 22, fontWeight: 700, lineHeight: 1, color }}>
          {score}
          <span style={{ fontSize: 13, fontWeight: 500, color: 'var(--c-text-muted)', marginLeft: 3 }}>/ 100</span>
        </div>
        <div style={{ fontSize: 12, fontWeight: 600, color, marginTop: 2 }}>{label}</div>
      </div>
    </div>
  )
}
