import { useEffect, useRef } from 'react'
import * as echarts from 'echarts'
import type { ChartDataPoint } from './NbBarChart'
import { CHART_COLORS } from './chartTheme'

interface NbPieChartProps {
  data: ChartDataPoint[]
  loading?: boolean
  height?: number
  donut?: boolean
  showLegend?: boolean
  valueFormatter?: (v: number) => string
  // Note: callers should memoize colors prop to avoid re-renders
  colors?: string[]
}

export function NbPieChart({
  data,
  loading = false,
  height = 260,
  donut = true,
  showLegend = true,
  valueFormatter,
  colors,
}: NbPieChartProps) {
  const ref = useRef<HTMLDivElement>(null)
  const chartRef = useRef<echarts.ECharts | null>(null)

  const hasData = data.length > 0

  // Effect 1: init once
  useEffect(() => {
    if (!ref.current) return
    chartRef.current = echarts.init(ref.current)
    const observer = new ResizeObserver(() => chartRef.current?.resize())
    observer.observe(ref.current)
    return () => {
      observer.disconnect()
      chartRef.current?.dispose()
      chartRef.current = null
    }
  }, [])

  // Effect 2: update option when data changes
  useEffect(() => {
    if (!chartRef.current) return
    if (!hasData) {
      chartRef.current.clear()
      return
    }

    // palette moved inside effect so inline array literals from callers don't cause re-renders
    const palette = colors ?? CHART_COLORS

    const total = data.reduce((sum, d) => sum + d.value, 0)

    const option: echarts.EChartsOption = {
      color: palette,
      legend: showLegend
        ? {
            top: 0,
            orient: 'horizontal',
            textStyle: { fontSize: 12, color: '#64748B', fontFamily: 'inherit' },
          }
        : undefined,
      tooltip: {
        trigger: 'item',
        backgroundColor: '#0F172A',
        borderColor: 'transparent',
        borderWidth: 0,
        textStyle: { color: '#fff', fontSize: 12, fontFamily: 'inherit' },
        formatter: (params: echarts.TooltipComponentFormatterCallbackParams) => {
          const p = Array.isArray(params) ? params[0] : params
          const val = typeof p.value === 'number' ? p.value : 0
          const pct = total > 0 ? ((val / total) * 100).toFixed(1) : '0.0'
          const formatted = valueFormatter ? valueFormatter(val) : String(val)
          return `${p.name}: ${formatted} (${pct}%)`
        },
      },
      series: [
        {
          type: 'pie',
          radius: donut ? ['40%', '70%'] : '70%',
          center: ['50%', showLegend ? '60%' : '55%'],
          data: data.map((d) => ({ name: d.name, value: d.value })),
          emphasis: {
            itemStyle: { shadowBlur: 10, shadowOffsetX: 0, shadowColor: 'rgba(0,0,0,0.2)' },
            scale: true,
          },
          label: donut
            ? { show: false }
            : {
                show: true,
                fontSize: 12,
                fontFamily: 'inherit',
                formatter: (params: echarts.DefaultLabelFormatterCallbackParams) => {
                  const val = typeof params.value === 'number' ? params.value : 0
                  const pct = total > 0 ? ((val / total) * 100).toFixed(1) : '0.0'
                  return `${pct}%`
                },
              },
        },
      ],
    }

    chartRef.current.setOption(option, { notMerge: true })
  }, [data, donut, showLegend, valueFormatter, colors, hasData])

  if (loading) {
    return (
      <div
        style={{
          height,
          background: '#F1F5F9',
          borderRadius: 8,
          animation: 'pulse 1.5s ease-in-out infinite',
        }}
      />
    )
  }

  if (!hasData) {
    return (
      <div
        style={{
          height,
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          color: '#94A3B8',
          fontSize: 13,
        }}
      >
        Sin datos disponibles
      </div>
    )
  }

  return <div ref={ref} style={{ height, width: '100%' }} />
}
