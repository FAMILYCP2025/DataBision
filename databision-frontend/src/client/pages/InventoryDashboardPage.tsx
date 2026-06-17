import { useState } from 'react'
import NativeBiPageHeader from '../components/nativebi/NativeBiPageHeader'
import KpiCard from '../components/nativebi/KpiCard'
import SortableTable, { type ColumnDef } from '../components/nativebi/SortableTable'
import { NbErrorState, NbEmptyState } from '../components/nativebi/NativeBiState'
import NativeBiMiniBarList from '../components/nativebi/NativeBiMiniBarList'
import NativeBiFilterBar from '../components/nativebi/NativeBiFilterBar'
import {
  useBiInventoryRotation,
  useBiInventoryWarehouses,
} from '../hooks/useProcessBi'
import { useNativeBiFilters } from '../hooks/useNativeBiFilters'
import { useWarehouseOptions, useItemGroupOptions } from '../hooks/useFilterOptions'
import type { InventoryRotation, InventoryWarehouse } from '../types/processBi'
import type { NbPagedMeta, PaginationParams } from '../types/nativeBi'
import type { NativeBiFilterDefinition } from '../types/nativeBiFilters'

function fmtQty(n: number | null) {
  if (n === null || n === undefined) return '—'
  return n.toLocaleString('es-CL', { maximumFractionDigits: 2 })
}

function fmtDate(iso: string | null) {
  if (!iso) return '—'
  return new Date(iso + 'T00:00:00').toLocaleDateString('es-CL', {
    day: '2-digit', month: 'short', year: 'numeric',
  })
}

function daysSince(iso: string | null): number | null {
  if (!iso) return null
  const diff = Date.now() - new Date(iso + 'T00:00:00').getTime()
  return Math.floor(diff / (1000 * 60 * 60 * 24))
}

const ROTATION_LABEL: Record<string, string> = {
  FAST:        'Alta',
  NORMAL:      'Normal',
  SLOW:        'Baja',
  NO_MOVEMENT: 'Sin movimiento',
}

const ROTATION_COLOR: Record<string, string> = {
  FAST:        '#16A34A',
  NORMAL:      '#2563EB',
  SLOW:        '#D97706',
  NO_MOVEMENT: '#94A3B8',
}

type Tab = 'resumen' | 'rotation' | 'warehouses' | 'no-movement'

const LIMIT = 20
const EMPTY_META: NbPagedMeta = { limit: LIMIT, offset: 0, count: 0, hasMore: false }

function initPag(sortBy: string): PaginationParams {
  return { limit: LIMIT, offset: 0, sortBy, sortDir: 'desc' }
}

const tabs: { id: Tab; label: string }[] = [
  { id: 'resumen',     label: 'Resumen' },
  { id: 'rotation',    label: 'Rotación' },
  { id: 'warehouses',  label: 'Almacenes' },
  { id: 'no-movement', label: 'Sin movimiento' },
]

const INVENTORY_FILTER_DEFS: NativeBiFilterDefinition[] = [
  { key: 'warehouseCodes', label: 'Almacén',         type: 'select', source: 'endpoint', modules: ['inventory'], placeholder: 'Todos' },
  { key: 'itemGroupCodes', label: 'Grupo artículo',  type: 'select', source: 'endpoint', modules: ['inventory'], isAdvanced: true, placeholder: 'Todos' },
]

function TabButton({ label, active, onClick }: { id?: string; label: string; active: boolean; onClick: () => void }) {
  return (
    <button
      role="tab"
      aria-selected={active}
      onClick={onClick}
      style={{
        padding: '0 16px', height: 44, background: 'none', border: 'none',
        borderBottom: active ? '2px solid var(--brand-primary, #2563EB)' : '2px solid transparent',
        color: active ? 'var(--brand-primary, #2563EB)' : 'var(--c-text-muted)',
        fontWeight: active ? 600 : 500, fontSize: 13.5, cursor: 'pointer',
        fontFamily: 'inherit', marginBottom: -1,
        transition: 'color 150ms, border-color 150ms', whiteSpace: 'nowrap',
      }}
    >
      {label}
    </button>
  )
}

function RotationBadge({ status }: { status: string }) {
  return (
    <span style={{
      display: 'inline-block',
      padding: '2px 8px',
      borderRadius: 4,
      fontSize: 12,
      fontWeight: 600,
      color: '#fff',
      backgroundColor: ROTATION_COLOR[status] ?? '#94A3B8',
    }}>
      {ROTATION_LABEL[status] ?? status}
    </span>
  )
}

