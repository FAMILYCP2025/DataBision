import type { CSSProperties } from 'react'
import type { DateRange } from '../../hooks/useDateRangeFilter'

interface DateRangeSelectorProps {
  range: DateRange
  onFromChange: (year: number, month: number) => void
  onToChange:   (year: number, month: number) => void
}

const MONTHS = [
  'Ene','Feb','Mar','Abr','May','Jun',
  'Jul','Ago','Sep','Oct','Nov','Dic',
]

export default function DateRangeSelector({ range, onFromChange, onToChange }: DateRangeSelectorProps) {
  const currentYear = new Date().getFullYear()
  const years = Array.from({ length: 5 }, (_, i) => currentYear - 4 + i)

  const selectStyle: CSSProperties = {
    padding: '5px 8px',
    borderRadius: 6,
    border: '1px solid var(--c-border)',
    fontSize: 13,
    fontFamily: 'inherit',
    background: '#fff',
    cursor: 'pointer',
  }

  return (
    <div style={{ display: 'flex', alignItems: 'center', gap: 8, flexWrap: 'wrap' }}>
      <span style={{ fontSize: 12.5, color: 'var(--c-text-muted)', fontWeight: 500 }}>Desde</span>
      <select
        value={range.fromMonth}
        onChange={e => onFromChange(range.fromYear, Number(e.target.value))}
        style={selectStyle}
      >
        {MONTHS.map((m, i) => <option key={i} value={i + 1}>{m}</option>)}
      </select>
      <select
        value={range.fromYear}
        onChange={e => onFromChange(Number(e.target.value), range.fromMonth)}
        style={selectStyle}
      >
        {years.map(y => <option key={y} value={y}>{y}</option>)}
      </select>

      <span style={{ fontSize: 12.5, color: 'var(--c-text-muted)', fontWeight: 500, marginLeft: 4 }}>Hasta</span>
      <select
        value={range.toMonth}
        onChange={e => onToChange(range.toYear, Number(e.target.value))}
        style={selectStyle}
      >
        {MONTHS.map((m, i) => <option key={i} value={i + 1}>{m}</option>)}
      </select>
      <select
        value={range.toYear}
        onChange={e => onToChange(Number(e.target.value), range.toMonth)}
        style={selectStyle}
      >
        {years.map(y => <option key={y} value={y}>{y}</option>)}
      </select>
    </div>
  )
}
