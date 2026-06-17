import type { FilterOption } from '../../types/nativeBiFilters'

interface NativeBiFilterSelectProps {
  label: string
  value: string | undefined
  options: FilterOption[]
  onChange: (value: string | undefined) => void
  placeholder?: string
  loading?: boolean
  disabled?: boolean
  width?: number | string
}

export default function NativeBiFilterSelect({
  label,
  value,
  options,
  onChange,
  placeholder = 'Todos',
  loading = false,
  disabled = false,
  width = 160,
}: NativeBiFilterSelectProps) {
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
      <label style={{ fontSize: 11, fontWeight: 500, color: 'var(--color-text-muted)', textTransform: 'uppercase', letterSpacing: '0.04em' }}>
        {label}
      </label>
      <select
        className="db-select"
        style={{ width, height: 34, fontSize: 13 }}
        value={value ?? ''}
        onChange={(e) => onChange(e.target.value || undefined)}
        disabled={disabled || loading}
      >
        <option value="">{loading ? 'Cargando…' : placeholder}</option>
        {options.map((opt) => (
          <option key={opt.value} value={opt.value}>
            {opt.label}
          </option>
        ))}
      </select>
    </div>
  )
}
