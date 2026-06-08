import { useState } from 'react'
import DateRangePicker from '../components/nativebi/DateRangePicker'
import SortableTable, { type ColumnDef } from '../components/nativebi/SortableTable'
import {
  useSalesOverview,
  useSalesCustomers,
  useSalesItems,
  useSalesSalespersons,
} from '../hooks/useNativeBiSales'
import type {
  CustomerSales,
  ItemSales,
  SalespersonSales,
  NbPagedMeta,
  PaginationParams,
} from '../types/nativeBi'

function defaultDates() {
  const to = new Date()
  const from = new Date()
  from.setDate(from.getDate() - 30)
  return {
    dateFrom: from.toISOString().slice(0, 10),
    dateTo: to.toISOString().slice(0, 10),
  }
}

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

type Tab = 'customers' | 'items' | 'salespersons'

const LIMIT = 20

const EMPTY_META: NbPagedMeta = { limit: LIMIT, offset: 0, count: 0, hasMore: false }

function initPag(sortBy: string): PaginationParams {
  return { limit: LIMIT, offset: 0, sortBy, sortDir: 'desc' }
}

export default function NativeBiSalesPage() {
  const [dates, setDates] = useState(defaultDates)
  const [tab, setTab] = useState<Tab>('customers')

  const [custP, setCustP] = useState<PaginationParams>(initPag('netSalesAmount'))
  const [itemP, setItemP] = useState<PaginationParams>(initPag('grossSalesAmount'))
  const [spP, setSpP] = useState<PaginationParams>(initPag('netSalesAmount'))

  const { data: overview, isLoading: loadingOv } = useSalesOverview(dates)
  const { data: custData, isLoading: loadingCust } = useSalesCustomers(custP)
  const { data: itemData, isLoading: loadingItems } = useSalesItems(itemP)
  const { data: spData, isLoading: loadingSp } = useSalesSalespersons(spP)

  // ── Column definitions ───────────────────────────────────────────────────

  const custCols: ColumnDef<CustomerSales>[] = [
    {
      key: 'name',
      label: 'Cliente',
      render: (r) => (
        <div>
          <div style={{ fontWeight: 500 }}>{r.cardName}</div>
          <div style={{ fontSize: 11.5, color: 'var(--c-text-faint)' }}>{r.cardCode}</div>
        </div>
      ),
    },
    {
      key: 'net',
      label: 'Ventas netas',
      sortKey: 'netSalesAmount',
      align: 'right',
      render: (r) => (
        <span style={{ fontVariantNumeric: 'tabular-nums' }}>{fmtAmt(r.netSalesAmount)}</span>
      ),
    },
    {
      key: 'inv',
      label: 'Facturas',
      sortKey: 'invoiceCount',
      align: 'right',
      render: (r) => <span style={{ fontVariantNumeric: 'tabular-nums' }}>{r.invoiceCount}</span>,
    },
    {
      key: 'avg',
      label: 'Ticket prom.',
      align: 'right',
      render: (r) => (
        <span style={{ fontVariantNumeric: 'tabular-nums' }}>{fmtAmt(r.avgTicketAmount)}</span>
      ),
    },
    {
      key: 'last',
      label: 'Última factura',
      align: 'right',
      render: (r) => fmtDate(r.lastInvoiceDate),
    },
  ]

  const itemCols: ColumnDef<ItemSales>[] = [
    {
      key: 'name',
      label: 'Producto',
      render: (r) => (
        <div>
          <div style={{ fontWeight: 500 }}>{r.itemName}</div>
          <div style={{ fontSize: 11.5, color: 'var(--c-text-faint)' }}>{r.itemCode}</div>
        </div>
      ),
    },
    {
      key: 'qty',
      label: 'Cantidad',
      sortKey: 'quantitySold',
      align: 'right',
      render: (r) => (
        <span style={{ fontVariantNumeric: 'tabular-nums' }}>
          {r.quantitySold.toLocaleString('es-CL')}
        </span>
      ),
    },
    {
      key: 'gross',
      label: 'Ventas brutas',
      sortKey: 'grossSalesAmount',
      align: 'right',
      render: (r) => (
        <span style={{ fontVariantNumeric: 'tabular-nums' }}>{fmtAmt(r.grossSalesAmount)}</span>
      ),
    },
    {
      key: 'inv',
      label: 'Facturas',
      sortKey: 'invoiceCount',
      align: 'right',
      render: (r) => <span style={{ fontVariantNumeric: 'tabular-nums' }}>{r.invoiceCount}</span>,
    },
    {
      key: 'last',
      label: 'Última venta',
      align: 'right',
      render: (r) => fmtDate(r.lastSaleDate),
    },
  ]

  const spCols: ColumnDef<SalespersonSales>[] = [
    {
      key: 'name',
      label: 'Vendedor',
      render: (r) => (
        <div>
          <div style={{ fontWeight: 500 }}>{r.salesPersonName}</div>
          <div style={{ fontSize: 11.5, color: 'var(--c-text-faint)' }}>#{r.salesPersonCode}</div>
        </div>
      ),
    },
    {
      key: 'net',
      label: 'Ventas netas',
      sortKey: 'netSalesAmount',
      align: 'right',
      render: (r) => (
        <span style={{ fontVariantNumeric: 'tabular-nums' }}>{fmtAmt(r.netSalesAmount)}</span>
      ),
    },
    {
      key: 'inv',
      label: 'Facturas',
      sortKey: 'invoiceCount',
      align: 'right',
      render: (r) => <span style={{ fontVariantNumeric: 'tabular-nums' }}>{r.invoiceCount}</span>,
    },
    {
      key: 'cust',
      label: 'Clientes',
      align: 'right',
      render: (r) => <span style={{ fontVariantNumeric: 'tabular-nums' }}>{r.activeCustomers}</span>,
    },
    {
      key: 'avg',
      label: 'Ticket prom.',
      align: 'right',
      render: (r) => (
        <span style={{ fontVariantNumeric: 'tabular-nums' }}>{fmtAmt(r.avgTicketAmount)}</span>
      ),
    },
  ]

  // ── Tab labels ───────────────────────────────────────────────────────────

  const tabs: { id: Tab; label: string }[] = [
    { id: 'customers', label: 'Clientes' },
    { id: 'items', label: 'Productos' },
    { id: 'salespersons', label: 'Vendedores' },
  ]

  return (
    <div className="cp-page">
      {/* Header */}
      <div className="cp-page-header" style={{ flexWrap: 'wrap', gap: 12 }}>
        <div>
          <h1 className="cp-page-title">Ventas</h1>
          <p className="cp-page-subtitle">Análisis de ventas por rango de fechas</p>
        </div>
        <DateRangePicker
          dateFrom={dates.dateFrom}
          dateTo={dates.dateTo}
          onChange={(dateFrom, dateTo) => setDates({ dateFrom, dateTo })}
        />
      </div>

      {/* Overview cards */}
      <div className="db-stats-grid">
        {loadingOv ? (
          Array.from({ length: 4 }).map((_, i) => (
            <div key={i} className="db-stat-card">
              <div className="cp-skeleton" style={{ height: 13, width: '55%' }} />
              <div className="cp-skeleton" style={{ height: 26, width: '75%', marginTop: 10 }} />
            </div>
          ))
        ) : (
          <>
            <div className="db-stat-card">
              <span className="db-stat-label">Ventas netas</span>
              <span className="db-stat-value" style={{ fontSize: 24, fontVariantNumeric: 'tabular-nums' }}>
                {overview ? fmtAmt(overview.netSalesAmount) : '—'}
              </span>
            </div>
            <div className="db-stat-card">
              <span className="db-stat-label">Ventas brutas</span>
              <span className="db-stat-value" style={{ fontSize: 24, fontVariantNumeric: 'tabular-nums' }}>
                {overview ? fmtAmt(overview.grossSalesAmount) : '—'}
              </span>
            </div>
            <div className="db-stat-card">
              <span className="db-stat-label">Facturas</span>
              <span className="db-stat-value" style={{ fontSize: 24, fontVariantNumeric: 'tabular-nums' }}>
                {overview?.invoiceCount ?? '—'}
              </span>
            </div>
            <div className="db-stat-card">
              <span className="db-stat-label">Ticket promedio</span>
              <span className="db-stat-value" style={{ fontSize: 24, fontVariantNumeric: 'tabular-nums' }}>
                {overview ? fmtAmt(overview.avgTicketAmount) : '—'}
              </span>
            </div>
          </>
        )}
      </div>

      {/* Tabbed table */}
      <div className="db-card">
        {/* Tab bar */}
        <div
          className="db-card-header"
          style={{ paddingLeft: 4, paddingRight: 16, gap: 0, borderBottom: '1px solid var(--c-border)' }}
        >
          {tabs.map((t) => (
            <button
              key={t.id}
              onClick={() => setTab(t.id)}
              style={{
                padding: '0 16px',
                height: 44,
                background: 'none',
                border: 'none',
                borderBottom:
                  tab === t.id
                    ? '2px solid var(--brand-primary, #2563EB)'
                    : '2px solid transparent',
                color:
                  tab === t.id ? 'var(--brand-primary, #2563EB)' : 'var(--c-text-muted)',
                fontWeight: tab === t.id ? 600 : 500,
                fontSize: 13.5,
                cursor: 'pointer',
                fontFamily: 'inherit',
                marginBottom: -1,
                transition: 'color 150ms, border-color 150ms',
              }}
            >
              {t.label}
            </button>
          ))}
        </div>

        {tab === 'customers' && (
          <SortableTable
            data={custData?.data ?? []}
            columns={custCols}
            meta={custData?.meta ?? EMPTY_META}
            sortBy={custP.sortBy}
            sortDir={custP.sortDir}
            onPageChange={(offset) => setCustP((p) => ({ ...p, offset }))}
            onSortChange={(sortBy, sortDir) =>
              setCustP((p) => ({ ...p, sortBy, sortDir, offset: 0 }))
            }
            isLoading={loadingCust}
            rowKey={(r) => r.cardCode}
          />
        )}

        {tab === 'items' && (
          <SortableTable
            data={itemData?.data ?? []}
            columns={itemCols}
            meta={itemData?.meta ?? EMPTY_META}
            sortBy={itemP.sortBy}
            sortDir={itemP.sortDir}
            onPageChange={(offset) => setItemP((p) => ({ ...p, offset }))}
            onSortChange={(sortBy, sortDir) =>
              setItemP((p) => ({ ...p, sortBy, sortDir, offset: 0 }))
            }
            isLoading={loadingItems}
            rowKey={(r) => r.itemCode}
          />
        )}

        {tab === 'salespersons' && (
          <SortableTable
            data={spData?.data ?? []}
            columns={spCols}
            meta={spData?.meta ?? EMPTY_META}
            sortBy={spP.sortBy}
            sortDir={spP.sortDir}
            onPageChange={(offset) => setSpP((p) => ({ ...p, offset }))}
            onSortChange={(sortBy, sortDir) =>
              setSpP((p) => ({ ...p, sortBy, sortDir, offset: 0 }))
            }
            isLoading={loadingSp}
            rowKey={(r) => r.salesPersonCode}
          />
        )}
      </div>
    </div>
  )
}
