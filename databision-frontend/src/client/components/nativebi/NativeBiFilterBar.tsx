import { useState } from 'react'
import type { NativeBiFilterState, NativeBiFilterDefinition, FilterOption } from '../../types/nativeBiFilters'
import NativeBiFilterSelect from './NativeBiFilterSelect'
import NativeBiFilterChip from './NativeBiFilterChip'
import DateRangePicker from './DateRangePicker'
import { MONTH_OPTIONS, yearOptions, SALES_TYPE_OPTIONS } from '../../utils/nativeBiFilterUtils'

interface NativeBiFilterBarProps {
  filters: NativeBiFilterState
  definitions: NativeBiFilterDefinition[]
  optionsByKey?: Record<string, FilterOption[]>
  loadingKeys?: Set<string>
  onFilterChange: <K extends keyof NativeBiFilterState>(key: K, value: NativeBiFilterState[K]) => void
  onFilterReset: (key: keyof NativeBiFilterState) => void
  onResetAll?: () => void
  hasActiveFilters?: boolean
}

const CHIP_LABELS: Partial<Record<keyof NativeBiFilterState, string>> = {
  dateFrom:          'Desde',
  dateTo:            'Hasta',
  year:              'Año',
  month:             'Mes',
  salesType:         'Tipo venta',
  salespersonCodes:  'Vendedor',
  customerGroupCodes:'Grupo cliente',
  itemGroupCodes:    'Grupo artículo',
  supplierGroupCodes:'Grupo proveedor',
  warehouseCodes:    'Almacén',
  warehouseLocations:'Localidad',
  severity:          'Severidad',
  processCode:       'Proceso',
  objectCode:        'Objeto',
  schema:            'Schema',
  tableFilter:       'Tabla',
  udf1: 'UDF1', udf2: 'UDF2', udf3: 'UDF3',
  udf4: 'UDF4', udf5: 'UDF5', udf6: 'UDF6',
}

