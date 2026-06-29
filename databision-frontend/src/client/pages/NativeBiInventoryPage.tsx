import { useState, useMemo, type ReactNode } from 'react'
import { useSearchParams } from 'react-router-dom'
import { Download } from 'lucide-react'
import { exportXlsx } from '../utils/exportXlsx'
import { useDateRangeFilter, filterByRange } from '../hooks/useDateRangeFilter'
import DateRangeSelector from '../components/nativebi/DateRangeSelector'
import SortableTable, { type ColumnDef } from '../components/nativebi/SortableTable'
import NativeBiPageHeader from '../components/nativebi/NativeBiPageHeader'
import { NbEmptyState } from '../components/nativebi/NativeBiState'
import { NbBarChart, NbAreaChart } from '../components/charts'
import type { ChartDataPoint } from '../components/charts'
import {
  useInventoryMartKpi,
  useInventoryMartSnapshot,
  useInventoryMartMovement,
  useInventoryMartSlowMoving,
  useInventoryMartWarehouses,
} from '../hooks/useNativeBiInventory'
import type {
  InventorySnapshotItem,
  InventoryMovementKpi,
  SlowMovingItem,
  WarehouseStock,
  NbPagedMeta,
} from '../types/nativeBi'

function fmtAmt(n: number) {
  return n.toLocaleString('es-CL', { maximumFractionDigits: 0 })
}

function fmtQty(n: number) {
  return n.toLocaleString('es-CL', { maximumFractionDigits: 2 })
}

function fmtDate(iso: string | null) {
  if (!iso) return '—'
  return new Date(iso + 'T00:00:00').toLocaleDateString('es-CL', {
    day: '2-digit', month: 'short', year: 'numeric',
  })
}

type Tab = 'resumen' | 'stock' | 'movimientos' | 'almacenes' | 'slow-moving'

const VALID_TABS: Tab[] = ['resumen', 'stock', 'movimientos', 'almacenes', 'slow-moving']
const DEFAULT_TAB: Tab = 'resumen'

function fakeMeta(count: number): NbPagedMeta {
  return { limit: count, offset: 0, count, hasMore: false }
}

