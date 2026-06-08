import type { SalesDaily } from '../../types/nativeBi'

function fmtAmt(n: number) {
  return n.toLocaleString('es-CL', { maximumFractionDigits: 0 })
}

function fmtShortDate(iso: string) {
  return new Date(iso + 'T00:00:00').toLocaleDateString('es-CL', {
    month: 'short',
    day: 'numeric',
  })
}

interface SalesBarChartProps {
  data: SalesDaily[]
  height?: number
}

export default function SalesBarChart({ data, height = 120 }: SalesBarChartProps) {
  if (!data.length) {
    return (
      <div
        style={{
          height,
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          color: 'var(--c-text-faint)',
          fontSize: 13,
        }}
      >
        Sin datos de ventas
      </div>
    )
  }

  const sorted = [...data].sort((a, b) => a.salesDate.localeCompare(b.salesDate))
  const max = Math.max(...sorted.map((d) => d.netSalesAmount), 1)
  const PAD = 4
  const barH = height - PAD * 2
  const barW = 8
  const gap = 3
  const totalW = sorted.length * (barW + gap)

  return (
    <div style={{ width: '100%', overflowX: 'auto' }}>
      <svg
        viewBox={`0 0 ${totalW} ${height}`}
        style={{ display: 'block', width: '100%', height }}
        preserveAspectRatio="none"
        aria-label="Gráfico de ventas diarias (ventas netas)"
        role="img"
      >
        {sorted.map((d, i) => {
          const h = Math.max((d.netSalesAmount / max) * barH, 1)
          const x = i * (barW + gap)
          const y = height - h - PAD
          return (
            <rect
              key={d.salesDate}
              x={x}
              y={y}
              width={barW}
              height={h}
              rx={1}
              fill="var(--brand-primary, #2563EB)"
              opacity={0.75}
            >
              <title>
                {fmtShortDate(d.salesDate)}: {fmtAmt(d.netSalesAmount)}
              </title>
            </rect>
          )
        })}
      </svg>
      <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: 6 }}>
        <span style={{ fontSize: 11, color: 'var(--c-text-faint)' }}>
          {fmtShortDate(sorted[0].salesDate)}
        </span>
        <span style={{ fontSize: 11, color: 'var(--c-text-faint)' }}>
          {fmtShortDate(sorted[sorted.length - 1].salesDate)}
        </span>
      </div>
    </div>
  )
}
