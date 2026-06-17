import { useEffect, useRef } from 'react'
import * as echarts from 'echarts'

export interface ChartDataPoint {
  name: string
  value: number
}

interface NbBarChartProps {
  data: ChartDataPoint[]
  loading?: boolean
  height?: number
  color?: string
  valueFormatter?: (v: number) => string
  title?: string
}

export function NbBarChart({
  data,
  loading = false,
  height = 260,
  color = 'var(--brand-primary, #2563EB)',
  valueFormatter,
  title,
}: NbBarChartProps) {
  const ref = useRef<HTMLDivElement>(null)
  const chartRef = useRef<echarts.ECharts | null>(null)

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
    if (!data.length) {
      chartRef.current.clear()
      return
    }

    const rotateLabels = data.length > 6

    const option: echarts.EChartsOption = {
      title: title
        ? { text: title, textStyle: { fontSize: 13, fontWeight: 600, color: '#0F172A', fontFamily: 'inherit' }, left: 0, top: 0 }
        : undefined,
      grid: {
        left: 60,
        right: 20,
        top: title ? 36 : 12,
        bottom: 60,
      },
      tooltip: {
        trigger: 'axis',
        backgroundColor: '#0F172A',
        borderColor: 'transparent',
        borderWidth: 0,
        textStyle: { color: '#fff', fontSize: 12, fontFamily: 'inherit' },
        formatter: (params: echarts.DefaultLabelFormatterCallbackParams | echarts.DefaultLabelFormatterCallbackParams[]) => {
          const p = Array.isArray(params) ? params[0] : params
          const val = typeof p.value === 'number' ? p.value : 0
          return `${p.name}: ${valueFormatter ? valueFormatter(val) : val}`
        },
      },
      xAxis: {
        type: 'category',
        data: data.map((d) => d.name),
        axisLabel: {
          rotate: rotateLabels ? 35 : 0,
          fontSize: 12,
          color: '#64748B',
          fontFamily: 'inherit',
        },
        axisLine: { lineStyle: { color: '#E2E8F0' } },
        axisTick: { show: false },
      },
      yAxis: {
        type: 'value',
        axisLabel: {
          fontSize: 12,
          color: '#64748B',
          fontFamily: 'inherit',
          formatter: valueFormatter ?? undefined,
        },
        splitLine: { lineStyle: { color: '#E2E8F0' } },
        axisLine: { show: false },
        axisTick: { show: false },
      },
      series: [
        {
          type: 'bar',
          data: data.map((d) => d.value),
          barMaxWidth: 48,
          itemStyle: { color, borderRadius: [4, 4, 0, 0] },
        },
      ],
    }

    chartRef.current.setOption(option, { notMerge: true })
  }, [data, color, valueFormatter, title])

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

  if (!data.length) {
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