export default function NativeBiFilterBar({
  filters,
  definitions,
  optionsByKey = {},
  loadingKeys = new Set(),
  onFilterChange,
  onFilterReset,
  onResetAll,
  hasActiveFilters = false,
}: NativeBiFilterBarProps) {
  const [showAdvanced, setShowAdvanced] = useState(false)

  const visible  = definitions.filter((d) => !d.isAdvanced && d.isEnabled !== false)
  const advanced = definitions.filter((d) => d.isAdvanced  && d.isEnabled !== false)

  // Active filter chips (only from definitions visible here)
  const activeChips = definitions
    .filter((d) => d.isEnabled !== false)
    .flatMap((def) => {
      const val = filters[def.key]
      if (!val) return []
      const label = CHIP_LABELS[def.key] ?? def.label
      return [{ key: def.key, label, value: String(val) }]
    })

  // Date-from and date-to chips not linked to a definition (handled via DateRangePicker)
  const hasDateRange = filters.dateFrom || filters.dateTo

  function renderControl(def: NativeBiFilterDefinition) {
    switch (def.type) {
      case 'date-range':
        return (
          <DateRangePicker
            key={def.key}
            dateFrom={filters.dateFrom ?? ''}
            dateTo={filters.dateTo ?? ''}
            onChange={(from, to) => {
              onFilterChange('dateFrom', from || undefined)
              onFilterChange('dateTo',   to   || undefined)
            }}
          />
        )

      case 'year':
        return (
          <NativeBiFilterSelect
            key={def.key}
            label={def.label}
            value={filters.year}
            options={yearOptions()}
            onChange={(v) => onFilterChange('year', v)}
            width={100}
          />
        )

      case 'month':
        return (
          <NativeBiFilterSelect
            key={def.key}
            label={def.label}
            value={filters.month}
            options={MONTH_OPTIONS}
            onChange={(v) => onFilterChange('month', v)}
            placeholder="Todos"
            width={130}
          />
        )

      case 'toggle':
        if (def.key === 'salesType') {
          return (
            <div key={def.key} style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
              <label style={{ fontSize: 11, fontWeight: 500, color: 'var(--color-text-muted)', textTransform: 'uppercase', letterSpacing: '0.04em' }}>
                {def.label}
              </label>
              <div style={{ display: 'flex', borderRadius: 6, border: '1px solid var(--color-border)', overflow: 'hidden', height: 34 }}>
                {SALES_TYPE_OPTIONS.map((opt) => {
                  const active = (filters.salesType ?? 'gross') === opt.value
                  return (
                    <button
                      key={opt.value}
                      onClick={() => onFilterChange('salesType', opt.value as NativeBiFilterState['salesType'])}
                      style={{
                        padding: '0 12px',
                        fontSize: 13,
                        fontWeight: active ? 600 : 400,
                        background: active ? 'var(--color-primary,#2563EB)' : 'var(--color-surface,#fff)',
                        color: active ? '#fff' : 'var(--color-text-muted)',
                        border: 'none',
                        cursor: 'pointer',
                        borderRight: '1px solid var(--color-border)',
                        transition: 'background 0.15s',
                      }}
                    >
                      {opt.label}
                    </button>
                  )
                })}
              </div>
            </div>
          )
        }
        return null

      case 'select':
      case 'multi-select': {
        const opts = def.source === 'static'
          ? (def.options ?? [])
          : (optionsByKey[def.key] ?? [])
        return (
          <NativeBiFilterSelect
            key={def.key}
            label={def.label}
            value={filters[def.key] as string | undefined}
            options={opts}
            onChange={(v) => onFilterChange(def.key, v as never)}
            placeholder={def.placeholder ?? 'Todos'}
            loading={loadingKeys.has(def.key)}
            width={160}
          />
        )
      }

      default:
        return null
    }
  }

  return (
    <div
      style={{
        background: 'var(--color-surface,#fff)',
        border: '1px solid var(--color-border,#E2E8F0)',
        borderRadius: 8,
        padding: '12px 16px',
        marginBottom: 16,
      }}
    >
      {/* Primary filter row */}
      <div style={{ display: 'flex', flexWrap: 'wrap', gap: 16, alignItems: 'flex-end' }}>
        {visible.map(renderControl)}

        <div style={{ marginLeft: 'auto', display: 'flex', gap: 8, alignItems: 'flex-end' }}>
          {advanced.length > 0 && (
            <button
              className="db-btn db-btn--ghost db-btn--sm"
              onClick={() => setShowAdvanced((v) => !v)}
            >
              {showAdvanced ? 'Menos filtros ▲' : `Más filtros ▼${advanced.length > 0 ? ` (${advanced.length})` : ''}`}
            </button>
          )}
          {hasActiveFilters && onResetAll && (
            <button className="db-btn db-btn--ghost db-btn--sm" onClick={onResetAll}>
              Limpiar
            </button>
          )}
        </div>
      </div>

      {/* Advanced filters */}
      {showAdvanced && advanced.length > 0 && (
        <div
          style={{
            display: 'flex', flexWrap: 'wrap', gap: 16,
            marginTop: 12, paddingTop: 12,
            borderTop: '1px solid var(--color-border,#E2E8F0)',
          }}
        >
          {advanced.map(renderControl)}
        </div>
      )}

      {/* Active filter chips */}
      {(activeChips.length > 0 || hasDateRange) && (
        <div style={{ display: 'flex', flexWrap: 'wrap', gap: 6, marginTop: 10 }}>
          {hasDateRange && (
            <NativeBiFilterChip
              label="Período"
              value={[filters.dateFrom, filters.dateTo].filter(Boolean).join(' → ')}
              onRemove={() => { onFilterReset('dateFrom'); onFilterReset('dateTo') }}
            />
          )}
          {activeChips
            .filter((c) => c.key !== 'dateFrom' && c.key !== 'dateTo')
            .map((chip) => (
              <NativeBiFilterChip
                key={chip.key}
                label={chip.label}
                value={chip.value}
                onRemove={() => onFilterReset(chip.key)}
              />
            ))}
        </div>
      )}
    </div>
  )
}
