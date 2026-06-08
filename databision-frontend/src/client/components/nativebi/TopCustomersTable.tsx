import { useState } from 'react'
import { useDashboardTopCustomers } from '../../hooks/useNativeBiDashboard'

function fmtAmt(n: number) {
  return n.toLocaleString('es-CL', { maximumFractionDigits: 0 })
}

function fmtDate(iso: string | null) {
  if (!iso) return '—'
  return new Date(iso + 'T00:00:00').toLocaleDateString('es-CL', {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
  })
}

const LIMIT = 10

export default function TopCustomersTable() {
  const [offset, setOffset] = useState(0)
  const { data, isLoading, isError } = useDashboardTopCustomers({
    limit: LIMIT,
    offset,
    sortBy: 'netSalesAmount',
    sortDir: 'desc',
  })

  if (isLoading) {
    return (
      <div style={{ padding: '32px 0', display: 'flex', justifyContent: 'center' }}>
        <span className="db-spinner" />
      </div>
    )
  }

  if (isError) {
    return (
      <div className="db-alert db-alert--error" style={{ margin: 16 }}>
        Error al cargar clientes. Intenta recargar la página.
      </div>
    )
  }

  const rows = data?.data ?? []
  const meta = data?.meta

  if (!rows.length) {
    return (
      <div className="cp-empty-state" style={{ padding: 32 }}>
        <p>Sin datos de clientes disponibles</p>
      </div>
    )
  }

  return (
    <div>
      <div className="db-table-wrapper">
        <table className="db-table">
          <thead>
            <tr>
              <th>Cliente</th>
              <th style={{ textAlign: 'right' }}>Ventas netas</th>
              <th style={{ textAlign: 'right' }}>Facturas</th>
              <th style={{ textAlign: 'right' }}>Última factura</th>
            </tr>
          </thead>
          <tbody>
            {rows.map((c) => (
              <tr key={c.cardCode}>
                <td>
                  <div style={{ fontWeight: 500 }}>{c.cardName}</div>
                  <div style={{ fontSize: 11.5, color: 'var(--c-text-faint)' }}>{c.cardCode}</div>
                </td>
                <td style={{ textAlign: 'right', fontVariantNumeric: 'tabular-nums' }}>
                  {fmtAmt(c.netSalesAmount)}
                </td>
                <td style={{ textAlign: 'right', fontVariantNumeric: 'tabular-nums' }}>
                  {c.invoiceCount}
                </td>
                <td style={{ textAlign: 'right' }}>{fmtDate(c.lastInvoiceDate)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      {meta && (
        <div
          style={{
            display: 'flex',
            justifyContent: 'flex-end',
            alignItems: 'center',
            gap: 8,
            padding: '10px 16px',
            borderTop: '1px solid var(--c-border)',
          }}
        >
          <button
            className="db-btn db-btn--ghost db-btn--sm"
            onClick={() => setOffset(Math.max(0, offset - LIMIT))}
            disabled={offset === 0}
          >
            ← Anterior
          </button>
          <span style={{ fontSize: 12.5, color: 'var(--c-text-muted)' }}>
            {offset + 1}–{offset + rows.length}
          </span>
          <button
            className="db-btn db-btn--ghost db-btn--sm"
            onClick={() => setOffset(offset + LIMIT)}
            disabled={!meta.hasMore}
          >
            Siguiente →
          </button>
        </div>
      )}
    </div>
  )
}
