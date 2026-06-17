import { useState } from 'react'
import SortableTable, { type ColumnDef } from '../components/nativebi/SortableTable'
import NativeBiPageHeader from '../components/nativebi/NativeBiPageHeader'
import { NbEmptyState } from '../components/nativebi/NativeBiState'
import NativeBiMiniBarList from '../components/nativebi/NativeBiMiniBarList'
import NativeBiFilterBar from '../components/nativebi/NativeBiFilterBar'
import {
  useSalesOverview,
  useSalesCustomers,
  useSalesItems,
  useSalesSalespersons,
} from '../hooks/useNativeBiSales'
import { useBiSalesFulfillment } from '../hooks/useProcessBi'
import { useNativeBiFilters } from '../hooks/useNativeBiFilters'
import { useItemGroupOptions, useCustomerGroupOptions, useSalespersonOptions } from '../hooks/useFilterOptions'
import type {
  CustomerSales,
  ItemSales,
  SalespersonSales,
  NbPagedMeta,
  PaginationParams,
} from '../types/nativeBi'
import type { SalesFulfillment } from '../types/processBi'
import type { NativeBiFilterDefinition } from '../types/nativeBiFilters'

function defaultDates() {
  const to = new Date()
  const from = new Date()
  from.setDate(from.getDate() - 365)
  return {
    dateFrom: from.toISOString().slice(0, 10),
    dateTo: to.toISOString().slice(0, 10),
  }
}

const SALES_FILTER_DEFS: NativeBiFilterDefinition[] = [
  { key: 'dateFrom', label: 'Período', type: 'date-range', source: 'static', modules: ['sales'] },
  { key: 'year',     label: 'Año',     type: 'year',       source: 'static', modules: ['sales'] },
  { key: 'salesType', label: 'Tipo',   type: 'toggle',     source: 'static', modules: ['sales'] },
  { key: 'month',           label: 'Mes',            type: 'month',  source: 'static',   modules: ['sales'], isAdvanced: true, placeholder: 'Todos' },
  { key: 'salespersonCodes',  label: 'Vendedor',     type: 'select', source: 'endpoint', modules: ['sales'], isAdvanced: true, placeholder: 'Todos' },
  { key: 'itemGroupCodes',    label: 'Grupo artículo', type: 'select', source: 'endpoint', modules: ['sales'], isAdvanced: true, placeholder: 'Todos' },
  { key: 'customerGroupCodes', label: 'Grupo cliente', type: 'select', source: 'endpoint', modules: ['sales'], isAdvanced: true, placeholder: 'Todos' },
]

function fmtAmt(n: number) {
  return n.toLocaleString('es-CL', { maximumFractionDigits: 0 })
}

function fmtDate(iso: string | null) {
  if (!iso) return '—'
  return new Date(iso + 'T00:00:00').toLocaleDateString('es-CL', {
    day: '2-digit', month: 'short', year: 'numeric',
  })
}

function pct(part: number, total: number): number {
  if (!total) return 0
  return (part / total) * 100
}

function semaforo(value: number, total: number): { text: string; color: string } {
  const p = pct(value, total)
  if (p >= 15) return { text: 'Alto', color: '#16A34A' }
  if (p >= 5)  return { text: 'Medio', color: '#D97706' }
  return { text: 'Bajo', color: '#94A3B8' }
}

type Tab = 'resumen' | 'customers' | 'items' | 'salespersons' | 'fulfillment'

const LIMIT = 20
const EMPTY_META: NbPagedMeta = { limit: LIMIT, offset: 0, count: 0, hasMore: false }

function initPag(sortBy: string): PaginationParams {
  return { limit: LIMIT, offset: 0, sortBy, sortDir: 'desc' }
}

function TabButton({ label, active, onClick }: { id?: string; label: string; active: boolean; onClick: () => void }) {
  return (
    <button
      role="tab"
      aria-selected={active}
      onClick={onClick}
      style={{
        padding: '0 16px',
        height: 44,
        background: 'none',
        border: 'none',
        borderBottom: active ? '2px solid var(--brand-primary, #2563EB)' : '2px solid transparent',
        color: active ? 'var(--brand-primary, #2563EB)' : 'var(--c-text-muted)',
        fontWeight: active ? 600 : 500,
        fontSize: 13.5,
        cursor: 'pointer',
        fontFamily: 'inherit',
        marginBottom: -1,
        transition: 'color 150ms, border-color 150ms',
        whiteSpace: 'nowrap',
      }}
    >
      {label}
    </button>
  )
}

