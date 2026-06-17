import { useState } from 'react'
import NativeBiPageHeader from '../components/nativebi/NativeBiPageHeader'
import KpiCard from '../components/nativebi/KpiCard'
import SortableTable, { type ColumnDef } from '../components/nativebi/SortableTable'
import { NbErrorState, NbEmptyState } from '../components/nativebi/NativeBiState'
import NativeBiMiniBarList from '../components/nativebi/NativeBiMiniBarList'
import NativeBiFilterBar from '../components/nativebi/NativeBiFilterBar'
import {
  useBiPurchasingExecutive,
  useBiPurchasingSuppliers,
  useBiPurchasingReceiving,
} from '../hooks/useProcessBi'
import { useNativeBiFilters } from '../hooks/useNativeBiFilters'
import { useSupplierGroupOptions, useWarehouseOptions } from '../hooks/useFilterOptions'
import type { PurchasingSupplier, PurchasingReceiving, PurchasingExecutive } from '../types/processBi'
import type { NbPagedMeta, PaginationParams } from '../types/nativeBi'
import type { NativeBiFilterDefinition } from '../types/nativeBiFilters'

function fmtAmt(n: number) {
  return n.toLocaleString('es-CL', { maximumFractionDigits: 0 })
}

function fmtPct(n: number) {
  return `${n.toLocaleString('es-CL', { maximumFractionDigits: 1 })}%`
}

function fmtDate(iso: string | null) {
  if (!iso) return '—'
  return new Date(iso + 'T00:00:00').toLocaleDateString('es-CL', {
    day: '2-digit', month: 'short', year: 'numeric',
  })
}

function pct(part: number, total: number) {
  if (!total) return 0
  return (part / total) * 100
}

type Tab = 'resumen' | 'suppliers' | 'receiving' | 'evolution'

const LIMIT = 20
const EMPTY_META: NbPagedMeta = { limit: LIMIT, offset: 0, count: 0, hasMore: false }

function initPag(sortBy: string): PaginationParams {
  return { limit: LIMIT, offset: 0, sortBy, sortDir: 'desc' }
}

const tabs: { id: Tab; label: string }[] = [
  { id: 'resumen',   label: 'Resumen' },
  { id: 'suppliers', label: 'Proveedores' },
  { id: 'receiving', label: 'Recepciones' },
  { id: 'evolution', label: 'Evolución OC' },
]

