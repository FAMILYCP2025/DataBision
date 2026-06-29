import { useState, useMemo, type ReactNode } from 'react'
import { useSearchParams } from 'react-router-dom'
import { Download } from 'lucide-react'
import { exportXlsx } from '../utils/exportXlsx'
import { useDateRangeFilter, filterByRange } from '../hooks/useDateRangeFilter'
import DateRangeSelector from '../components/nativebi/DateRangeSelector'
import SortableTable, { type ColumnDef } from '../components/nativebi/SortableTable'
import NativeBiPageHeader from '../components/nativebi/NativeBiPageHeader'
import { NbEmptyState } from '../components/nativebi/NativeBiState'
import NativeBiMiniBarList from '../components/nativebi/NativeBiMiniBarList'
import NativeBiFilterBar from '../components/nativebi/NativeBiFilterBar'
import { NbBarChart, NbLineChart, NbAreaChart, NbPieChart, NbScatterChart } from '../components/charts'
import type { ChartDataPoint } from '../components/charts'
import {
  useSalesOverview,
  useSalesMonthly,
  useSalesCustomers,
  useSalesItems,
  useSalesSalespersons,
  useSalesMartKpi,
  useSalesMartOpenOrders,
} from '../hooks/useNativeBiSales'
import { useBiSalesFulfillment, useBiSalesItemGroupSummary, useBiSalesWarehouseSummary } from '../hooks/useProcessBi'
import { useNativeBiFilters } from '../hooks/useNativeBiFilters'
import { useItemGroupOptions, useCustomerGroupOptions, useSalespersonOptions } from '../hooks/useFilterOptions'
import type {
  CustomerSales,
  ItemSales,
  SalespersonSales,
  NbPagedMeta,
  PaginationParams,
  OpenSalesOrderMart,
} from '../types/nativeBi'
import type { SalesFulfillment, SalesWarehouseSummary } from '../types/processBi'
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

type Tab = 'resumen' | 'tendencia' | 'grupos' | 'almacenes' | 'customers' | 'items' | 'salespersons' | 'fulfillment' | 'pipeline'

const VALID_TABS: Tab[] = ['resumen', 'tendencia', 'grupos', 'almacenes', 'customers', 'items', 'salespersons', 'fulfillment', 'pipeline']
const DEFAULT_TAB: Tab = 'resumen'

const LIMIT = 20
const EMPTY_META: NbPagedMeta = { limit: LIMIT, offset: 0, count: 0, hasMore: false }

function initPag(sortBy: string): PaginationParams {
  return { limit: LIMIT, offset: 0, sortBy, sortDir: 'desc' }
}

