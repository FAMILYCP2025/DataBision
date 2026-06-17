import type { ReactNode } from 'react'
import type { NbPagedMeta } from '../../types/nativeBi'

export interface ColumnDef<T> {
  key: string
  label: string
  sortKey?: string
  align?: 'left' | 'right'
  render: (row: T, index?: number) => ReactNode
}

interface SortableTableProps<T extends object> {
  data: T[]
  columns: ColumnDef<T>[]
  meta: NbPagedMeta
  sortBy?: string
  sortDir?: 'asc' | 'desc'
  onPageChange: (offset: number) => void
  onSortChange: (sortBy: string, sortDir: 'asc' | 'desc') => void
  isLoading?: boolean
  rowKey: (row: T) => string
}

export default function SortableTable<T extends object>({
  data,
  columns,
  meta,
  sortBy,
  sortDir = 'desc',
  onPageChange,
  onSortChange,
  isLoading,
  rowKey,
}: SortableTableProps<T>) {
  function handleSort(col: ColumnDef<T>) {
    if (!col.sortKey) return
    const next: 'asc' | 'desc' =
      col.sortKey === sortBy && sortDir === 'desc' ? 'asc' : 'desc'
    onSortChange(col.sortKey, next)
  }

  return (
    <div>
      <div className="db-table-wrapper">
        <table className="db-table">
          <thead>
            <tr>
              {columns.map((col) => (
                <th
                  key={col.key}
                  style={{
                    textAlign: col.align ?? 'left',
                    cursor: col.sortKey ? 'pointer' : 'default',
                    userSelect: 'none',
                    whiteSpace: 'nowrap',
                  }}
                  onClick={() => handleSort(col)}
                >
                  {col.label}
                  {col.sortKey === sortBy && (
                    <span style={{ marginLeft: 4, opacity: 0.7 }}>
                      {sortDir === 'asc' ? '↑' : '↓'}
                    </span>
                  )}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {isLoading ? (
              Array.from({ length: 5 }).map((_, i) => (
                <tr key={i}>
                  {columns.map((col) => (
                    <td key={col.key}>
                      <div
                        className="cp-skeleton"
                        style={{ height: 14, width: col.align === 'right' ? '60%' : '75%', marginLeft: col.align === 'right' ? 'auto' : 0 }}
                      />
                    </td>
                  ))}
                </tr>
              ))
            ) : data.length === 0 ? (
              <tr>
                <td colSpan={columns.length} className="db-table-empty">
                  Sin datos disponibles
                </td>
              </tr>
            ) : (
              data.map((row, rowIdx) => (
                <tr key={rowKey(row)}>
                  {columns.map((col) => (
                    <td key={col.key} style={{ textAlign: col.align ?? 'left' }}>
                      {col.render(row, rowIdx)}
                    </td>
                  ))}
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      <div
        style={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          padding: '10px 16px',
          borderTop: '1px solid var(--c-border)',
        }}
      >
        <span style={{ fontSize: 12.5, color: 'var(--c-text-muted)' }}>
          {data.length > 0
            ? `${meta.offset + 1}–${meta.offset + data.length}`
            : '0 resultados'}
        </span>
        <div style={{ display: 'flex', gap: 8 }}>
          <button
            className="db-btn db-btn--ghost db-btn--sm"
            onClick={() => onPageChange(Math.max(0, meta.offset - meta.limit))}
            disabled={meta.offset === 0 || isLoading}
          >
            ← Anterior
          </button>
          <button
            className="db-btn db-btn--ghost db-btn--sm"
            onClick={() => onPageChange(meta.offset + meta.limit)}
            disabled={!meta.hasMore || isLoading}
          >
            Siguiente →
          </button>
        </div>
      </div>
    </div>
  )
}
