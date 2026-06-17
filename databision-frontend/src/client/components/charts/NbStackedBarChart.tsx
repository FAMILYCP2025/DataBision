import { useEffect, useRef } from 'react'
import * as echarts from 'echarts'
import { CHART_COLORS } from './chartTheme'

interface NbStackedSeries {
  name: string
  data: number[]
  color?: string
}

interface NbStackedBarChartProps {
  categories: string[]
  series: NbStackedSeries[]
  loading?: boolean
  height?: number
  valueFormatter?: (v: number) => string
  horizontal?: boolean
}

export function NbStackedBarChart({
  categories,
  series,
  loading = false,
  height = 300,
  valueFormatter,
  horizontal = false,
}: NbStackedBarChartProps) {
  const ref = useRef<HTMLDivElement>(null)
  const chartRef = useRef<echarts.ECharts | null>(null)

  const hasData = series.length > 0 && categories.length > 0

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

    const categoryAxis = {
      type: 'category' as const,
      data: categories,
      axisLabel: { fontSize: 12, color: '#64748B', fontFamily: 'inherit' },
      axisLine: { lineStyle: { color: '#E2E8F0' } },
      axisTick: { show: false },
    }

    const valueAxis = {
      type: 'value' as const,
      axisLabel: {
        fontSize: 12,
        color: '#64748B',
        fontFamily: 'inherit',
        formatter: valueFormatter ?? undefined,
      },
      splitLine: { lineStyle: { color: '#E2E8F0' } },
      axisLine: { show: false },
      axisTick: { show: false },
    }

    const option: echarts.EChartsOption = {
      grid: {
        left: 60,
        right: 20,
        top: 40,
        bottom: 60,
      },
      legend: {
        top: 0,
        textStyle: { fontSize: 12, color: '#64748B', fontFamily: 'inherit' },
      },
      tooltip: {
        trigger: 'axis',
        axisPointer: { type: 'shadow' },
        backgroundColor: '#0F172A',
        borderColor: 'transparent',
        borderWidth: 0,
        textStyle: { color: '#fff', fontSize: 12, fontFamily: 'inherit' },
        formatter: (params: echarts.DefaultLabelFormatterCallbackParams | echarts.DefaultLabelFormatterCallbackParams[]) => {
          const arr = Array.isArray(params) ? params : [params]
          const header = arr[0]?.name ?? ''
          const lines = arr.map((p) => {
            const val = typeof p.value === 'number' ? p.value : 0
            const formatted = valueFormatter ? valueFormatter(val) : String(val)
            return `${p.seriesName ?? ''}: ${formatted}`
          })
          return [header, ...lines].join('<br/>')
        },
      },
      xAxis: horizontal ? valueAxis : categoryAxis,
      yAxis: horizontal ? categoryAxis : valueAxis,
      series: series.map((s, i) => ({
        name: s.name,
        type: 'bar' as const,
        stack: 'total',
        data: s.data,
        barMaxWidth: 56,
        itemStyle: { color: s.color ?? CHART_COLORS[i % CHART_COLORS.length] },
      })),
    }

    chartRef.current.setOption(option, { notMerge: true })
  }, [categories, series, valueFormatter, horizontal, hasData])

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