function TabButton({ label, active, onClick }: { id?: string; label: ReactNode; active: boolean; onClick: () => void }) {
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

function StatCard({ label, value, sub, loading }: { label: string; value: ReactNode; sub?: string; loading?: boolean }) {
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
  const [searchParams] = useSearchParams()
  const initialTab = searchParams.get('tab') as Tab | null
  const [tab, setTab] = useState<Tab>(
    initialTab && (VALID_TABS as string[]).includes(initialTab)
      ? (initialTab as Tab)
      : DEFAULT_TAB
  )
  const [custP, setCustP]     = useState<PaginationParams>(initPag('netSalesAmount'))
  const [itemP, setItemP]     = useState<PaginationParams>(initPag('grossSalesAmount'))
  const [spP, setSpP]         = useState<PaginationParams>(initPag('netSalesAmount'))
  const [overdueOnly, setOverdueOnly] = useState(false)
  const { range, setFrom, setTo } = useDateRangeFilter(12)

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
  const { data: monthly, isLoading: loadingMonthly } = useSalesMonthly({ dateFrom: filters.dateFrom, dateTo: filters.dateTo })
  const { data: groupData, isLoading: loadingGroups } = useBiSalesItemGroupSummary({
    dateFrom: filters.dateFrom,
    dateTo: filters.dateTo,
    itemGroupCodes: filters.itemGroupCodes,
  })
  const { data: custData, isLoading: loadingCust }     = useSalesCustomers(custP)
  const { data: itemData, isLoading: loadingItems }    = useSalesItems(itemP)
  const { data: spData, isLoading: loadingSp }         = useSalesSalespersons(spP)
  const { data: fulfillData, isLoading: loadingFulfill } = useBiSalesFulfillment(30)
  const { data: martKpi,    isLoading: loadingMartKpi } = useSalesMartKpi()
  const { data: openOrders, isLoading: loadingOrders }  = useSalesMartOpenOrders(overdueOnly)
  const { data: whSales, isLoading: loadingWhSales } = useBiSalesWarehouseSummary({
    dateFrom: filters.dateFrom,
    dateTo: filters.dateTo,
    itemGroupCodes: filters.itemGroupCodes,
  })

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

  const pipelineCols: ColumnDef<OpenSalesOrderMart>[] = [
    {
      key: 'docNum',
      label: '# Pedido',
      render: (r) => <span style={{ fontVariantNumeric: 'tabular-nums', fontWeight: 500 }}>{r.docNum}</span>,
    },
    {
      key: 'customer',
      label: 'Cliente',
      render: (r) => (
        <div>
          <div style={{ fontWeight: 500 }}>{r.cardName ?? r.cardCode}</div>
          {r.cardName && <div style={{ fontSize: 11.5, color: 'var(--c-text-faint)' }}>{r.cardCode}</div>}
        </div>
      ),
    },
    {
      key: 'salesPerson',
      label: 'Vendedor',
      render: (r) => <span>{r.salesPersonName ?? '—'}</span>,
    },
    {
      key: 'docDate',
      label: 'Fecha',
      render: (r) => fmtDate(r.docDate),
    },
    {
      key: 'docDueDate',
      label: 'Vencimiento',
      render: (r) => (
        <span style={{ color: r.isOverdue ? '#DC2626' : 'inherit' }}>
          {fmtDate(r.docDueDate)}
        </span>
      ),
    },
    {
      key: 'daysOpen',
      label: 'Días abierto',
      align: 'right',
      render: (r) => (
        <span style={{ fontVariantNumeric: 'tabular-nums', color: (r.daysOpen ?? 0) > 30 ? '#D97706' : 'inherit' }}>
          {r.daysOpen ?? '—'}
        </span>
      ),
    },
    {
      key: 'openAmount',
      label: 'Monto abierto',
      align: 'right',
      render: (r) => <span style={{ fontVariantNumeric: 'tabular-nums' }}>{fmtAmt(r.openAmount)}</span>,
    },
    {
      key: 'isOverdue',
      label: 'Estado',
      render: (r) => (
        <span style={{
          display: 'inline-block',
          padding: '2px 8px',
          borderRadius: 4,
          fontSize: 11.5,
          fontWeight: 600,
          color: '#fff',
          backgroundColor: r.isOverdue ? '#DC2626' : '#16A34A',
        }}>
          {r.isOverdue ? 'Vencido' : 'Vigente'}
        </span>
      ),
    },
  ]

  const overdueCount = openOrders?.filter((o) => o.isOverdue).length ?? 0

  const filteredMonthly = useMemo(() => {
    if (!monthly) return []
    return filterByRange(
      monthly.map(m => ({
        ...m,
        year: parseInt(m.salesMonth.slice(0, 4)),
        month: parseInt(m.salesMonth.slice(5, 7)),
      })),
      range
    )
  }, [monthly, range])

  const tabs: { id: Tab; label: ReactNode }[] = [
    { id: 'resumen',      label: 'Resumen' },
    { id: 'tendencia',    label: 'Tendencia' },
    { id: 'grupos',       label: 'Por Grupo' },
    { id: 'almacenes',    label: 'Por Almacén' },
    { id: 'customers',    label: 'Clientes' },
    { id: 'items',        label: 'Productos' },
    { id: 'salespersons', label: 'Vendedores' },
    { id: 'fulfillment',  label: 'Fulfillment' },
    {
      id: 'pipeline',
      label: (
        <span style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
          Pipeline
          {overdueCount > 0 && (
            <span style={{
              display: 'inline-flex',
              alignItems: 'center',
              justifyContent: 'center',
              minWidth: 18,
              height: 18,
              padding: '0 5px',
              borderRadius: 9,
              fontSize: 11,
              fontWeight: 700,
              color: '#fff',
              backgroundColor: '#DC2626',
            }}>
              {overdueCount}
            </span>
          )}
        </span>
      ),
    },
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

            {/* Trend chart */}
            {monthly && monthly.length > 0 && (
              <div style={{ padding: '0 0 16px' }}>
                <NbAreaChart
                  series={[{
                    name: 'Ventas netas',
                    data: monthly.slice().reverse().map((m): ChartDataPoint => ({
                      name: new Date(m.salesMonth + 'T00:00:00').toLocaleDateString('es-CL', { month: 'short', year: '2-digit' }),
                      value: m.netSalesAmount,
                    })),
                    color: 'var(--brand-primary, #2563EB)',
                  }]}
                  height={180}
                  loading={loadingMonthly}
                  valueFormatter={(v) => v.toLocaleString('es-CL', { maximumFractionDigits: 0 })}
                />
              </div>
            )}

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

            {/* MART KPI block — LTM aggregated (Sprint 3) */}
            {(martKpi || loadingMartKpi) && (
              <div style={{ marginBottom: 24, padding: '14px 16px', background: 'var(--c-surface-alt, #F8FAFC)', borderRadius: 8, border: '1px solid var(--c-border)' }}>
                <p style={{ fontSize: 12, fontWeight: 600, color: 'var(--c-text-muted)', marginBottom: 10, textTransform: 'uppercase', letterSpacing: '0.05em' }}>
                  KPIs últimos 12 meses (MART)
                </p>
                <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(140px, 1fr))', gap: 10 }}>
                  <StatCard
                    label="Crecimiento YoY"
                    value={martKpi ? `${martKpi.growthPct >= 0 ? '+' : ''}${martKpi.growthPct.toFixed(1)}%` : '—'}
                    loading={loadingMartKpi}
                  />
                  <StatCard
                    label="Tasa devolución"
                    value={martKpi ? `${martKpi.returnRatePct.toFixed(1)}%` : '—'}
                    loading={loadingMartKpi}
                  />
                  <StatCard
                    label="Pipeline abierto"
                    value={martKpi ? fmtAmt(martKpi.openOrdersAmount) : '—'}
                    sub={martKpi ? `${martKpi.openOrdersCount} pedidos · ${martKpi.overdueOrdersCount} vencidos` : undefined}
                    loading={loadingMartKpi}
                  />
                  <StatCard
                    label="Clientes activos LTM"
                    value={martKpi?.activeCustomersLtm ?? '—'}
                    loading={loadingMartKpi}
                  />
                </div>
              </div>
            )}

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

        {/* ── Tendencia ───────────────────────────────────────────────────── */}
        {tab === 'tendencia' && (
          <div style={{ padding: '20px' }}>
            <div style={{ marginBottom: 16 }}>
              <DateRangeSelector range={range} onFromChange={setFrom} onToChange={setTo} />
            </div>
            <NbLineChart
              series={[
                {
                  name: 'Ventas brutas',
                  data: filteredMonthly.slice().reverse().map((m): ChartDataPoint => ({
                    name: new Date(m.salesMonth + 'T00:00:00').toLocaleDateString('es-CL', { month: 'short', year: '2-digit' }),
                    value: m.grossSalesAmount,
                  })),
                  color: '#7C3AED',
                },
                {
                  name: 'Ventas netas',
                  data: filteredMonthly.slice().reverse().map((m): ChartDataPoint => ({
                    name: new Date(m.salesMonth + 'T00:00:00').toLocaleDateString('es-CL', { month: 'short', year: '2-digit' }),
                    value: m.netSalesAmount,
                  })),
                  color: 'var(--brand-primary, #2563EB)',
                },
              ]}
              height={300}
              loading={loadingMonthly}
              valueFormatter={(v) => v.toLocaleString('es-CL', { maximumFractionDigits: 0 })}
            />
            <div style={{ marginTop: 24, display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(160px, 1fr))', gap: 12 }}>
              {filteredMonthly.slice().reverse().map((m) => (
                <div key={m.salesMonth} className="db-stat-card">
                  <span className="db-stat-label">
                    {new Date(m.salesMonth + 'T00:00:00').toLocaleDateString('es-CL', { month: 'long', year: 'numeric' })}
                  </span>
                  <span className="db-stat-value" style={{ fontSize: 18, fontVariantNumeric: 'tabular-nums' }}>
                    {m.netSalesAmount.toLocaleString('es-CL', { maximumFractionDigits: 0 })}
                  </span>
                  <span style={{ fontSize: 11.5, color: 'var(--c-text-faint)' }}>
                    {m.invoiceCount} facturas · {m.activeCustomers} clientes
                  </span>
                </div>
              ))}
            </div>
          </div>
        )}

        {/* ── Por Grupo ───────────────────────────────────────────────────── */}
        {tab === 'grupos' && (
          <div style={{ padding: '20px' }}>
            {loadingGroups ? (
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 24 }}>
                <div className="cp-skeleton" style={{ height: 280, borderRadius: 8 }} />
                <div className="cp-skeleton" style={{ height: 280, borderRadius: 8 }} />
              </div>
            ) : !groupData || groupData.length === 0 ? (
              <NbEmptyState message="Sin datos de grupos de artículos disponibles." icon="table" />
            ) : (
              <>
                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 24, marginBottom: 24 }}>
                  {/* Donut: share by group */}
                  <div>
                    <p style={{ fontSize: 13, fontWeight: 600, marginBottom: 8, color: 'var(--c-text)' }}>
                      Participación por grupo
                    </p>
                    <NbPieChart
                      data={groupData.slice(0, 8).map((g): ChartDataPoint => ({
                        name: g.itemGroupName ?? g.itemGroupCode,
                        value: g.grossSales,
                      }))}
                      height={280}
                      valueFormatter={(v) => v.toLocaleString('es-CL', { maximumFractionDigits: 0 })}
                    />
                  </div>
                  {/* Bar: top groups */}
                  <div>
                    <p style={{ fontSize: 13, fontWeight: 600, marginBottom: 8, color: 'var(--c-text)' }}>
                      Ventas brutas por grupo
                    </p>
                    <NbBarChart
                      data={groupData.slice(0, 10).map((g): ChartDataPoint => ({
                        name: g.itemGroupName ?? g.itemGroupCode,
                        value: g.grossSales,
                      }))}
                      height={280}
                      valueFormatter={(v) => v.toLocaleString('es-CL', { maximumFractionDigits: 0 })}
                    />
                  </div>
                </div>
                {/* Table */}
                <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 13 }}>
                  <thead>
                    <tr style={{ borderBottom: '1px solid var(--c-border)' }}>
                      <th style={{ textAlign: 'left', padding: '8px 12px', color: 'var(--c-text-muted)', fontWeight: 500 }}>Grupo</th>
                      <th style={{ textAlign: 'right', padding: '8px 12px', color: 'var(--c-text-muted)', fontWeight: 500 }}>Ventas brutas</th>
                      <th style={{ textAlign: 'right', padding: '8px 12px', color: 'var(--c-text-muted)', fontWeight: 500 }}>Ventas netas</th>
                      <th style={{ textAlign: 'right', padding: '8px 12px', color: 'var(--c-text-muted)', fontWeight: 500 }}>Facturas</th>
                      <th style={{ textAlign: 'right', padding: '8px 12px', color: 'var(--c-text-muted)', fontWeight: 500 }}>SKUs</th>
                      <th style={{ textAlign: 'right', padding: '8px 12px', color: 'var(--c-text-muted)', fontWeight: 500 }}>Margen %</th>
                    </tr>
                  </thead>
                  <tbody>
                    {groupData.map((g, i) => (
                      <tr key={g.itemGroupCode} style={{ borderBottom: '1px solid var(--c-border)', height: 44, background: i % 2 === 1 ? 'var(--c-surface-alt, #F8FAFC)' : undefined }}>
                        <td style={{ padding: '8px 12px', fontWeight: 500 }}>{g.itemGroupName ?? g.itemGroupCode}</td>
                        <td style={{ padding: '8px 12px', textAlign: 'right', fontVariantNumeric: 'tabular-nums' }}>
                          {g.grossSales.toLocaleString('es-CL', { maximumFractionDigits: 0 })}
                        </td>
                        <td style={{ padding: '8px 12px', textAlign: 'right', fontVariantNumeric: 'tabular-nums' }}>
                          {g.netSales.toLocaleString('es-CL', { maximumFractionDigits: 0 })}
                        </td>
                        <td style={{ padding: '8px 12px', textAlign: 'right', fontVariantNumeric: 'tabular-nums' }}>{g.invoiceCount}</td>
                        <td style={{ padding: '8px 12px', textAlign: 'right', fontVariantNumeric: 'tabular-nums' }}>{g.skuCount}</td>
                        <td style={{ padding: '8px 12px', textAlign: 'right', fontVariantNumeric: 'tabular-nums', color: g.grossMarginPct >= 30 ? '#16A34A' : g.grossMarginPct >= 15 ? '#D97706' : 'var(--c-text-muted)' }}>
                          {g.grossMarginPct.toFixed(1)}%
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </>
            )}
          </div>
        )}

        {/* ── Por Almacén ─────────────────────────────────────────────────── */}
        {tab === 'almacenes' && (
          <div style={{ padding: 20 }}>
            {loadingWhSales ? (
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 24 }}>
                <div className="cp-skeleton" style={{ height: 260, borderRadius: 8 }} />
                <div className="cp-skeleton" style={{ height: 260, borderRadius: 8 }} />
              </div>
            ) : !whSales || whSales.length === 0 ? (
              <NbEmptyState message="Sin datos de almacén disponibles." icon="table" />
            ) : (
              <>
                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 24, marginBottom: 24 }}>
                  <div>
                    <p style={{ fontSize: 13, fontWeight: 600, marginBottom: 8, color: 'var(--c-text)' }}>
                      Ventas brutas por almacén
                    </p>
                    <NbBarChart
                      data={whSales.slice(0, 10).map((w): ChartDataPoint => ({
                        name: w.warehouseName ?? w.warehouseCode,
                        value: w.grossSales,
                      }))}
                      height={260}
                      valueFormatter={(v) => v.toLocaleString('es-CL', { maximumFractionDigits: 0 })}
                    />
                  </div>
                  <div>
                    <p style={{ fontSize: 13, fontWeight: 600, marginBottom: 8, color: 'var(--c-text)' }}>
                      Participación por almacén
                    </p>
                    <NbPieChart
                      data={whSales.slice(0, 8).map((w): ChartDataPoint => ({
                        name: w.warehouseName ?? w.warehouseCode,
                        value: w.grossSales,
                      }))}
                      height={260}
                      valueFormatter={(v) => v.toLocaleString('es-CL', { maximumFractionDigits: 0 })}
                    />
                  </div>
                </div>
                <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 13 }}>
                  <thead>
                    <tr style={{ borderBottom: '1px solid var(--c-border)' }}>
                      <th style={{ textAlign: 'left', padding: '8px 12px', color: 'var(--c-text-muted)', fontWeight: 500 }}>Almacén</th>
                      <th style={{ textAlign: 'right', padding: '8px 12px', color: 'var(--c-text-muted)', fontWeight: 500 }}>Ventas brutas</th>
                      <th style={{ textAlign: 'right', padding: '8px 12px', color: 'var(--c-text-muted)', fontWeight: 500 }}>Ventas netas</th>
                      <th style={{ textAlign: 'right', padding: '8px 12px', color: 'var(--c-text-muted)', fontWeight: 500 }}>Facturas</th>
                      <th style={{ textAlign: 'right', padding: '8px 12px', color: 'var(--c-text-muted)', fontWeight: 500 }}>SKUs</th>
                    </tr>
                  </thead>
                  <tbody>
                    {whSales.map((w: SalesWarehouseSummary, i: number) => (
                      <tr key={w.warehouseCode} style={{ borderBottom: '1px solid var(--c-border)', height: 44, background: i % 2 === 1 ? 'var(--c-surface-alt, #F8FAFC)' : undefined }}>
                        <td style={{ padding: '8px 12px', fontWeight: 500 }}>{w.warehouseName ?? w.warehouseCode}</td>
                        <td style={{ padding: '8px 12px', textAlign: 'right', fontVariantNumeric: 'tabular-nums' }}>
                          {w.grossSales.toLocaleString('es-CL', { maximumFractionDigits: 0 })}
                        </td>
                        <td style={{ padding: '8px 12px', textAlign: 'right', fontVariantNumeric: 'tabular-nums' }}>
                          {w.netSales.toLocaleString('es-CL', { maximumFractionDigits: 0 })}
                        </td>
                        <td style={{ padding: '8px 12px', textAlign: 'right', fontVariantNumeric: 'tabular-nums' }}>{w.invoiceCount}</td>
                        <td style={{ padding: '8px 12px', textAlign: 'right', fontVariantNumeric: 'tabular-nums' }}>{w.skuCount}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </>
            )}
          </div>
        )}

        {/* ── Clientes ────────────────────────────────────────────────────── */}
        {tab === 'customers' && (
          <>
            <div style={{ display: 'flex', justifyContent: 'flex-end', padding: '8px 20px 0' }}>
              <button
                onClick={() => exportXlsx('ventas-top-clientes', 'Top Clientes', (custData?.data ?? []).map(r => ({
                  'Código': r.cardCode,
                  'Cliente': r.cardName,
                  'Ventas Netas': r.netSalesAmount,
                  'N° Facturas': r.invoiceCount,
                  'Última Factura': r.lastInvoiceDate,
                })))}
                disabled={(custData?.data ?? []).length === 0}
                style={{ display: 'inline-flex', alignItems: 'center', gap: 6, padding: '6px 14px', borderRadius: 6, border: '1px solid var(--c-border)', background: '#fff', color: 'var(--c-text)', fontSize: 13, fontWeight: 500, cursor: 'pointer', fontFamily: 'inherit' }}
              >
                <Download size={14} />
                Exportar Excel
              </button>
            </div>
            {custData && custData.data.length > 0 && (
              <div style={{ padding: '16px 20px 0' }}>
                <p style={{ fontSize: 13, fontWeight: 600, marginBottom: 8, color: 'var(--c-text)' }}>
                  Dispersión: ticket promedio vs. volumen de facturas
                </p>
                <NbScatterChart
                  data={custData.data.slice(0, 50).map((c) => ({
                    x: c.invoiceCount,
                    y: c.avgTicketAmount,
                    name: c.cardName,
                    size: Math.max(6, Math.min(20, c.netSalesAmount / (totalCustNet / 20 || 1))),
                  }))}
                  height={220}
                  xLabel="Facturas"
                  yLabel="Ticket promedio"
                  xFormatter={(v) => String(Math.round(v))}
                  yFormatter={(v) => v.toLocaleString('es-CL', { maximumFractionDigits: 0 })}
                  color="var(--brand-primary, #2563EB)"
                />
                <div style={{ height: 1, background: 'var(--c-border)', margin: '12px 0 0' }} />
              </div>
            )}
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
          </>
        )}

        {/* ── Productos ───────────────────────────────────────────────────── */}
        {tab === 'items' && (
          <>
            <div style={{ display: 'flex', justifyContent: 'flex-end', padding: '8px 20px 0' }}>
              <button
                onClick={() => exportXlsx('ventas-top-items', 'Top Ítems', (itemData?.data ?? []).map(r => ({
                  'Código': r.itemCode,
                  'Ítem': r.itemName,
                  'Ventas Brutas': r.grossSalesAmount,
                  'Cantidad': r.quantitySold,
                  'N° Facturas': r.invoiceCount,
                  'Última Venta': r.lastSaleDate,
                })))}
                disabled={(itemData?.data ?? []).length === 0}
                style={{ display: 'inline-flex', alignItems: 'center', gap: 6, padding: '6px 14px', borderRadius: 6, border: '1px solid var(--c-border)', background: '#fff', color: 'var(--c-text)', fontSize: 13, fontWeight: 500, cursor: 'pointer', fontFamily: 'inherit' }}
              >
                <Download size={14} />
                Exportar Excel
              </button>
            </div>
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
          </>
        )}

        {/* ── Vendedores ──────────────────────────────────────────────────── */}
        {tab === 'salespersons' && (
          <>
            {sps.length > 0 && (
              <div style={{ padding: '16px 20px 0' }}>
                <NbBarChart
                  data={sps.slice(0, 10).map((sp): ChartDataPoint => ({
                    name: sp.salesPersonName,
                    value: sp.netSalesAmount,
                  }))}
                  height={220}
                  valueFormatter={(v) => v.toLocaleString('es-CL', { maximumFractionDigits: 0 })}
                />
                <div style={{ height: 1, background: 'var(--c-border)', margin: '12px 0 0' }} />
              </div>
            )}
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
          </>
        )}

        {/* ── Pipeline (Sprint 3 MART) ────────────────────────────────────── */}
        {tab === 'pipeline' && (
          <>
            {/* KPI header */}
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(150px, 1fr))', gap: 12, padding: '16px 20px 0' }}>
              <StatCard
                label="Pedidos abiertos"
                value={openOrders?.length ?? '—'}
                loading={loadingOrders}
              />
              <StatCard
                label="Monto total abierto"
                value={openOrders ? fmtAmt(openOrders.reduce((s, o) => s + o.openAmount, 0)) : '—'}
                loading={loadingOrders}
              />
              <StatCard
                label="Vencidos"
                value={overdueCount > 0 ? overdueCount : (loadingOrders ? '—' : '0')}
                loading={loadingOrders}
              />
              <StatCard
                label="Monto vencido"
                value={openOrders ? fmtAmt(openOrders.filter((o) => o.isOverdue).reduce((s, o) => s + o.openAmount, 0)) : '—'}
                loading={loadingOrders}
              />
            </div>

            {/* Overdue toggle */}
            <div style={{ padding: '12px 20px 0', display: 'flex', alignItems: 'center', gap: 8 }}>
              <label style={{ fontSize: 13, color: 'var(--c-text-muted)', display: 'flex', alignItems: 'center', gap: 6, cursor: 'pointer' }}>
                <input
                  type="checkbox"
                  checked={overdueOnly}
                  onChange={(e) => setOverdueOnly(e.target.checked)}
                  style={{ accentColor: 'var(--brand-primary, #2563EB)', width: 14, height: 14, cursor: 'pointer' }}
                />
                Mostrar solo vencidos
              </label>
            </div>
            <div style={{ height: 1, background: 'var(--c-border)', margin: '12px 0 0' }} />

            {loadingOrders ? (
              <div style={{ padding: 24 }}>
                {Array.from({ length: 5 }).map((_, i) => (
                  <div key={i} className="cp-skeleton" style={{ height: 44, marginBottom: 4 }} />
                ))}
              </div>
            ) : !openOrders || openOrders.length === 0 ? (
              <NbEmptyState
                message={overdueOnly ? 'No hay pedidos vencidos en este momento.' : 'No hay pedidos abiertos. El MART se actualiza tras cada sincronización.'}
                icon="chart"
              />
            ) : (
              <SortableTable
                data={openOrders}
                columns={pipelineCols}
                meta={{ limit: openOrders.length, offset: 0, count: openOrders.length, hasMore: false }}
                isLoading={false}
                rowKey={(r) => String(r.docNum)}
                onPageChange={() => {}}
                onSortChange={() => {}}
              />
            )}
          </>
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