export default function InventoryDashboardPage() {
  const { filters, setFilter, resetFilter, resetAll, hasActiveFilters } = useNativeBiFilters('inventory')
  const [tab, setTab] = useState<Tab>('resumen')
  const [rotP, setRotP] = useState<PaginationParams>(initPag('qtySold90d'))

  const { data: whOpts, isLoading: whOptsLoading } = useWarehouseOptions()
  const { data: igOpts, isLoading: igOptsLoading } = useItemGroupOptions()
  const optionsByKey = { warehouseCodes: whOpts ?? [], itemGroupCodes: igOpts ?? [] }
  const loadingKeys  = new Set<string>([...(whOptsLoading ? ['warehouseCodes'] : []), ...(igOptsLoading ? ['itemGroupCodes'] : [])])

  const { data: rotData, isLoading: loadingRot, error: rotErr, refetch: refetchRot } = useBiInventoryRotation(rotP)
  const { data: whData, isLoading: loadingWh } = useBiInventoryWarehouses()

  const allRotation = rotData?.data ?? []
  const fastItems   = allRotation.filter((r) => r.rotationStatus === 'FAST')
  const normalItems = allRotation.filter((r) => r.rotationStatus === 'NORMAL')
  const slowItems   = allRotation.filter((r) => r.rotationStatus === 'SLOW')
  const noMoveItems = allRotation.filter((r) => r.rotationStatus === 'NO_MOVEMENT')

  const fastCount   = fastItems.length
  const normalCount = normalItems.length
  const slowCount   = slowItems.length
  const noMoveCount = noMoveItems.length
  const totalCount  = allRotation.length

  const avgCoverage = allRotation.length > 0
    ? allRotation.reduce((s, r) => s + (r.coverageDays ?? 0), 0) / allRotation.length
    : 0
  const pctNoMove   = totalCount > 0 ? (noMoveCount / totalCount) * 100 : 0
  const topRotItem  = fastItems[0] ?? allRotation[0]
  const maxQty90    = allRotation.length > 0 ? Math.max(...allRotation.map((r) => r.qtySold90d)) : 1

  const rotCols: ColumnDef<InventoryRotation>[] = [
    {
      key: 'rank',
      label: '#',
      render: (_r, i) => (
        <span style={{ fontSize: 12, color: 'var(--c-text-faint)', fontWeight: 700 }}>
          {(rotP.offset ?? 0) + (i ?? 0) + 1}
        </span>
      ),
    },
    {
      key: 'item',
      label: 'Artículo',
      render: (r) => (
        <div>
          <div style={{ fontWeight: 500 }}>{r.itemName ?? r.itemCode}</div>
          <div style={{ fontSize: 11.5, color: 'var(--c-text-faint)' }}>{r.itemCode}</div>
        </div>
      ),
    },
    {
      key: 'status',
      label: 'Rotación',
      render: (r) => <RotationBadge status={r.rotationStatus} />,
    },
    {
      key: 'qty30',
      label: 'Cant. 30d',
      sortKey: 'qtySold30d',
      align: 'right',
      render: (r) => <span style={{ fontVariantNumeric: 'tabular-nums' }}>{fmtQty(r.qtySold30d)}</span>,
    },
    {
      key: 'qty90',
      label: 'Cant. 90d',
      sortKey: 'qtySold90d',
      align: 'right',
      render: (r) => <span style={{ fontVariantNumeric: 'tabular-nums' }}>{fmtQty(r.qtySold90d)}</span>,
    },
    {
      key: 'onHand',
      label: 'Stock actual',
      align: 'right',
      render: (r) => (
        <span style={{ fontVariantNumeric: 'tabular-nums', color: (r.onHandQty ?? 0) === 0 ? '#DC2626' : 'inherit' }}>
          {fmtQty(r.onHandQty)}
        </span>
      ),
    },
    {
      key: 'coverage',
      label: 'Cobertura',
      align: 'right',
      render: (r) => {
        const days = r.coverageDays
        const color = days === null ? 'var(--c-text-faint)' : days < 15 ? '#DC2626' : days < 45 ? '#D97706' : '#16A34A'
        return (
          <span style={{ fontVariantNumeric: 'tabular-nums', color }}>
            {days !== null ? `${fmtQty(days)}d` : '—'}
          </span>
        )
      },
    },
    {
      key: 'lastSale',
      label: 'Última venta',
      align: 'right',
      render: (r) => fmtDate(r.lastSaleDate),
    },
  ]

  const noMoveCols: ColumnDef<InventoryRotation>[] = [
    {
      key: 'item',
      label: 'Artículo',
      render: (r) => (
        <div>
          <div style={{ fontWeight: 500 }}>{r.itemName ?? r.itemCode}</div>
          <div style={{ fontSize: 11.5, color: 'var(--c-text-faint)' }}>{r.itemCode}</div>
        </div>
      ),
    },
    {
      key: 'onHand',
      label: 'Stock actual',
      align: 'right',
      render: (r) => (
        <span style={{ fontVariantNumeric: 'tabular-nums', color: (r.onHandQty ?? 0) > 0 ? '#D97706' : 'inherit' }}>
          {fmtQty(r.onHandQty)}
        </span>
      ),
    },
    {
      key: 'lastSale',
      label: 'Última venta',
      align: 'right',
      render: (r) => fmtDate(r.lastSaleDate),
    },
    {
      key: 'daysSince',
      label: 'Días sin venta',
      align: 'right',
      render: (r) => {
        const d = daysSince(r.lastSaleDate)
        return (
          <span style={{ fontVariantNumeric: 'tabular-nums', color: (d ?? 0) > 90 ? '#DC2626' : '#D97706', fontWeight: 600 }}>
            {d !== null ? `${d}d` : 'Sin registro'}
          </span>
        )
      },
    },
    {
      key: 'qty90',
      label: 'Vendido 90d',
      align: 'right',
      render: (r) => <span style={{ fontVariantNumeric: 'tabular-nums' }}>{fmtQty(r.qtySold90d)}</span>,
    },
  ]

  const whCols: ColumnDef<InventoryWarehouse>[] = [
    {
      key: 'name',
      label: 'Almacén',
      render: (r) => (
        <div>
          <div style={{ fontWeight: 500 }}>{r.warehouseName ?? r.warehouseCode}</div>
          <div style={{ fontSize: 11.5, color: 'var(--c-text-faint)' }}>{r.warehouseCode}</div>
        </div>
      ),
    },
    {
      key: 'inCount',
      label: 'Entradas',
      align: 'right',
      render: (r) => <span style={{ fontVariantNumeric: 'tabular-nums' }}>{r.transferInCount}</span>,
    },
    {
      key: 'inQty',
      label: 'Cant. entrada',
      align: 'right',
      render: (r) => <span style={{ fontVariantNumeric: 'tabular-nums' }}>{fmtQty(r.transferInQty)}</span>,
    },
    {
      key: 'outCount',
      label: 'Salidas',
      align: 'right',
      render: (r) => <span style={{ fontVariantNumeric: 'tabular-nums' }}>{r.transferOutCount}</span>,
    },
    {
      key: 'outQty',
      label: 'Cant. salida',
      align: 'right',
      render: (r) => <span style={{ fontVariantNumeric: 'tabular-nums' }}>{fmtQty(r.transferOutQty)}</span>,
    },
    {
      key: 'lastTransfer',
      label: 'Último traslado',
      align: 'right',
      render: (r) => fmtDate(r.lastTransferDate),
    },
  ]

  const rotLoading = loadingRot && !rotData

  return (
    <div className="cp-page">
      <NativeBiPageHeader
        title="Inventario"
        description="Rotación de artículos y movimientos por almacén"
      />

      <NativeBiFilterBar
        filters={filters}
        definitions={INVENTORY_FILTER_DEFS}
        optionsByKey={optionsByKey}
        loadingKeys={loadingKeys}
        onFilterChange={setFilter}
        onFilterReset={resetFilter}
        onResetAll={resetAll}
        hasActiveFilters={hasActiveFilters}
      />

      {rotErr ? (
        <NbErrorState message="Error al cargar datos de inventario." onRetry={() => refetchRot()} />
      ) : (
        <div className="nb-card-grid">
          <KpiCard label="Alta rotación"   value={fastCount}   loading={rotLoading} />
          <KpiCard label="Rotación normal" value={normalCount} loading={rotLoading} />
          <KpiCard label="Baja rotación"   value={slowCount}   loading={rotLoading} />
          <KpiCard label="Sin movimiento"  value={noMoveCount} loading={rotLoading}
            subLabel={totalCount > 0 ? `${pctNoMove.toFixed(1)}% de la muestra` : undefined}
          />
        </div>
      )}

      <div className="db-card">
        <div
          className="db-card-header nb-tab-bar"
          style={{ paddingLeft: 4, paddingRight: 16, gap: 0, borderBottom: '1px solid var(--c-border)', overflowX: 'auto' }}
          role="tablist"
          aria-label="Secciones de inventario"
        >
          {tabs.map((t) => (
            <TabButton key={t.id} id={t.id} label={t.label} active={tab === t.id} onClick={() => setTab(t.id)} />
          ))}
        </div>

        {/* ── Resumen ─────────────────────────────────────────────────────── */}
        {tab === 'resumen' && (
          rotLoading ? (
            <div style={{ padding: 24 }}>
              {Array.from({ length: 4 }).map((_, i) => (
                <div key={i} className="cp-skeleton" style={{ height: 44, borderRadius: 6, marginBottom: 8 }} />
              ))}
            </div>
          ) : allRotation.length === 0 ? (
            <NbEmptyState message="Sin datos de inventario disponibles. Disponible al completar carga histórica." icon="table" />
          ) : (
            <div style={{ padding: '16px 20px' }}>

              {/* Secondary KPIs */}
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(160px, 1fr))', gap: 12, marginBottom: 24 }}>
                {[
                  { label: 'SKUs analizados',   value: totalCount },
                  { label: 'Cobertura promedio', value: `${fmtQty(avgCoverage)}d` },
                  { label: '% Sin movimiento',   value: `${pctNoMove.toFixed(1)}%` },
                  { label: 'Artículo líder',     value: topRotItem ? (topRotItem.itemName ?? topRotItem.itemCode) : '—', small: true },
                ].map((kpi) => (
                  <div key={kpi.label} className="db-stat-card">
                    <span className="db-stat-label">{kpi.label}</span>
                    <span className="db-stat-value" style={{ fontSize: kpi.small ? 14 : 20, fontVariantNumeric: 'tabular-nums', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                      {kpi.value}
                    </span>
                  </div>
                ))}
              </div>

              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 24, marginBottom: 0 }}>
                {/* Distribution visualization */}
                <div>
                  <div style={{ fontSize: 12.5, fontWeight: 600, color: 'var(--c-text-muted)', marginBottom: 14, textTransform: 'uppercase', letterSpacing: '0.04em' }}>
                    Distribución por rotación
                  </div>
                  <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
                    {([
                      { status: 'FAST',        label: 'Alta rotación',   count: fastCount,   color: '#16A34A' },
                      { status: 'NORMAL',      label: 'Rotación normal', count: normalCount, color: '#2563EB' },
                      { status: 'SLOW',        label: 'Baja rotación',   count: slowCount,   color: '#D97706' },
                      { status: 'NO_MOVEMENT', label: 'Sin movimiento',  count: noMoveCount, color: '#94A3B8' },
                    ] as const).map((row) => {
                      const p = totalCount > 0 ? (row.count / totalCount) * 100 : 0
                      return (
                        <div key={row.status}>
                          <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 4, fontSize: 13 }}>
                            <span style={{ color: 'var(--c-text)', fontWeight: 500 }}>{row.label}</span>
                            <span style={{ fontVariantNumeric: 'tabular-nums', color: 'var(--c-text-muted)' }}>
                              {row.count} · {p.toFixed(1)}%
                            </span>
                          </div>
                          <div style={{ height: 6, backgroundColor: 'var(--c-border)', borderRadius: 3 }}>
                            <div style={{
                              width: `${p}%`, height: '100%',
                              backgroundColor: row.color, borderRadius: 3,
                              transition: 'width 400ms ease',
                            }} />
                          </div>
                        </div>
                      )
                    })}
                  </div>
                </div>

                {/* Top artículos por 90d */}
                <div>
                  {allRotation.length > 0 && (
                    <NativeBiMiniBarList
                      title="Top artículos — cantidad vendida 90d"
                      items={[...allRotation].sort((a, b) => b.qtySold90d - a.qtySold90d).slice(0, 6).map((r) => ({
                        label: r.itemName ?? r.itemCode,
                        sublabel: r.itemCode,
                        value: r.qtySold90d,
                        pct: maxQty90 > 0 ? (r.qtySold90d / maxQty90) * 100 : 0,
                        color: ROTATION_COLOR[r.rotationStatus] ?? '#94A3B8',
                        badgeText: ROTATION_LABEL[r.rotationStatus],
                        badgeColor: ROTATION_COLOR[r.rotationStatus],
                      }))}
                      formatValue={(n) => n.toLocaleString('es-CL', { maximumFractionDigits: 0 })}
                      maxItems={6}
                    />
                  )}
                </div>
              </div>

              <p style={{ fontSize: 12, color: 'var(--c-text-faint)', marginTop: 20 }}>
                Datos de la página actual ({totalCount} artículos). Navega a "Rotación" para filtrar y ordenar.
                {noMoveCount > 0 && ` ${noMoveCount} artículo(s) sin movimiento identificados — revisar oportunidades de liquidación.`}
              </p>
            </div>
          )
        )}

        {/* ── Rotación ────────────────────────────────────────────────────── */}
        {tab === 'rotation' && (
          allRotation.length === 0 && !loadingRot ? (
            <NbEmptyState message="Sin datos de rotación de artículos en el período analizado." icon="table" />
          ) : (
            <SortableTable
              data={allRotation}
              columns={rotCols}
              meta={rotData?.meta ?? EMPTY_META}
              sortBy={rotP.sortBy}
              sortDir={rotP.sortDir}
              onPageChange={(offset) => setRotP((p) => ({ ...p, offset }))}
              onSortChange={(sortBy, sortDir) => setRotP((p) => ({ ...p, sortBy, sortDir, offset: 0 }))}
              isLoading={loadingRot}
              rowKey={(r) => r.itemCode}
            />
          )
        )}

        {/* ── Almacenes ───────────────────────────────────────────────────── */}
        {tab === 'warehouses' && (
          loadingWh ? (
            <div style={{ padding: 24 }}>
              {Array.from({ length: 5 }).map((_, i) => (
                <div key={i} className="cp-skeleton" style={{ height: 44, borderRadius: 6, marginBottom: 8 }} />
              ))}
            </div>
          ) : !whData || whData.length === 0 ? (
            <div style={{ padding: '24px 24px' }}>
              <div style={{ maxWidth: 440, margin: '0 auto', textAlign: 'center' }}>
                <div style={{ fontSize: 28, marginBottom: 12 }}>🏭</div>
                <div style={{ fontSize: 15, fontWeight: 600, color: 'var(--c-text)', marginBottom: 8 }}>
                  Movimientos por almacén
                </div>
                <p style={{ fontSize: 13.5, color: 'var(--c-text-muted)', lineHeight: 1.6, marginBottom: 16 }}>
                  Disponible al completar la carga de traspasos entre almacenes (OWTR).
                  Esta sección mostrará entradas, salidas y último movimiento por bodega.
                </p>
                <div style={{ padding: '10px 16px', backgroundColor: '#F0F9FF', border: '1px solid #BAE6FD', borderRadius: 6, fontSize: 12.5, color: '#0369A1', textAlign: 'left' }}>
                  <strong>Próximamente:</strong> stock valorizado por almacén, artículos en stockout, cobertura por bodega.
                </div>
              </div>
            </div>
          ) : (
            <SortableTable
              data={whData}
              columns={whCols}
              meta={{ limit: whData.length, offset: 0, count: whData.length, hasMore: false }}
              isLoading={false}
              rowKey={(r) => r.warehouseCode}
              onPageChange={() => {}}
              onSortChange={() => {}}
            />
          )
        )}

        {/* ── Sin movimiento ──────────────────────────────────────────────── */}
        {tab === 'no-movement' && (
          noMoveItems.length === 0 && !loadingRot ? (
            <NbEmptyState message="Sin artículos sin movimiento en el período analizado. ¡Buen inventario activo!" icon="table" />
          ) : (
            <>
              {noMoveItems.length > 0 && (
                <div style={{ padding: '12px 20px', backgroundColor: '#FFFBEB', borderBottom: '1px solid #FDE68A', display: 'flex', gap: 8, alignItems: 'center' }}>
                  <span style={{ fontSize: 16 }}>⚠️</span>
                  <span style={{ fontSize: 13, color: '#92400E' }}>
                    <strong>{noMoveItems.length} artículo(s)</strong> sin ventas registradas.
                    Revisar oportunidades de liquidación, redistribución o descontinuación.
                  </span>
                </div>
              )}
              <SortableTable
                data={noMoveItems}
                columns={noMoveCols}
                meta={{ limit: noMoveItems.length, offset: 0, count: noMoveItems.length, hasMore: false }}
                isLoading={rotLoading}
                rowKey={(r) => r.itemCode}
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
