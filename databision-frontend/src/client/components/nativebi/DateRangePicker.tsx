interface DateRangePickerProps {
  dateFrom: string
  dateTo: string
  onChange: (dateFrom: string, dateTo: string) => void
}

export default function DateRangePicker({ dateFrom, dateTo, onChange }: DateRangePickerProps) {
  function handleFrom(e: React.ChangeEvent<HTMLInputElement>) {
    const from = e.target.value
    if (from && dateTo && from > dateTo) return
    onChange(from, dateTo)
  }

  function handleTo(e: React.ChangeEvent<HTMLInputElement>) {
    const to = e.target.value
    if (dateFrom && to && dateFrom > to) return
    onChange(dateFrom, to)
  }

  return (
    <div style={{ display: 'flex', alignItems: 'center', gap: 8, flexWrap: 'wrap' }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
        <label
          className="db-label"
          htmlFor="nb-date-from"
          style={{ fontSize: 12.5, whiteSpace: 'nowrap' }}
        >
          Desde
        </label>
        <input
          id="nb-date-from"
          type="date"
          className="db-input"
          style={{ width: 148, height: 34 }}
          value={dateFrom}
          onChange={handleFrom}
          max={dateTo || undefined}
        />
      </div>
      <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
        <label
          className="db-label"
          htmlFor="nb-date-to"
          style={{ fontSize: 12.5, whiteSpace: 'nowrap' }}
        >
          Hasta
        </label>
        <input
          id="nb-date-to"
          type="date"
          className="db-input"
          style={{ width: 148, height: 34 }}
          value={dateTo}
          onChange={handleTo}
          min={dateFrom || undefined}
        />
      </div>
    </div>
  )
}