const PURCHASING_FILTER_DEFS: NativeBiFilterDefinition[] = [
  { key: 'dateFrom',          label: 'Período',         type: 'date-range', source: 'static',   modules: ['purchasing'] },
  { key: 'supplierGroupCodes', label: 'Grupo proveedor', type: 'select',     source: 'endpoint', modules: ['purchasing'], isAdvanced: true, placeholder: 'Todos' },
  { key: 'warehouseCodes',    label: 'Almacén',          type: 'select',     source: 'endpoint', modules: ['purchasing'], isAdvanced: true, placeholder: 'Todos' },
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

export default function PurchasingDashboardPage() {
  const { filters, setFilter, resetFilter, resetAll, hasActiveFilters } = useNativeBiFilters('purchasing')
  const [tab, setTab] = useState<Tab>('resumen')
  const [suppP, setSuppP] = useState<PaginationParams>(initPag('poAmount'))
  const [recvP, setRecvP] = useState<PaginationParams>(initPag('grAmount'))

  const { data: sgOpts, isLoading: sgLoading } = useSupplierGroupOptions()
  const { data: whOpts, isLoading: whLoading }  = useWarehouseOptions()
  const optionsByKey = { supplierGroupCodes: sgOpts ?? [], warehouseCodes: whOpts ?? [] }
  const loadingKeys  = new Set<string>([...(sgLoading ? ['supplierGroupCodes'] : []), ...(whLoading ? ['warehouseCodes'] : [])])

  const { data: execData, isLoading: loadingExec, error: execErr, refetch: refetchExec } = useBiPurchasingExecutive(30)
  const { data: suppData, isLoading: loadingSupp } = useBiPurchasingSuppliers(suppP)
  const { data: recvData, isLoading: loadingRecv } = useBiPurchasingReceiving(recvP)

  // ── KPI aggregates ──────────────────────────────────────────────────────
  const totalPo       = execData?.reduce((s, d) => s + d.poCount, 0) ?? 0
  const totalPoAmt    = execData?.reduce((s, d) => s + d.poAmount, 0) ?? 0
  const totalRecvAmt  = execData?.reduce((s, d) => s + d.receivedAmount, 0) ?? 0
  const totalRecvCnt  = execData?.reduce((s, d) => s + d.receivedCount, 0) ?? 0
  const maxSuppliers  = execData?.reduce((m, d) => Math.max(m, d.activeSuppliers), 0) ?? 0
  const pctReceived   = totalPoAmt > 0 ? pct(totalRecvAmt, totalPoAmt) : 0
  const gap           = totalPoAmt - totalRecvAmt
  const avgPoAmt      = totalPo > 0 ? totalPoAmt / totalPo : 0

  const suppliers     = suppData?.data ?? []
  const totalSuppAmt  = suppliers.reduce((s, p) => s + p.poAmount, 0)
  const topSupplier   = suppliers[0]

  const execCols: ColumnDef<PurchasingExecutive>[] = [
    {
      key: 'date',
      label: 'Fecha',
      render: (r) => fmtDate(r.purchaseDate),
    },
    {
      key: 'poCount',
      label: 'OC',
      align: 'right',
      render: (r) => <span style={{ fontVariantNumeric: 'tabular-nums' }}>{r.poCount}</span>,
    },
    {
      key: 'poAmount',
      label: 'Monto OC',
      align: 'right',
      render: (r) => <span style={{ fontVariantNumeric: 'tabular-nums' }}>{fmtAmt(r.poAmount)}</span>,
    },
    {
      key: 'receivedAmount',
      label: 'Recibido',
      align: 'right',
      render: (r) => <span style={{ fontVariantNumeric: 'tabular-nums' }}>{fmtAmt(r.receivedAmount)}</span>,
    },
    {
      key: 'pctRecv',
      label: '% Recibido',
      align: 'right',
      render: (r) => {
        const p = r.poAmount > 0 ? pct(r.receivedAmount, r.poAmount) : 0
        return (
          <span style={{ fontVariantNumeric: 'tabular-nums', color: p < 50 ? '#D97706' : '#16A34A' }}>
            {fmtPct(p)}
          </span>
        )
      },
    },
    {
      key: 'gap',
      label: 'Brecha',
      align: 'right',
      render: (r) => {
        const g = r.poAmount - r.receivedAmount
        return (
          <span style={{ fontVariantNumeric: 'tabular-nums', color: g > 0 ? '#D97706' : 'var(--c-text-muted)' }}>
            {fmtAmt(g)}
          </span>
        )
      },
    },
    {
      key: 'activeSuppliers',
      label: 'Prov. activos',
      align: 'right',
      render: (r) => <span style={{ fontVariantNumeric: 'tabular-nums' }}>{r.activeSuppliers}</span>,
    },
  ]

  const suppCols: ColumnDef<PurchasingSupplier>[] = [
    {
      key: 'rank',
      label: '#',
      render: (_r, i) => (
        <span style={{ fontSize: 12, color: 'var(--c-text-faint)', fontWeight: 700, fontVariantNumeric: 'tabular-nums' }}>
          {(suppP.offset ?? 0) + (i ?? 0) + 1}
        </span>
      ),
    },
    {
      key: 'name',
      label: 'Proveedor',
      render: (r) => (
        <div>
          <div style={{ fontWeight: 500 }}>{r.supplierName ?? r.supplierCode}</div>
          <div style={{ fontSize: 11.5, color: 'var(--c-text-faint)' }}>{r.supplierCode}</div>
        </div>
      ),
    },
    {
      key: 'poCount',
      label: 'OC',
      sortKey: 'poCount',
      align: 'right',
      render: (r) => <span style={{ fontVariantNumeric: 'tabular-nums' }}>{r.poCount}</span>,
    },
    {
      key: 'poAmount',
      label: 'Monto OC',
      sortKey: 'poAmount',
      align: 'right',
      render: (r) => <span style={{ fontVariantNumeric: 'tabular-nums' }}>{fmtAmt(r.poAmount)}</span>,
    },
    {
      key: 'pct',
      label: '% Part.',
      align: 'right',
      render: (r) => (
        <span style={{ fontVariantNumeric: 'tabular-nums', color: 'var(--c-text-muted)' }}>
          {pct(r.poAmount, totalSuppAmt).toFixed(1)}%
        </span>
      ),
    },
    {
      key: 'receivedAmount',
      label: 'Recibido',
      sortKey: 'receivedAmount',
      align: 'right',
      render: (r) => <span style={{ fontVariantNumeric: 'tabular-nums' }}>{fmtAmt(r.receivedAmount)}</span>,
    },
    {
      key: 'pctRecv',
      label: '% Recibido',
      align: 'right',
      render: (r) => {
        const p = r.poAmount > 0 ? pct(r.receivedAmount, r.poAmount) : 0
        return (
          <span style={{ fontVariantNumeric: 'tabular-nums', color: p < 50 ? '#D97706' : '#16A34A' }}>
            {fmtPct(p)}
          </span>
        )
      },
    },
    {
      key: 'avgPoAmount',
      label: 'Prom. OC',
      align: 'right',
      render: (r) => <span style={{ fontVariantNumeric: 'tabular-nums' }}>{fmtAmt(r.avgPoAmount)}</span>,
    },
    {
      key: 'lastPoDate',
      label: 'Última OC',
      align: 'right',
      render: (r) => fmtDate(r.lastPoDate),
    },
  ]

  const recvCols: ColumnDef<PurchasingReceiving>[] = [
    {
      key: 'rank',
      label: '#',
      render: (_r, i) => (
        <span style={{ fontSize: 12, color: 'var(--c-text-faint)', fontWeight: 700, fontVariantNumeric: 'tabular-nums' }}>
          {(recvP.offset ?? 0) + (i ?? 0) + 1}
        </span>
      ),
    },
    {
      key: 'name',
      label: 'Proveedor',
      render: (r) => (
        <div>
          <div style={{ fontWeight: 500 }}>{r.supplierName ?? r.supplierCode}</div>
          <div style={{ fontSize: 11.5, color: 'var(--c-text-faint)' }}>{r.supplierCode}</div>
        </div>
      ),
    },
    {
      key: 'grCount',
      label: 'Recepciones',
      sortKey: 'grCount',
      align: 'right',
      render: (r) => <span style={{ fontVariantNumeric: 'tabular-nums' }}>{r.grCount}</span>,
    },
    {
      key: 'grAmount',
      label: 'Monto recibido',
      sortKey: 'grAmount',
      align: 'right',
      render: (r) => <span style={{ fontVariantNumeric: 'tabular-nums' }}>{fmtAmt(r.grAmount)}</span>,
    },
    {
      key: 'lastGrDate',
      label: 'Última recepción',
      align: 'right',
      render: (r) => fmtDate(r.lastGrDate),
    },
  ]

  return (
    <div className="cp-page">
      <NativeBiPageHeader
        title="Compras"
        description="Órdenes de compra, proveedores y recepciones — últimos 30 días"
      />

      <NativeBiFilterBar
        filters={filters}
        definitions={PURCHASING_FILTER_DEFS}
        optionsByKey={optionsByKey}
        loadingKeys={loadingKeys}
        onFilterChange={setFilter}
        onFilterReset={resetFilter}
        onResetAll={resetAll}
        hasActiveFilters={hasActiveFilters}
      />

      {/* KPI row */}
      {execErr ? (
        <NbErrorState message="Error al cargar datos de compras." onRetry={() => refetchExec()} />
      ) : (
        <div className="nb-card-grid">
          <KpiCard label="Órdenes de compra"  value={totalPo}                                                loading={loadingExec} />
          <KpiCard label="Monto OC"           value={loadingExec ? '—' : fmtAmt(totalPoAmt)}               loading={loadingExec} />
          <KpiCard label="Monto recibido"     value={loadingExec ? '—' : fmtAmt(totalRecvAmt)}             loading={loadingExec} />
          <KpiCard label="% Recibido vs OC"   value={loadingExec ? '—' : fmtPct(pctReceived)}              loading={loadingExec} />
          <KpiCard label="Proveedores activos" value={maxSuppliers}                                         loading={loadingExec} />
          <KpiCard label="Recepciones"        value={totalRecvCnt}                                         loading={loadingExec} />
          <KpiCard label="Promedio OC"        value={loadingExec ? '—' : fmtAmt(avgPoAmt)}                 loading={loadingExec} />
          <KpiCard
            label="Brecha pendiente"
            value={loadingExec ? '—' : fmtAmt(gap)}
            subLabel={gap > 0 ? 'OC emitidas sin recibir' : undefined}
            loading={loadingExec}
          />
        </div>
      )}

      <div className="db-card">
        <div
          className="db-card-header nb-tab-bar"
          style={{ paddingLeft: 4, paddingRight: 16, gap: 0, borderBottom: '1px solid var(--c-border)', overflowX: 'auto' }}
          role="tablist"
          aria-label="Secciones de compras"
        >
          {tabs.map((t) => (
            <TabButton key={t.id} id={t.id} label={t.label} active={tab === t.id} onClick={() => setTab(t.id)} />
          ))}
        </div>

        {/* ── Resumen ─────────────────────────────────────────────────────── */}
        {tab === 'resumen' && (
          loadingExec ? (
            <div style={{ padding: 24 }}>
              {Array.from({ length: 4 }).map((_, i) => (
                <div key={i} className="cp-skeleton" style={{ height: 44, borderRadius: 6, marginBottom: 8 }} />
              ))}
            </div>
          ) : !execData || execData.length === 0 ? (
            <NbEmptyState message="Sin datos de compras disponibles. Disponible al completar carga histórica." icon="table" />
          ) : (
            <div style={{ padding: '16px 20px' }}>
              {/* OC vs Recibido comparison */}
              <div style={{ fontSize: 12.5, fontWeight: 600, color: 'var(--c-text-muted)', marginBottom: 10, textTransform: 'uppercase', letterSpacing: '0.04em' }}>
                OC vs Recibido (30 días)
              </div>
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16, marginBottom: 24 }}>
                <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
                  <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: 13 }}>
                    <span style={{ color: 'var(--c-text-muted)' }}>Monto OC emitido</span>
                    <span style={{ fontWeight: 600, fontVariantNumeric: 'tabular-nums' }}>{fmtAmt(totalPoAmt)}</span>
                  </div>
                  <div style={{ height: 8, backgroundColor: 'var(--c-border)', borderRadius: 4 }}>
                    <div style={{ width: '100%', height: '100%', backgroundColor: 'var(--brand-primary, #2563EB)', borderRadius: 4 }} />
                  </div>
                  <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: 13 }}>
                    <span style={{ color: 'var(--c-text-muted)' }}>Monto recibido</span>
                    <span style={{ fontWeight: 600, fontVariantNumeric: 'tabular-nums', color: pctReceived < 70 ? '#D97706' : '#16A34A' }}>{fmtAmt(totalRecvAmt)}</span>
                  </div>
                  <div style={{ height: 8, backgroundColor: 'var(--c-border)', borderRadius: 4 }}>
                    <div style={{ width: `${Math.min(pctReceived, 100)}%`, height: '100%', backgroundColor: pctReceived < 70 ? '#D97706' : '#16A34A', borderRadius: 4, transition: 'width 400ms ease' }} />
                  </div>
                  <div style={{ fontSize: 12, color: 'var(--c-text-faint)' }}>
                    {fmtPct(pctReceived)} de las OC ha sido recibido · Brecha: {fmtAmt(gap)}
                  </div>
                </div>

                {/* Top proveedores mini bar */}
                <div>
                  {suppliers.length === 0 ? (
                    <NbEmptyState message="Sin datos de proveedores aún." icon="table" />
                  ) : (
                    <NativeBiMiniBarList
                      title="Top proveedores por monto OC"
                      items={suppliers.slice(0, 5).map((s) => ({
                        label: s.supplierName ?? s.supplierCode,
                        sublabel: s.supplierCode,
                        value: s.poAmount,
                        pct: pct(s.poAmount, totalSuppAmt),
                        color: 'var(--brand-primary, #2563EB)',
                      }))}
                      formatValue={fmtAmt}
                      maxItems={5}
                    />
                  )}
                </div>
              </div>

              {/* Evolución últimos 5 días */}
              <div style={{ fontSize: 12.5, fontWeight: 600, color: 'var(--c-text-muted)', marginBottom: 10, textTransform: 'uppercase', letterSpacing: '0.04em' }}>
                Evolución diaria (últimos 5 períodos)
              </div>
              <div className="nb-table-scroll">
                <table className="db-table">
                  <thead>
                    <tr>
                      <th>Fecha</th>
                      <th style={{ textAlign: 'right' }}>OC</th>
                      <th style={{ textAlign: 'right' }}>Monto OC</th>
                      <th style={{ textAlign: 'right' }}>Recibido</th>
                      <th style={{ textAlign: 'right' }}>% Recibido</th>
                      <th style={{ textAlign: 'right' }}>Brecha</th>
                    </tr>
                  </thead>
                  <tbody>
                    {execData.slice(-5).reverse().map((d) => {
                      const p = d.poAmount > 0 ? pct(d.receivedAmount, d.poAmount) : 0
                      return (
                        <tr key={d.purchaseDate}>
                          <td style={{ fontSize: 13 }}>{fmtDate(d.purchaseDate)}</td>
                          <td style={{ textAlign: 'right', fontVariantNumeric: 'tabular-nums' }}>{d.poCount}</td>
                          <td style={{ textAlign: 'right', fontVariantNumeric: 'tabular-nums' }}>{fmtAmt(d.poAmount)}</td>
                          <td style={{ textAlign: 'right', fontVariantNumeric: 'tabular-nums' }}>{fmtAmt(d.receivedAmount)}</td>
                          <td style={{ textAlign: 'right', fontVariantNumeric: 'tabular-nums', color: p < 50 ? '#D97706' : '#16A34A' }}>{fmtPct(p)}</td>
                          <td style={{ textAlign: 'right', fontVariantNumeric: 'tabular-nums', color: (d.poAmount - d.receivedAmount) > 0 ? '#D97706' : 'inherit' }}>
                            {fmtAmt(d.poAmount - d.receivedAmount)}
                          </td>
                        </tr>
                      )
                    })}
                  </tbody>
                </table>
              </div>
              {topSupplier && (
                <p style={{ fontSize: 12, color: 'var(--c-text-faint)', marginTop: 16 }}>
                  Proveedor líder: <strong>{topSupplier.supplierName ?? topSupplier.supplierCode}</strong> · {fmtAmt(topSupplier.poAmount)} en OC
                </p>
              )}
            </div>
          )
        )}

        {/* ── Proveedores ─────────────────────────────────────────────────── */}
        {tab === 'suppliers' && (
          suppData?.data.length === 0 && !loadingSupp ? (
            <NbEmptyState message="Sin datos de proveedores en el período analizado." icon="table" />
          ) : (
            <SortableTable
              data={suppData?.data ?? []}
              columns={suppCols}
              meta={suppData?.meta ?? EMPTY_META}
              sortBy={suppP.sortBy}
              sortDir={suppP.sortDir}
              onPageChange={(offset) => setSuppP((p) => ({ ...p, offset }))}
              onSortChange={(sortBy, sortDir) => setSuppP((p) => ({ ...p, sortBy, sortDir, offset: 0 }))}
              isLoading={loadingSupp}
              rowKey={(r) => r.supplierCode}
            />
          )
        )}

        {/* ── Recepciones ─────────────────────────────────────────────────── */}
        {tab === 'receiving' && (
          recvData?.data.length === 0 && !loadingRecv ? (
            <NbEmptyState message="Sin datos de recepciones en el período analizado." icon="table" />
          ) : (
            <SortableTable
              data={recvData?.data ?? []}
              columns={recvCols}
              meta={recvData?.meta ?? EMPTY_META}
              sortBy={recvP.sortBy}
              sortDir={recvP.sortDir}
              onPageChange={(offset) => setRecvP((p) => ({ ...p, offset }))}
              onSortChange={(sortBy, sortDir) => setRecvP((p) => ({ ...p, sortBy, sortDir, offset: 0 }))}
              isLoading={loadingRecv}
              rowKey={(r) => r.supplierCode}
            />
          )
        )}

        {/* ── Evolución OC ────────────────────────────────────────────────── */}
        {tab === 'evolution' && (
          !execData || execData.length === 0 ? (
            <NbEmptyState message="Sin datos de evolución de órdenes de compra en el período." icon="chart" />
          ) : (
            <SortableTable
              data={execData}
              columns={execCols}
              meta={{ limit: execData.length, offset: 0, count: execData.length, hasMore: false }}
              isLoading={loadingExec}
              rowKey={(r) => r.purchaseDate}
              onPageChange={() => {}}
              onSortChange={() => {}}
            />
          )
        )}
      </div>
    </div>
  )
}
