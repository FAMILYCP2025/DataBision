import { useEffect, useRef } from 'react'
import * as echarts from 'echarts'

export interface NbScatterPoint {
  x: number
  y: number
  name?: string
  size?: number
}

interface NbScatterChartProps {
  data: NbScatterPoint[]
  loading?: boolean
  height?: number
  xLabel?: string
  yLabel?: string
  xFormatter?: (v: number) => string
  yFormatter?: (v: number) => string
  color?: string
}

export function NbScatterChart({
  data,
  loading = false,
  height = 300,
  xLabel,
  yLabel,
  xFormatter,
  yFormatter,
  color = '#2563EB',
}: NbScatterChartProps) {
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

    const option: echarts.EChartsOption = {
      grid: {
        left: 60,
        right: 20,
        top: 12,
        bottom: 60,
      },
      tooltip: {
        trigger: 'item',
        backgroundColor: '#0F172A',
        borderColor: 'transparent',
        borderWidth: 0,
        textStyle: { color: '#fff', fontSize: 12, fontFamily: 'inherit' },
        formatter: (params: unknown) => {
          const p = params as { value: number[]; name?: string }
          const xVal = p.value[0]
          const yVal = p.value[1]
          const xFormatted = xFormatter ? xFormatter(xVal) : String(xVal)
          const yFormatted = yFormatter ? yFormatter(yVal) : String(yVal)
          const pointName = p.name ?? ''
          const namePart = pointName ? `${pointName}<br/>` : ''
          const xPart = xLabel ? `${xLabel}: ${xFormatted}` : xFormatted
          const yPart = yLabel ? `${yLabel}: ${yFormatted}` : yFormatted
          return `${namePart}${xPart}<br/>${yPart}`
        },
      },
      xAxis: {
        type: 'value',
        name: xLabel,
        nameLocation: 'middle',
        nameGap: 30,
        nameTextStyle: { fontSize: 12, color: '#64748B', fontFamily: 'inherit' },
        axisLabel: {
          fontSize: 12,
          color: '#64748B',
          fontFamily: 'inherit',
          formatter: xFormatter ?? undefined,
        },
        splitLine: { lineStyle: { color: '#E2E8F0' } },
        axisLine: { lineStyle: { color: '#E2E8F0' } },
        axisTick: { show: false },
      },
      yAxis: {
        type: 'value',
        name: yLabel,
        nameLocation: 'middle',
        nameGap: 40,
        nameTextStyle: { fontSize: 12, color: '#64748B', fontFamily: 'inherit' },
        axisLabel: {
          fontSize: 12,
          color: '#64748B',
          fontFamily: 'inherit',
          formatter: yFormatter ?? undefined,
        },
        splitLine: { lineStyle: { color: '#E2E8F0' } },
        axisLine: { show: false },
        axisTick: { show: false },
      },
      series: [
        {
          type: 'scatter',
          // Store size as third element [x, y, size] so symbolSize callback can read it
          data: data.map((p) => [p.x, p.y, p.size ?? 8]),
          itemStyle: { color, opacity: 0.8 },
          symbolSize: (val: number[]) => val[2] ?? 8,
        },
      ],
    }

    chartRef.current.setOption(option, { notMerge: true })
  }, [data, xLabel, yLabel, xFormatter, yFormatter, color])

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