function localSort<T extends object>(arr: T[], key: string, dir: 'asc' | 'desc'): T[] {
  return [...arr].sort((a, b) => {
    const av = (a as Record<string, unknown>)[key] ?? ''
    const bv = (b as Record<string, unknown>)[key] ?? ''
    if (av < bv) return dir === 'asc' ? -1 : 1
    if (av > bv) return dir === 'asc' ? 1 : -1
    return 0
  })
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

export default function NativeBiInventoryPage() {
  const [searchParams] = useSearchParams()
  const initialTab = searchParams.get('tab') as Tab | null
  const [tab, setTab] = useState<Tab>(
    initialTab && (VALID_TABS as string[]).includes(initialTab)
      ? (initialTab as Tab)
      : DEFAULT_TAB
  )
  const { range, setFrom, setTo } = useDateRangeFilter(12)
  const [minDays, setMinDays] = useState(90)

  const [stockSort,   setStockSort]   = useState<{ key: string; dir: 'asc' | 'desc' }>({ key: 'stockValue',         dir: 'desc' })
  const [whSort,      setWhSort]      = useState<{ key: string; dir: 'asc' | 'desc' }>({ key: 'totalStockValue',    dir: 'desc' })
  const [slowSort,    setSlowSort]    = useState<{ key: string; dir: 'asc' | 'desc' }>({ key: 'daysWithoutMovement', dir: 'desc' })

  const { data: kpi,       isLoading: loadingKpi }     = useInventoryMartKpi()
  const { data: snapshot,  isLoading: loadingSnapshot } = useInventoryMartSnapshot(50)
  const { data: movement,  isLoading: loadingMovement } = useInventoryMartMovement(24)
  const { data: slowItems, isLoading: loadingSlowItems } = useInventoryMartSlowMoving(minDays)
  const { data: warehouses, isLoading: loadingWarehouses } = useInventoryMartWarehouses()

  // ── Derived ────────────────────────────────────────────────────────────────

  const topStockChartData: ChartDataPoint[] = (snapshot ?? [])
    .slice(0, 10)
    .map(s => ({ name: s.itemName ?? s.itemCode, value: s.stockValue }))

  const filteredMovement = useMemo(
    () => filterByRange(movement ?? [], range),
    [movement, range],
  )

  const movementInbound: ChartDataPoint[] = filteredMovement.map(m => ({
    name: `${m.year}-${String(m.month).padStart(2, '0')}`,
    value: m.inboundValue,
  }))

  const movementOutbound: ChartDataPoint[] = filteredMovement.map(m => ({
    name: `${m.year}-${String(m.month).padStart(2, '0')}`,
    value: m.outboundValue,
  }))

  const sortedStock = useMemo(
    () => localSort(snapshot ?? [], stockSort.key, stockSort.dir),
    [snapshot, stockSort],
  )
  const sortedWarehouses = useMemo(
    () => localSort(warehouses ?? [], whSort.key, whSort.dir),
    [warehouses, whSort],
  )
  const sortedSlowItems = useMemo(
    () => localSort(slowItems ?? [], slowSort.key, slowSort.dir),
    [slowItems, slowSort],
  )

  // ── Columns ────────────────────────────────────────────────────────────────

  const stockCols: ColumnDef<InventorySnapshotItem>[] = [
    { key: 'itemCode',  label: 'Código',        render: r => <span style={{ fontFamily: 'monospace', fontSize: 12 }}>{r.itemCode}</span> },
    { key: 'itemName',  label: 'Artículo',      render: r => <span style={{ fontWeight: 500 }}>{r.itemName ?? r.itemCode}</span> },
    { key: 'onHand',    label: 'En Stock',      sortKey: 'onHand',    align: 'right', render: r => fmtQty(r.onHand) },
    { key: 'committed', label: 'Comprometido',  sortKey: 'committed', align: 'right', render: r => fmtQty(r.committed) },
    { key: 'available', label: 'Disponible',    sortKey: 'available', align: 'right', render: r => fmtQty(r.available) },
    { key: 'avgPrice',  label: 'Precio Prom.',  sortKey: 'avgPrice',  align: 'right', render: r => fmtAmt(r.avgPrice) },
    { key: 'stockValue',label: 'Valor Stock',   sortKey: 'stockValue',align: 'right', render: r => fmtAmt(r.stockValue) },
  ]

  const movKpiCols: ColumnDef<InventoryMovementKpi>[] = [
    { key: 'period',         label: 'Período',         render: r => `${r.year}-${String(r.month).padStart(2, '0')}` },
    { key: 'inboundQty',     label: 'Entradas (u.)',   sortKey: 'inboundQty',     align: 'right', render: r => fmtQty(r.inboundQty) },
    { key: 'outboundQty',    label: 'Salidas (u.)',    sortKey: 'outboundQty',    align: 'right', render: r => fmtQty(r.outboundQty) },
    { key: 'inboundValue',   label: 'Valor entrada',  sortKey: 'inboundValue',   align: 'right', render: r => fmtAmt(r.inboundValue) },
    { key: 'outboundValue',  label: 'Valor salida',   sortKey: 'outboundValue',  align: 'right', render: r => fmtAmt(r.outboundValue) },
    { key: 'transactionCount', label: 'Transacciones', sortKey: 'transactionCount', align: 'right', render: r => r.transactionCount },
  ]

  const whCols: ColumnDef<WarehouseStock>[] = [
    { key: 'warehouseCode',  label: 'Código',        render: r => <span style={{ fontFamily: 'monospace', fontSize: 12 }}>{r.warehouseCode}</span> },
    { key: 'warehouseName',  label: 'Almacén',       render: r => r.warehouseName ?? r.warehouseCode },
    { key: 'totalItems',     label: 'Artículos',     sortKey: 'totalItems',     align: 'right', render: r => r.totalItems },
    { key: 'totalOnHand',    label: 'Stock Total',   sortKey: 'totalOnHand',    align: 'right', render: r => fmtQty(r.totalOnHand) },
    { key: 'totalStockValue',label: 'Valor Stock',   sortKey: 'totalStockValue',align: 'right', render: r => fmtAmt(r.totalStockValue) },
    { key: 'itemsBelowMin',  label: 'Bajo Mínimo',  sortKey: 'itemsBelowMin',  align: 'right', render: r => (
      r.itemsBelowMin > 0
        ? <span style={{ color: '#DC2626', fontWeight: 600 }}>{r.itemsBelowMin}</span>
        : <span style={{ color: '#16A34A' }}>{r.itemsBelowMin}</span>
    )},
  ]

  const slowCols: ColumnDef<SlowMovingItem>[] = [
    { key: 'itemCode',            label: 'Código',              render: r => <span style={{ fontFamily: 'monospace', fontSize: 12 }}>{r.itemCode}</span> },
    { key: 'itemName',            label: 'Artículo',            render: r => <span style={{ fontWeight: 500 }}>{r.itemName ?? r.itemCode}</span> },
    { key: 'itemGroupName',       label: 'Grupo',               render: r => r.itemGroupName ?? '—' },
    { key: 'onHand',              label: 'En Stock',            sortKey: 'onHand',              align: 'right', render: r => fmtQty(r.onHand) },
    { key: 'stockValue',          label: 'Valor Stock',         sortKey: 'stockValue',          align: 'right', render: r => fmtAmt(r.stockValue) },
    { key: 'lastMovementDate',    label: 'Último movimiento',   render: r => fmtDate(r.lastMovementDate) },
    {
      key: 'daysWithoutMovement',
      label: 'Días sin mov.',
      sortKey: 'daysWithoutMovement',
      align: 'right',
      render: r => (
        <span style={{
          color: r.daysWithoutMovement >= 180 ? '#DC2626' : r.daysWithoutMovement >= 90 ? '#D97706' : undefined,
          fontWeight: r.daysWithoutMovement >= 90 ? 600 : undefined,
          fontVariantNumeric: 'tabular-nums',
        }}>
          {r.daysWithoutMovement}
        </span>
      ),
    },
  ]

  return (
    <div className="nb-page">
      <NativeBiPageHeader
        title="Inventario MART"
        description="Stock valorado, movimientos y análisis de rotación"
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
        <TabButton label="Resumen"      active={tab === 'resumen'}      onClick={() => setTab('resumen')} />
        <TabButton label="Stock"        active={tab === 'stock'}        onClick={() => setTab('stock')} />
        <TabButton label="Movimientos"  active={tab === 'movimientos'}  onClick={() => setTab('movimientos')} />
        <TabButton label="Almacenes"    active={tab === 'almacenes'}    onClick={() => setTab('almacenes')} />
        <TabButton
          label={
            <span style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
              Slow-Moving
              {(slowItems?.length ?? 0) > 0 && (
                <span style={{ background: '#DC2626', color: '#fff', borderRadius: 9, fontSize: 10, padding: '1px 6px', fontWeight: 700 }}>
                  {slowItems!.length}
                </span>
              )}
            </span>
          }
          active={tab === 'slow-moving'}
          onClick={() => setTab('slow-moving')}
        />
      </div>

      {/* ── Tab: Resumen ────────────────────────────────────────────────────── */}
      {tab === 'resumen' && (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 24 }}>
          <div className="nb-stat-grid">
            <StatCard label="Valor total stock"   value={`$${fmtAmt(kpi?.totalStockValue ?? 0)}`}   loading={loadingKpi} />
            <StatCard label="Total artículos"     value={kpi?.totalItems ?? 0}                        loading={loadingKpi} />
            <StatCard label="Slow-moving"         value={kpi?.slowMovingItemsCount ?? 0}              loading={loadingKpi} sub="artículos sin movimiento 90+ días" />
            <StatCard label="Valor slow-moving"   value={`$${fmtAmt(kpi?.slowMovingStockValue ?? 0)}`} loading={loadingKpi} />
            <StatCard label="Artículos bajo mínimo" value={kpi?.itemsBelowMin ?? 0}                  loading={loadingKpi} />
            <StatCard label="Almacenes activos"   value={kpi?.warehouseCount ?? 0}                    loading={loadingKpi} />
          </div>

          <div className="db-card" style={{ padding: 20 }}>
            <h3 style={{ fontSize: 13.5, fontWeight: 600, margin: '0 0 16px', color: 'var(--c-text)' }}>
              Top 10 artículos por valor de stock
            </h3>
            {loadingSnapshot ? (
              <div className="cp-skeleton" style={{ height: 220, borderRadius: 6 }} />
            ) : topStockChartData.length === 0 ? (
              <NbEmptyState message="Sin datos de stock disponibles." icon="chart" />
            ) : (
              <NbBarChart data={topStockChartData} height={220} />
            )}
          </div>
        </div>
      )}

      {/* ── Tab: Stock ──────────────────────────────────────────────────────── */}
      {tab === 'stock' && (
        <div className="db-card" style={{ overflow: 'hidden' }}>
          <div style={{ display: 'flex', justifyContent: 'flex-end', padding: '8px 12px' }}>
            <button
              onClick={() => exportXlsx('inventario-stock', 'Stock', sortedStock.map(r => ({
                'Código': r.itemCode,
                'Artículo': r.itemName,
                'Grupo': r.itemGroupName,
                'En Stock': r.onHand,
                'Comprometido': r.committed,
                'Disponible': r.available,
                'Precio Prom.': r.avgPrice,
                'Valor Stock': r.stockValue,
              })))}
              disabled={sortedStock.length === 0}
              style={{ display: 'inline-flex', alignItems: 'center', gap: 6, padding: '6px 14px', borderRadius: 6, border: '1px solid var(--c-border)', background: '#fff', color: 'var(--c-text)', fontSize: 13, fontWeight: 500, cursor: 'pointer', fontFamily: 'inherit' }}
            >
              <Download size={14} />
              Exportar Excel
            </button>
          </div>
          {loadingSnapshot ? (
            <div className="cp-skeleton" style={{ height: 300, margin: 16, borderRadius: 6 }} />
          ) : (snapshot ?? []).length === 0 ? (
            <NbEmptyState message="Sin datos de stock disponibles." icon="table" />
          ) : (
            <SortableTable<InventorySnapshotItem>
              data={sortedStock}
              columns={stockCols}
              meta={fakeMeta(sortedStock.length)}
              sortBy={stockSort.key}
              sortDir={stockSort.dir}
              onPageChange={() => {}}
              onSortChange={(key, dir) => setStockSort({ key, dir })}
              rowKey={r => r.itemCode}
            />
          )}
        </div>
      )}

      {/* ── Tab: Movimientos ────────────────────────────────────────────────── */}
      {tab === 'movimientos' && (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 20 }}>
          <DateRangeSelector range={range} onFromChange={setFrom} onToChange={setTo} />

          <div className="db-card" style={{ padding: 20 }}>
            <h3 style={{ fontSize: 13.5, fontWeight: 600, margin: '0 0 16px', color: 'var(--c-text)' }}>
              Entradas vs. Salidas (valor)
            </h3>
            {loadingMovement ? (
              <div className="cp-skeleton" style={{ height: 220, borderRadius: 6 }} />
            ) : movementInbound.length === 0 ? (
              <NbEmptyState message="Sin movimientos en el período seleccionado." icon="chart" />
            ) : (
              <NbAreaChart
                series={[
                  { name: 'Entradas', data: movementInbound },
                  { name: 'Salidas',  data: movementOutbound },
                ]}
                height={220}
              />
            )}
          </div>

          <div className="db-card" style={{ overflow: 'hidden' }}>
            {loadingMovement ? (
              <div className="cp-skeleton" style={{ height: 200, margin: 16, borderRadius: 6 }} />
            ) : filteredMovement.length === 0 ? (
              <NbEmptyState message="Sin datos de movimiento." icon="table" />
            ) : (
              <SortableTable<InventoryMovementKpi>
                data={filteredMovement}
                columns={movKpiCols}
                meta={fakeMeta(filteredMovement.length)}
                onPageChange={() => {}}
                onSortChange={() => {}}
                rowKey={r => `${r.year}-${r.month}`}
              />
            )}
          </div>
        </div>
      )}

      {/* ── Tab: Almacenes ──────────────────────────────────────────────────── */}
      {tab === 'almacenes' && (
        <div className="db-card" style={{ overflow: 'hidden' }}>
          {loadingWarehouses ? (
            <div className="cp-skeleton" style={{ height: 200, margin: 16, borderRadius: 6 }} />
          ) : (warehouses ?? []).length === 0 ? (
            <NbEmptyState message="Sin datos de almacenes disponibles." icon="table" />
          ) : (
            <SortableTable<WarehouseStock>
              data={sortedWarehouses}
              columns={whCols}
              meta={fakeMeta(sortedWarehouses.length)}
              sortBy={whSort.key}
              sortDir={whSort.dir}
              onPageChange={() => {}}
              onSortChange={(key, dir) => setWhSort({ key, dir })}
              rowKey={r => r.warehouseCode}
            />
          )}
        </div>
      )}

      {/* ── Tab: Slow-Moving ────────────────────────────────────────────────── */}
      {tab === 'slow-moving' && (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 20 }}>
          <div style={{ display: 'flex', gap: 8, alignItems: 'center', flexWrap: 'wrap' }}>
            <span style={{ fontSize: 13, color: 'var(--c-text-muted)' }}>Sin movimiento hace:</span>
            {([30, 60, 90, 180] as const).map(d => (
              <button
                key={d}
                onClick={() => setMinDays(d)}
                className={`db-btn db-btn--sm ${minDays === d ? 'db-btn--primary' : 'db-btn--ghost'}`}
              >
                {d}+ días
              </button>
            ))}
            <div style={{ marginLeft: 'auto' }}>
              <button
                onClick={() => exportXlsx('inventario-slow-moving', 'Slow Moving', sortedSlowItems.map(r => ({
                  'Código': r.itemCode,
                  'Artículo': r.itemName,
                  'Grupo': r.itemGroupName,
                  'En Stock': r.onHand,
                  'Valor Stock': r.stockValue,
                  'Último Movimiento': r.lastMovementDate,
                  'Días sin Movimiento': r.daysWithoutMovement,
                })))}
                disabled={sortedSlowItems.length === 0}
                style={{ display: 'inline-flex', alignItems: 'center', gap: 6, padding: '6px 14px', borderRadius: 6, border: '1px solid var(--c-border)', background: '#fff', color: 'var(--c-text)', fontSize: 13, fontWeight: 500, cursor: 'pointer', fontFamily: 'inherit' }}
              >
                <Download size={14} />
                Exportar Excel
              </button>
            </div>
          </div>

          <div className="db-card" style={{ overflow: 'hidden' }}>
            {loadingSlowItems ? (
              <div className="cp-skeleton" style={{ height: 250, margin: 16, borderRadius: 6 }} />
            ) : sortedSlowItems.length === 0 ? (
              <NbEmptyState message={`Sin artículos sin movimiento en los últimos ${minDays} días.`} icon="search" />
            ) : (
              <SortableTable<SlowMovingItem>
                data={sortedSlowItems}
                columns={slowCols}
                meta={fakeMeta(sortedSlowItems.length)}
                sortBy={slowSort.key}
                sortDir={slowSort.dir}
                onPageChange={() => {}}
                onSortChange={(key, dir) => setSlowSort({ key, dir })}
                rowKey={r => r.itemCode}
              />
            )}
          </div>
        </div>
      )}
    </div>
  )
}
