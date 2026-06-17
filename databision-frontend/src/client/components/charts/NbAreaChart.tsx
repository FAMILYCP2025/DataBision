import { NbLineChart, type NbLineChartProps } from './NbLineChart'

type NbAreaChartProps = Omit<NbLineChartProps, 'showArea'>

export function NbAreaChart(props: NbAreaChartProps) {
  return <NbLineChart {...props} showArea />
}
