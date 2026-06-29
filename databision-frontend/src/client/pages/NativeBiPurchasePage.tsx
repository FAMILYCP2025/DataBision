import { useState, useMemo, type ReactNode } from 'react'
import { Download } from 'lucide-react'
import { exportXlsx } from '../utils/exportXlsx'
import SortableTable, { type ColumnDef } from '../components/nativebi/SortableTable'
import NativeBiPageHeader from '../components/nativebi/NativeBiPageHeader'
import { NbEmptyState } from '../components/nativebi/NativeBiState'
import { NbBarChart, NbAreaChart } from '../components/charts'
import type { ChartDataPoint } from '../components/charts'
import {
  usePurchaseMartKpi,
  usePurchaseMartByPeriod,
  usePurchaseMartTopSuppliers,
  usePurchaseMartTopItems,
  usePurchaseMartOpenOrders,
} from '../hooks/useNativeBiPurchase'
import type {
  TopSupplierMart,
  TopPurchaseItemMart,
  OpenPurchaseOrderMart,
  NbPagedMeta,
} from '../types/nativeBi'

function fmtAmt(n: number) {
  return n.toLocaleString('es-CL', { maximumFractionDigits: 0 })
}

function fmtDate(iso: string | null) {
  if (!iso) return '—'
  return new Date(iso + 'T00:00:00').toLocaleDateString('es-CL', {
    day: '2-digit', month: 'short', year: 'numeric',
  })
}

type Tab = 'resumen' | 'tendencia' | 'proveedores' | 'articulos' | 'pipeline'

function fakeMeta(count: number): NbPagedMeta {
  return { limit: count, offset: 0, count, hasMore: false }
}