function StatCard({ label, value, sub, loading }: { label: string; value: React.ReactNode; sub?: string; loading?: boolean }) {
  return (
    <div className="db-stat-card">
      <span className="db-stat-label">{label}</span>
      {loading ? (
        <div className="cp-skeleton" style={{ height: 24, width: '70%', marginTop: 8 }} />
      ) : (
        <span className="db-stat-value" style={{ fontSize: 22, fontVariantNumeric: 'tabular-nums' }}>{value}</span>
      )}
      {sub && <span style={{ fontSize: 11.5, color: 'var(--c-text-faint)', marginTop: 2 }}>{sub}</span>}
    </div>
  )
}

export default function NativeBiSalesPage() {
  const { filters, setFilter, resetFilter, resetAll, hasActiveFilters } = useNativeBiFilters('sales', defaultDates())
  const [tab, setTab] = useState<Tab>('resumen')
  const [custP, setCustP] = useState<PaginationParams>(initPag('netSalesAmount'))
  const [itemP, setItemP] = useState<PaginationParams>(initPag('grossSalesAmount'))
  const [spP, setSpP]     = useState<PaginationParams>(initPag('netSalesAmount'))

  const { data: spOpts, isLoading: spOptsLoading } = useSalespersonOptions()
  const { data: igOpts, isLoading: igOptsLoading } = useItemGroupOptions()
  const { data: cgOpts, isLoading: cgOptsLoading } = useCustomerGroupOptions()

  const optionsByKey: Record<string, { value: string; label: string }[]> = {
    salespersonCodes:  spOpts ?? [],
    itemGroupCodes:    igOpts ?? [],
    customerGroupCodes: cgOpts ?? [],
  }
  const loadingKeys = new Set<string>([
    ...(spOptsLoading ? ['salespersonCodes'] : []),
    ...(igOptsLoading ? ['itemGroupCodes'] : []),
    ...(cgOptsLoading ? ['customerGroupCodes'] : []),
  ])

  const { data: overview, isLoading: loadingOv } = useSalesOverview({ dateFrom: filters.dateFrom, dateTo: filters.dateTo })
  const { data: custData, isLoading: loadingCust }     = useSalesCustomers(custP)
  const { data: itemData, isLoading: loadingItems }    = useSalesItems(itemP)
  const { data: spData, isLoading: loadingSp }         = useSalesSalespersons(spP)
  const { data: fulfillData, isLoading: loadingFulfill } = useBiSalesFulfillment(30)

  // ── Derived KPIs ─────────────────────────────────────────────────────────
  const customers   = custData?.data ?? []
  const items       = itemData?.data ?? []
  const sps         = spData?.data ?? []

  const totalCustNet = customers.reduce((s, c) => s + c.netSalesAmount, 0)
  const totalItemGross = items.reduce((s, i) => s + i.grossSalesAmount, 0)
  const totalSpNet  = sps.reduce((s, sp) => s + sp.netSalesAmount, 0)

  const topClient   = customers[0]
  const topItem     = items[0]
  const top5CustNet = customers.slice(0, 5).reduce((s, c) => s + c.netSalesAmount, 0)
  const concTop5    = overview ? pct(top5CustNet, overview.netSalesAmount) : 0
  const avgPerCust  = overview && overview.activeCustomers > 0
    ? overview.netSalesAmount / overview.activeCustomers
    : 0

  // ── Column definitions ───────────────────────────────────────────────────

  const custCols: ColumnDef<CustomerSales>[] = [
    {
      key: 'rank',
      label: '#',
      render: (_r, i) => (
        <span style={{ fontSize: 12, color: 'var(--c-text-faint)', fontWeight: 700, fontVariantNumeric: 'tabular-nums' }}>
          {(custP.offset ?? 0) + (i ?? 0) + 1}
        </span>
      ),
    },
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
      render: (r) => <span style={{ fontVariantNumeric: 'tabular-nums' }}>{fmtAmt(r.netSalesAmount)}</span>,
    },
    {
      key: 'pct',
      label: '% Part.',
      align: 'right',
      render: (r) => {
        const p = pct(r.netSalesAmount, totalCustNet)
        return (
          <span style={{ fontVariantNumeric: 'tabular-nums', color: 'var(--c-text-muted)' }}>
            {p.toFixed(1)}%
          </span>
        )
      },
    },
    {
      key: 'nivel',
      label: 'Nivel',
      render: (r) => {
        const s = semaforo(r.netSalesAmount, totalCustNet)
        return (
          <span style={{
            display: 'inline-block',
            padding: '2px 8px',
            borderRadius: 4,
            fontSize: 11.5,
            fontWeight: 600,
            color: '#fff',
            backgroundColor: s.color,
          }}>
            {s.text}
          </span>
        )
      },
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
      render: (r) => <span style={{ fontVariantNumeric: 'tabular-nums' }}>{fmtAmt(r.avgTicketAmount)}</span>,
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
      key: 'rank',
      label: '#',
      render: (_r, i) => (
        <span style={{ fontSize: 12, color: 'var(--c-text-faint)', fontWeight: 700, fontVariantNumeric: 'tabular-nums' }}>
          {(itemP.offset ?? 0) + (i ?? 0) + 1}
        </span>
      ),
    },
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
      render: (r) => <span style={{ fontVariantNumeric: 'tabular-nums' }}>{r.quantitySold.toLocaleString('es-CL')}</span>,
    },
    {
      key: 'gross',
      label: 'Ventas brutas',
      sortKey: 'grossSalesAmount',
      align: 'right',
      render: (r) => <span style={{ fontVariantNumeric: 'tabular-nums' }}>{fmtAmt(r.grossSalesAmount)}</span>,
    },
    {
      key: 'pct',
      label: '% Part.',
      align: 'right',
      render: (r) => (
        <span style={{ fontVariantNumeric: 'tabular-nums', color: 'var(--c-text-muted)' }}>
          {pct(r.grossSalesAmount, totalItemGross).toFixed(1)}%
        </span>
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
      key: 'rank',
      label: '#',
      render: (_r, i) => (
        <span style={{ fontSize: 12, color: 'var(--c-text-faint)', fontWeight: 700, fontVariantNumeric: 'tabular-nums' }}>
          {(spP.offset ?? 0) + (i ?? 0) + 1}
        </span>
      ),
    },
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
      render: (r) => <span style={{ fontVariantNumeric: 'tabular-nums' }}>{fmtAmt(r.netSalesAmount)}</span>,
    },
    {
      key: 'pct',
      label: '% Part.',
      align: 'right',
      render: (r) => (
        <span style={{ fontVariantNumeric: 'tabular-nums', color: 'var(--c-text-muted)' }}>
          {pct(r.netSalesAmount, totalSpNet).toFixed(1)}%
        </span>
      ),
    },
    {
      key: 'cust',
      label: 'Clientes',
      align: 'right',
      render: (r) => <span style={{ fontVariantNumeric: 'tabular-nums' }}>{r.activeCustomers}</span>,
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
      render: (r) => <span style={{ fontVariantNumeric: 'tabular-nums' }}>{fmtAmt(r.avgTicketAmount)}</span>,
    },
  ]

  const fulfillCols: ColumnDef<SalesFulfillment>[] = [
    {
      key: 'date',
      label: 'Fecha',
      render: (r) => fmtDate(r.periodDate),
    },
    {
      key: 'ordersCount',
      label: 'Pedidos',
      align: 'right',
      render: (r) => <span style={{ fontVariantNumeric: 'tabular-nums' }}>{r.ordersCount}</span>,
    },
    {
      key: 'ordersAmount',
      label: 'Monto pedidos',
      align: 'right',
      render: (r) => <span style={{ fontVariantNumeric: 'tabular-nums' }}>{fmtAmt(r.ordersAmount)}</span>,
    },
    {
      key: 'deliveredCount',
      label: 'Entregas',
      align: 'right',
      render: (r) => <span style={{ fontVariantNumeric: 'tabular-nums' }}>{r.deliveredCount}</span>,
    },
    {
      key: 'pendingOrders',
      label: 'Pendientes',
      align: 'right',
      render: (r) => (
        <span style={{ fontVariantNumeric: 'tabular-nums', color: r.pendingOrders > 0 ? '#D97706' : 'inherit' }}>
          {r.pendingOrders}
        </span>
      ),
    },
    {
      key: 'fillRate',
      label: 'Cumplimiento',
      align: 'right',
      render: (r) => {
        const rate = r.fillRatePct ?? 0
        const color = rate < 70 ? '#DC2626' : rate < 90 ? '#D97706' : '#16A34A'
        return (
          <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'flex-end', gap: 2 }}>
            <span style={{ fontVariantNumeric: 'tabular-nums', fontWeight: 600, color }}>
              {r.fillRatePct !== null ? `${rate.toFixed(1)}%` : '—'}
            </span>
            <div style={{ width: 60, height: 3, backgroundColor: 'var(--c-border)', borderRadius: 2 }}>
              <div style={{ width: `${Math.min(rate, 100)}%`, height: '100%', backgroundColor: color, borderRadius: 2 }} />
            </div>
          </div>
        )
      },
    },
  ]

  const tabs: { id: Tab; label: string }[] = [
    { id: 'resumen',      label: 'Resumen' },
    { id: 'customers',    label: 'Clientes' },
    { id: 'items',        label: 'Productos' },
    { id: 'salespersons', label: 'Vendedores' },
    { id: 'fulfillment',  label: 'Fulfillment' },
  ]

  return (
    <div className="cp-page">
      <NativeBiPageHeader
        title="Ventas"
        description="Análisis de ventas por rango de fechas"
      />

      <NativeBiFilterBar
        filters={filters}
        definitions={SALES_FILTER_DEFS}
        optionsByKey={optionsByKey}
        loadingKeys={loadingKeys}
        onFilterChange={setFilter}
        onFilterReset={resetFilter}
        onResetAll={resetAll}
        hasActiveFilters={hasActiveFilters}
      />

      <div className="db-card">
        {/* Tab bar */}
        <div
          className="db-card-header nb-tab-bar"
          style={{ paddingLeft: 4, paddingRight: 16, gap: 0, borderBottom: '1px solid var(--c-border)', overflowX: 'auto' }}
          role="tablist"
          aria-label="Secciones de ventas"
        >
          {tabs.map((t) => (
            <TabButton key={t.id} id={t.id} label={t.label} active={tab === t.id} onClick={() => setTab(t.id)} />
          ))}
        </div>

        {/* ── Resumen ─────────────────────────────────────────────────────── */}
        {tab === 'resumen' && (
          <div style={{ padding: '20px 20px 16px' }}>

            {/* KPI grid — 4 columns */}
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(160px, 1fr))', gap: 12, marginBottom: 24 }}>
              <StatCard label="Ventas netas"      value={overview ? fmtAmt(overview.netSalesAmount) : '—'}   loading={loadingOv} />
              <StatCard label="Ventas brutas"     value={overview ? fmtAmt(overview.grossSalesAmount) : '—'} loading={loadingOv} />
              <StatCard label="Facturas emitidas" value={overview?.invoiceCount ?? '—'}                       loading={loadingOv} />
              <StatCard label="Ticket promedio"   value={overview ? fmtAmt(overview.avgTicketAmount) : '—'}  loading={loadingOv} />
              <StatCard label="Clientes activos"  value={overview?.activeCustomers ?? '—'}                   loading={loadingOv} />
              <StatCard label="NC / Devoluciones" value={overview ? fmtAmt(overview.creditMemoAmount) : '—'} loading={loadingOv} />
              <StatCard
                label="Prom. por cliente"
                value={avgPerCust > 0 ? fmtAmt(avgPerCust) : '—'}
                loading={loadingOv}
              />
              <StatCard
                label="Conc. Top 5 clientes"
                value={overview && concTop5 > 0 ? `${concTop5.toFixed(1)}%` : '—'}
                sub={customers.length > 0 ? `Top cliente: ${topClient?.cardName ?? '—'}` : undefined}
                loading={loadingOv || loadingCust}
              />
            </div>

            {/* Rankings row */}
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 24 }}>
              {/* Top clientes */}
              <div>
                {loadingCust ? (
                  <div className="cp-skeleton" style={{ height: 140, borderRadius: 6 }} />
                ) : customers.length === 0 ? (
                  <NbEmptyState message="Sin datos de clientes aún." icon="table" />
                ) : (
                  <NativeBiMiniBarList
                    title="Top clientes — ventas netas"
                    items={customers.slice(0, 6).map((c) => ({
                      label: c.cardName,
                      sublabel: c.cardCode,
                      value: c.netSalesAmount,
                      pct: pct(c.netSalesAmount, totalCustNet),
                      color: 'var(--brand-primary, #2563EB)',
                    }))}
                    formatValue={fmtAmt}
                    maxItems={6}
                  />
                )}
              </div>

              {/* Top productos */}
              <div>
                {loadingItems ? (
                  <div className="cp-skeleton" style={{ height: 140, borderRadius: 6 }} />
                ) : items.length === 0 ? (
                  <NbEmptyState message="Sin datos de productos aún." icon="table" />
                ) : (
                  <NativeBiMiniBarList
                    title="Top productos — ventas brutas"
                    items={items.slice(0, 6).map((it) => ({
                      label: it.itemName,
                      sublabel: it.itemCode,
                      value: it.grossSalesAmount,
                      pct: pct(it.grossSalesAmount, totalItemGross),
                      color: '#7C3AED',
                    }))}
                    formatValue={fmtAmt}
                    maxItems={6}
                  />
                )}
              </div>
            </div>

            {/* Vendedores mini ranking */}
            {sps.length > 0 && (
              <div style={{ marginTop: 24 }}>
                <NativeBiMiniBarList
                  title="Vendedores — participación en ventas netas"
                  items={sps.slice(0, 5).map((sp) => ({
                    label: sp.salesPersonName,
                    sublabel: `${sp.invoiceCount} facturas · ${sp.activeCustomers} clientes`,
                    value: sp.netSalesAmount,
                    pct: pct(sp.netSalesAmount, totalSpNet),
                    color: '#0891B2',
                  }))}
                  formatValue={fmtAmt}
                  maxItems={5}
                />
              </div>
            )}

            <p style={{ fontSize: 12, color: 'var(--c-text-faint)', marginTop: 20 }}>
              Período: {filters.dateFrom} — {filters.dateTo}.
              {topItem && ` Producto líder: ${topItem.itemName}.`}
            </p>
          </div>
        )}

        {/* ── Clientes ────────────────────────────────────────────────────── */}
        {tab === 'customers' && (
          <SortableTable
            data={custData?.data ?? []}
            columns={custCols}
            meta={custData?.meta ?? EMPTY_META}
            sortBy={custP.sortBy}
            sortDir={custP.sortDir}
            onPageChange={(offset) => setCustP((p) => ({ ...p, offset }))}
            onSortChange={(sortBy, sortDir) => setCustP((p) => ({ ...p, sortBy, sortDir, offset: 0 }))}
            isLoading={loadingCust}
            rowKey={(r) => r.cardCode}
          />
        )}

        {/* ── Productos ───────────────────────────────────────────────────── */}
        {tab === 'items' && (
          <SortableTable
            data={itemData?.data ?? []}
            columns={itemCols}
            meta={itemData?.meta ?? EMPTY_META}
            sortBy={itemP.sortBy}
            sortDir={itemP.sortDir}
            onPageChange={(offset) => setItemP((p) => ({ ...p, offset }))}
            onSortChange={(sortBy, sortDir) => setItemP((p) => ({ ...p, sortBy, sortDir, offset: 0 }))}
            isLoading={loadingItems}
            rowKey={(r) => r.itemCode}
          />
        )}

        {/* ── Vendedores ──────────────────────────────────────────────────── */}
        {tab === 'salespersons' && (
          <SortableTable
            data={spData?.data ?? []}
            columns={spCols}
            meta={spData?.meta ?? EMPTY_META}
            sortBy={spP.sortBy}
            sortDir={spP.sortDir}
            onPageChange={(offset) => setSpP((p) => ({ ...p, offset }))}
            onSortChange={(sortBy, sortDir) => setSpP((p) => ({ ...p, sortBy, sortDir, offset: 0 }))}
            isLoading={loadingSp}
            rowKey={(r) => r.salesPersonCode}
          />
        )}

        {/* ── Fulfillment ─────────────────────────────────────────────────── */}
        {tab === 'fulfillment' && (
          loadingFulfill ? (
            <div style={{ padding: 24 }}>
              {Array.from({ length: 5 }).map((_, i) => (
                <div key={i} className="cp-skeleton" style={{ height: 44, marginBottom: 4 }} />
              ))}
            </div>
          ) : !fulfillData || fulfillData.length === 0 ? (
            <NbEmptyState message="Sin datos de fulfillment en el período. Disponible al registrar órdenes de venta en SAP." icon="chart" />
          ) : (
            <>
              {/* Fulfillment summary KPIs */}
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(150px, 1fr))', gap: 12, padding: '16px 20px 0' }}>
                {(() => {
                  const totalOrders    = fulfillData.reduce((s, d) => s + d.ordersCount, 0)
                  const totalDelivered = fulfillData.reduce((s, d) => s + d.deliveredCount, 0)
                  const totalPending   = fulfillData.reduce((s, d) => s + d.pendingOrders, 0)
                  const avgFill        = totalOrders > 0 ? (totalDelivered / totalOrders) * 100 : 0
                  return (
                    <>
                      <StatCard label="Pedidos (30d)"   value={totalOrders} />
                      <StatCard label="Entregados"      value={totalDelivered} />
                      <StatCard label="Pendientes"      value={totalPending} />
                      <StatCard label="Fill rate prom." value={`${avgFill.toFixed(1)}%`} />
                    </>
                  )
                })()}
              </div>
              <div style={{ height: 1, background: 'var(--c-border)', margin: '12px 0 0' }} />
              <SortableTable
                data={fulfillData}
                columns={fulfillCols}
                meta={{ limit: fulfillData.length, offset: 0, count: fulfillData.length, hasMore: false }}
                isLoading={false}
                rowKey={(r) => r.periodDate}
                onPageChange={() => {}}
                onSortChange={() => {}}
              />
            </>
          )
        )}
      </div>
    </div>
  )
}