function TabButton({ label, active, onClick }: { label: ReactNode; active: boolean; onClick: () => void }) {
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

export default function NativeBiPurchasePage() {
  const [tab, setTab] = useState<Tab>('resumen')
  const [overdueOnly, setOverdueOnly] = useState(false)
  const [periodMonths, setPeriodMonths] = useState(12)

  const [supSort, setSupSort]   = useState<{ key: string; dir: 'asc' | 'desc' }>({ key: 'netPurchases', dir: 'desc' })
  const [itemSort, setItemSort] = useState<{ key: string; dir: 'asc' | 'desc' }>({ key: 'grossPurchases', dir: 'desc' })
  const [ordSort, setOrdSort]   = useState<{ key: string; dir: 'asc' | 'desc' }>({ key: 'isOverdue', dir: 'desc' })

  const { data: kpi,       isLoading: loadingKpi }       = usePurchaseMartKpi()
  const { data: byPeriod,  isLoading: loadingPeriod }    = usePurchaseMartByPeriod(periodMonths)
  const { data: suppliers, isLoading: loadingSuppliers } = usePurchaseMartTopSuppliers(20)
  const { data: items,     isLoading: loadingItems }     = usePurchaseMartTopItems(20)
  const { data: orders,    isLoading: loadingOrders }    = usePurchaseMartOpenOrders(overdueOnly)

  // ── Derived ────────────────────────────────────────────────────────────────
  const periodChartData: ChartDataPoint[] = (byPeriod ?? []).map(p => ({
    name: `${p.year}-${String(p.month).padStart(2, '0')}`,
    value: p.grossPurchases,
  }))

  function localSort<T extends object>(arr: T[], key: string, dir: 'asc' | 'desc'): T[] {
    return [...arr].sort((a, b) => {
      const av = (a as Record<string, unknown>)[key] ?? ''
      const bv = (b as Record<string, unknown>)[key] ?? ''
      if (av < bv) return dir === 'asc' ? -1 : 1
      if (av > bv) return dir === 'asc' ? 1 : -1
      return 0
    })
  }

  const sortedSuppliers = useMemo(
    () => localSort(suppliers ?? [], supSort.key, supSort.dir),
    [suppliers, supSort],
  )
  const sortedItems = useMemo(
    () => localSort(items ?? [], itemSort.key, itemSort.dir),
    [items, itemSort],
  )
  const sortedOrders = useMemo(
    () => localSort(orders ?? [], ordSort.key, ordSort.dir),
    [orders, ordSort],
  )

  // ── Columns ────────────────────────────────────────────────────────────────
  const supplierCols: ColumnDef<TopSupplierMart>[] = [
    {
      key: 'rank',
      label: '#',
      render: (_r, i) => (
        <span style={{ fontSize: 12, color: 'var(--c-text-faint)', fontWeight: 700, fontVariantNumeric: 'tabular-nums' }}>
          {(i ?? 0) + 1}
        </span>
      ),
    },
    { key: 'cardName', label: 'Proveedor',      render: r => <span style={{ fontWeight: 500 }}>{r.cardName ?? r.cardCode}</span> },
    { key: 'grossPurchases',  label: 'Compras brutas', sortKey: 'grossPurchases',  render: r => fmtAmt(r.grossPurchases) },
    { key: 'netPurchases',    label: 'Compras netas',  sortKey: 'netPurchases',    render: r => fmtAmt(r.netPurchases) },
    { key: 'invoiceCount',    label: 'Facturas',        sortKey: 'invoiceCount',    render: r => r.invoiceCount },
    { key: 'lastInvoiceDate', label: 'Última factura',                             render: r => fmtDate(r.lastInvoiceDate) },
    { key: 'dpoDays',         label: 'DPO (días)',      sortKey: 'dpoDays',         render: r => r.dpoDays != null ? r.dpoDays.toFixed(0) : '—' },
  ]

  const itemCols: ColumnDef<TopPurchaseItemMart>[] = [
    {
      key: 'rank',
      label: '#',
      render: (_r, i) => (
        <span style={{ fontSize: 12, color: 'var(--c-text-faint)', fontWeight: 700, fontVariantNumeric: 'tabular-nums' }}>
          {(i ?? 0) + 1}
        </span>
      ),
    },
    { key: 'itemName',          label: 'Artículo',        render: r => <span style={{ fontWeight: 500 }}>{r.itemName ?? r.itemCode}</span> },
    { key: 'itemCode',          label: 'Código',          render: r => <span style={{ fontFamily: 'monospace', fontSize: 12 }}>{r.itemCode}</span> },
    { key: 'grossPurchases',    label: 'Monto comprado',  sortKey: 'grossPurchases',    render: r => fmtAmt(r.grossPurchases) },
    { key: 'quantityPurchased', label: 'Cantidad',        sortKey: 'quantityPurchased', render: r => r.quantityPurchased.toLocaleString('es-CL', { maximumFractionDigits: 2 }) },
    { key: 'invoiceCount',      label: 'Facturas',        sortKey: 'invoiceCount',      render: r => r.invoiceCount },
    { key: 'avgUnitPrice',      label: 'Precio prom.',    sortKey: 'avgUnitPrice',      render: r => fmtAmt(r.avgUnitPrice) },
  ]

  const orderCols: ColumnDef<OpenPurchaseOrderMart>[] = [
    { key: 'docNum',     label: 'N° OC',        render: r => r.docNum },
    { key: 'cardName',   label: 'Proveedor',    render: r => r.cardName ?? r.cardCode ?? '—' },
    { key: 'docDate',    label: 'Emisión',      render: r => fmtDate(r.docDate) },
    { key: 'docDueDate', label: 'Vencimiento',  render: r => fmtDate(r.docDueDate) },
    { key: 'docTotal',   label: 'Total',        sortKey: 'docTotal',   render: r => fmtAmt(r.docTotal) },
    { key: 'openAmount', label: 'Pendiente',    sortKey: 'openAmount', render: r => fmtAmt(r.openAmount) },
    { key: 'daysOpen',   label: 'Días abierta', sortKey: 'daysOpen',   render: r => r.daysOpen ?? '—' },
    {
      key: 'isOverdue',
      label: 'Estado',
      sortKey: 'isOverdue',
      render: r => r.isOverdue
        ? <span style={{ color: '#DC2626', fontWeight: 600, fontSize: 12 }}>Vencida</span>
        : <span style={{ color: '#16A34A', fontWeight: 600, fontSize: 12 }}>Vigente</span>,
    },
  ]

  return (
    <div className="nb-page">
      <NativeBiPageHeader
        title="Compras MART"
        description="Análisis de compras y proveedores (últimos 12 meses)"
      />

      {/* Tab bar */}
      <div
        role="tablist"
        style={{
          display: 'flex',
          borderBottom: '1px solid var(--c-border)',
          marginBottom: 24,
          overflowX: 'auto',
          gap: 0,
        }}
      >
        <TabButton label="Resumen"     active={tab === 'resumen'}     onClick={() => setTab('resumen')} />
        <TabButton label="Tendencia"   active={tab === 'tendencia'}   onClick={() => setTab('tendencia')} />
        <TabButton label="Proveedores" active={tab === 'proveedores'} onClick={() => setTab('proveedores')} />
        <TabButton label="Artículos"   active={tab === 'articulos'}   onClick={() => setTab('articulos')} />
        <TabButton label="Pipeline OC" active={tab === 'pipeline'}    onClick={() => setTab('pipeline')} />
      </div>

      {/* ── RESUMEN ─────────────────────────────────────────────────────────── */}
      {tab === 'resumen' && (
        <div>
          <div className="db-stat-grid" style={{ marginBottom: 24 }}>
            <StatCard
              label="Compras brutas LTM"
              value={`$${fmtAmt(kpi?.grossPurchasesLtm ?? 0)}`}
              sub={kpi ? `vs período anterior: ${kpi.growthPct >= 0 ? '+' : ''}${kpi.growthPct.toFixed(1)}%` : undefined}
              loading={loadingKpi}
            />
            <StatCard
              label="Ticket promedio LTM"
              value={`$${fmtAmt(kpi?.avgTicketLtm ?? 0)}`}
              loading={loadingKpi}
            />
            <StatCard
              label="Proveedores activos LTM"
              value={kpi?.activeSuppliersLtm ?? '—'}
              loading={loadingKpi}
            />
            <StatCard
              label="OC abiertas"
              value={kpi?.openOrdersCount ?? '—'}
              sub={kpi ? `Pendiente: $${fmtAmt(kpi.openOrdersAmount)}` : undefined}
              loading={loadingKpi}
            />
            <StatCard
              label="OC vencidas"
              value={kpi?.overdueOrdersCount ?? '—'}
              loading={loadingKpi}
            />
          </div>

          {!loadingKpi && !kpi && (
            <NbEmptyState message="Sin datos de compras. Ejecuta --object OPCH --send para sincronizar facturas de proveedores." />
          )}

          {byPeriod && byPeriod.length > 0 && (
            <div style={{ marginTop: 8 }}>
              <p style={{ fontSize: 13, color: 'var(--c-text-muted)', marginBottom: 8, fontWeight: 500 }}>
                Compras brutas — últimos {periodMonths} meses
              </p>
              <NbBarChart data={periodChartData} height={200} />
            </div>
          )}
        </div>
      )}

      {/* ── TENDENCIA ────────────────────────────────────────────────────────── */}
      {tab === 'tendencia' && (
        <div>
          <div style={{ display: 'flex', gap: 8, marginBottom: 16, alignItems: 'center' }}>
            <span style={{ fontSize: 13, color: 'var(--c-text-muted)' }}>Período:</span>
            {([6, 12, 24] as const).map(m => (
              <button
                key={m}
                onClick={() => setPeriodMonths(m)}
                style={{
                  padding: '4px 12px',
                  borderRadius: 6,
                  border: '1px solid var(--c-border)',
                  background: periodMonths === m ? 'var(--brand-primary, #2563EB)' : 'white',
                  color: periodMonths === m ? 'white' : 'var(--c-text)',
                  fontSize: 12,
                  fontWeight: 500,
                  cursor: 'pointer',
                }}
              >
                {m}M
              </button>
            ))}
          </div>

          {loadingPeriod && <div className="cp-skeleton" style={{ height: 220, borderRadius: 8 }} />}

          {!loadingPeriod && (!byPeriod || byPeriod.length === 0) && (
            <NbEmptyState message="Sin datos de tendencia. Ejecuta --object OPCH --send para sincronizar facturas de proveedores." />
          )}

          {byPeriod && byPeriod.length > 0 && (
            <>
              <NbAreaChart series={[{ name: 'Compras brutas', data: periodChartData }]} height={240} />
              <div style={{ marginTop: 24 }}>
                <SortableTable
                  columns={[
                    { key: 'period',           label: 'Mes',            render: r => `${r.year}-${String(r.month).padStart(2, '0')}` },
                    { key: 'grossPurchases',   label: 'Compras brutas', sortKey: 'grossPurchases',   render: r => fmtAmt(r.grossPurchases) },
                    { key: 'creditMemoAmount', label: 'NC proveedores', sortKey: 'creditMemoAmount', render: r => fmtAmt(r.creditMemoAmount) },
                    { key: 'netPurchases',     label: 'Compras netas',  sortKey: 'netPurchases',     render: r => fmtAmt(r.netPurchases) },
                    { key: 'invoiceCount',     label: 'Facturas',       sortKey: 'invoiceCount',     render: r => r.invoiceCount },
                    { key: 'activeSuppliers',  label: 'Proveedores',    sortKey: 'activeSuppliers',  render: r => r.activeSuppliers },
                    { key: 'avgTicket',        label: 'Ticket prom.',   sortKey: 'avgTicket',        render: r => fmtAmt(r.avgTicket) },
                  ]}
                  data={byPeriod}
                  meta={fakeMeta(byPeriod.length)}
                  sortBy="period"
                  sortDir="asc"
                  onPageChange={() => undefined}
                  onSortChange={() => undefined}
                  rowKey={r => `${r.year}-${r.month}`}
                />
              </div>
            </>
          )}
        </div>
      )}

      {/* ── PROVEEDORES ──────────────────────────────────────────────────────── */}
      {tab === 'proveedores' && (
        <div>
          <div style={{ display: 'flex', justifyContent: 'flex-end', marginBottom: 8 }}>
            <button
              onClick={() => exportXlsx('compras-top-proveedores', 'Top Proveedores', sortedSuppliers.map(r => ({
                'Código': r.cardCode,
                'Proveedor': r.cardName,
                'Compras Brutas': r.grossPurchases,
                'Compras Netas': r.netPurchases,
                'N° Facturas': r.invoiceCount,
                'Última Factura': r.lastInvoiceDate,
                'DPO (días)': r.dpoDays,
              })))}
              disabled={sortedSuppliers.length === 0}
              style={{ display: 'inline-flex', alignItems: 'center', gap: 6, padding: '6px 14px', borderRadius: 6, border: '1px solid var(--c-border)', background: '#fff', color: 'var(--c-text)', fontSize: 13, fontWeight: 500, cursor: 'pointer', fontFamily: 'inherit' }}
            >
              <Download size={14} />
              Exportar Excel
            </button>
          </div>
          {loadingSuppliers && <div className="cp-skeleton" style={{ height: 300, borderRadius: 8 }} />}

          {!loadingSuppliers && (!suppliers || suppliers.length === 0) && (
            <NbEmptyState message="Sin datos de proveedores. Ejecuta --object OPCH --send para sincronizar facturas de proveedores." />
          )}

          {suppliers && suppliers.length > 0 && (
            <SortableTable
              columns={supplierCols}
              data={sortedSuppliers}
              meta={fakeMeta(sortedSuppliers.length)}
              sortBy={supSort.key}
              sortDir={supSort.dir}
              onPageChange={() => undefined}
              onSortChange={(k, d) => setSupSort({ key: k, dir: d })}
              rowKey={r => r.cardCode}
            />
          )}
        </div>
      )}

      {/* ── ARTÍCULOS ────────────────────────────────────────────────────────── */}
      {tab === 'articulos' && (
        <div>
          <div style={{ display: 'flex', justifyContent: 'flex-end', marginBottom: 8 }}>
            <button
              onClick={() => exportXlsx('compras-top-articulos', 'Top Artículos', sortedItems.map(r => ({
                'Código': r.itemCode,
                'Artículo': r.itemName,
                'Grupo': r.itemGroupName,
                'Monto Comprado': r.grossPurchases,
                'Cantidad': r.quantityPurchased,
                'N° Facturas': r.invoiceCount,
                'Precio Prom.': r.avgUnitPrice,
              })))}
              disabled={sortedItems.length === 0}
              style={{ display: 'inline-flex', alignItems: 'center', gap: 6, padding: '6px 14px', borderRadius: 6, border: '1px solid var(--c-border)', background: '#fff', color: 'var(--c-text)', fontSize: 13, fontWeight: 500, cursor: 'pointer', fontFamily: 'inherit' }}
            >
              <Download size={14} />
              Exportar Excel
            </button>
          </div>
          {loadingItems && <div className="cp-skeleton" style={{ height: 300, borderRadius: 8 }} />}

          {!loadingItems && (!items || items.length === 0) && (
            <NbEmptyState message="Sin datos de artículos. Ejecuta --object OPCH --send y --object PCH1 --send para sincronizar líneas de facturas." />
          )}

          {items && items.length > 0 && (
            <SortableTable
              columns={itemCols}
              data={sortedItems}
              meta={fakeMeta(sortedItems.length)}
              sortBy={itemSort.key}
              sortDir={itemSort.dir}
              onPageChange={() => undefined}
              onSortChange={(k, d) => setItemSort({ key: k, dir: d })}
              rowKey={r => r.itemCode}
            />
          )}
        </div>
      )}

      {/* ── PIPELINE OC ──────────────────────────────────────────────────────── */}
      {tab === 'pipeline' && (
        <div>
          <div style={{ display: 'flex', gap: 8, marginBottom: 16, alignItems: 'center' }}>
            <label style={{ display: 'flex', alignItems: 'center', gap: 6, fontSize: 13, color: 'var(--c-text-muted)', cursor: 'pointer' }}>
              <input
                type="checkbox"
                checked={overdueOnly}
                onChange={e => setOverdueOnly(e.target.checked)}
                style={{ accentColor: 'var(--brand-primary, #2563EB)' }}
              />
              Solo vencidas
            </label>
          </div>

          {loadingOrders && <div className="cp-skeleton" style={{ height: 300, borderRadius: 8 }} />}

          {!loadingOrders && (!orders || orders.length === 0) && (
            <NbEmptyState
              message={overdueOnly
                ? 'No hay órdenes de compra vencidas.'
                : 'Sin órdenes de compra abiertas. Ejecuta --object OPOR --send para sincronizar.'}
            />
          )}

          {orders && orders.length > 0 && (
            <SortableTable
              columns={orderCols}
              data={sortedOrders}
              meta={fakeMeta(sortedOrders.length)}
              sortBy={ordSort.key}
              sortDir={ordSort.dir}
              onPageChange={() => undefined}
              onSortChange={(k, d) => setOrdSort({ key: k, dir: d })}
              rowKey={r => String(r.docNum)}
            />
          )}
        </div>
      )}
    </div>
  )
}
