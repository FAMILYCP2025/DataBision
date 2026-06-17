import { useState } from 'react'
import NativeBiPageHeader from '../components/nativebi/NativeBiPageHeader'
import KpiCard from '../components/nativebi/KpiCard'
import SortableTable, { type ColumnDef } from '../components/nativebi/SortableTable'
import { NbErrorState, NbEmptyState } from '../components/nativebi/NativeBiState'
import {
  useBiFinanceExecutive,
  useBiFinanceArAging,
  useBiFinanceApAging,
} from '../hooks/useProcessBi'
import type { FinanceArAging, FinanceApAging } from '../types/processBi'
import type { NbPagedMeta, PaginationParams } from '../types/nativeBi'

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
  if (total === 0) return 0
  return (part / total) * 100
}

function riskLevel(r: FinanceArAging): { text: string; color: string } {
  const ratio = r.balanceDue > 0 ? r.aging90Plus / r.balanceDue : 0
  if (ratio > 0.3 || r.aging90Plus > 0) return { text: 'Alto', color: '#DC2626' }
  if (r.overdueAmount > 0)             return { text: 'Medio', color: '#D97706' }
  return                                       { text: 'Bajo',  color: '#16A34A' }
}

type Tab = 'resumen' | 'ar' | 'ap' | 'risk'

const LIMIT = 20
const EMPTY_META: NbPagedMeta = { limit: LIMIT, offset: 0, count: 0, hasMore: false }

function initPag(sortBy: string): PaginationParams {
  return { limit: LIMIT, offset: 0, sortBy, sortDir: 'desc' }
}

const tabs: { id: Tab; label: string }[] = [
  { id: 'resumen', label: 'Resumen' },
  { id: 'ar',      label: 'Cuentas por cobrar' },
  { id: 'ap',      label: 'Cuentas por pagar' },
  { id: 'risk',    label: 'Riesgo +90d' },
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
        fontFamily: 'inherit', marginBottom: -1, whiteSpace: 'nowrap',
        transition: 'color 150ms, border-color 150ms',
      }}
    >
      {label}
    </button>
  )
}

function RiskBadge({ r }: { r: FinanceArAging }) {
  const { text, color } = riskLevel(r)
  return (
    <span style={{
      display: 'inline-block', padding: '2px 8px', borderRadius: 4,
      fontSize: 12, fontWeight: 600, color: '#fff', backgroundColor: color,
    }}>
      {text}
    </span>
  )
}

function AgingBar({ label, amount, total, color }: { label: string; amount: number; total: number; color: string }) {
  const p = pct(amount, total)
  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 4, fontSize: 13 }}>
        <span style={{ color: 'var(--c-text)', fontWeight: 500 }}>{label}</span>
        <span style={{ fontVariantNumeric: 'tabular-nums', color: 'var(--c-text-muted)' }}>
          {fmtAmt(amount)} · {p.toFixed(1)}%
        </span>
      </div>
      <div style={{ height: 6, backgroundColor: 'var(--c-border)', borderRadius: 3 }}>
        <div style={{ width: `${p}%`, height: '100%', backgroundColor: color, borderRadius: 3, transition: 'width 400ms ease' }} />
      </div>
    </div>
  )
}

export default function FinanceDashboardPage() {
  const [tab, setTab] = useState<Tab>('resumen')
  const [arP, setArP] = useState<PaginationParams>(initPag('overdueAmount'))
  const [apP, setApP] = useState<PaginationParams>(initPag('overdueAmount'))

  const { data: execData, isLoading: loadingExec, error: execErr, refetch: refetchExec } = useBiFinanceExecutive(30)
  const { data: arData,   isLoading: loadingAr } = useBiFinanceArAging(arP)
  const { data: apData,   isLoading: loadingAp } = useBiFinanceApAging(apP)

  const latest      = execData && execData.length > 0 ? execData[execData.length - 1] : null
  const totalArOv   = execData?.reduce((s, d) => s + d.arOverdue, 0) ?? 0
  const totalInvAmt = execData?.reduce((s, d) => s + d.newInvoicesAmount, 0) ?? 0
  const totalInvCnt = execData?.reduce((s, d) => s + d.newInvoicesCount, 0) ?? 0
  const avgOvPct    = latest?.arOverduePct ?? 0

  const allAr        = arData?.data ?? []
  const riskItems    = allAr.filter((r) => r.aging90Plus > 0)
  const withOverdue  = allAr.filter((r) => r.overdueAmount > 0)
  const topDebtor    = [...allAr].sort((a, b) => b.overdueAmount - a.overdueAmount)[0]
  const totalBalance = allAr.reduce((s, r) => s + r.balanceDue, 0)
  const avgDebt      = allAr.length > 0 ? totalBalance / allAr.length : 0
  const total90Plus  = allAr.reduce((s, r) => s + r.aging90Plus, 0)

  // Aging bucket totals for visualization (from current AR page)
  const bucket0to30  = allAr.reduce((s, r) => s + r.aging0To30, 0)
  const bucket31to60 = allAr.reduce((s, r) => s + r.aging31To60, 0)
  const bucket61to90 = allAr.reduce((s, r) => s + r.aging61To90, 0)
  const bucket90Plus = allAr.reduce((s, r) => s + r.aging90Plus, 0)
  const bucketTotal  = bucket0to30 + bucket31to60 + bucket61to90 + bucket90Plus

  const arCols: ColumnDef<FinanceArAging>[] = [
    {
      key: 'risk',
      label: 'Riesgo',
      render: (r) => <RiskBadge r={r} />,
    },
    {
      key: 'name',
      label: 'Cliente',
      render: (r) => (
        <div>
          <div style={{ fontWeight: 500 }}>{r.cardName ?? r.cardCode}</div>
          <div style={{ fontSize: 11.5, color: 'var(--c-text-faint)' }}>{r.cardCode}</div>
        </div>
      ),
    },
    {
      key: 'balanceDue',
      label: 'Saldo',
      sortKey: 'balanceDue',
      align: 'right',
      render: (r) => <span style={{ fontVariantNumeric: 'tabular-nums' }}>{fmtAmt(r.balanceDue)}</span>,
    },
    {
      key: 'overdueAmount',
      label: 'Vencido',
      sortKey: 'overdueAmount',
      align: 'right',
      render: (r) => (
        <span style={{ fontVariantNumeric: 'tabular-nums', color: r.overdueAmount > 0 ? '#DC2626' : 'inherit' }}>
          {fmtAmt(r.overdueAmount)}
        </span>
      ),
    },
    {
      key: '0-30',
      label: '0-30d',
      align: 'right',
      render: (r) => <span style={{ fontVariantNumeric: 'tabular-nums' }}>{fmtAmt(r.aging0To30)}</span>,
    },
    {
      key: '31-60',
      label: '31-60d',
      align: 'right',
      render: (r) => <span style={{ fontVariantNumeric: 'tabular-nums' }}>{fmtAmt(r.aging31To60)}</span>,
    },
    {
      key: '90+',
      label: '+90d',
      sortKey: 'aging90Plus',
      align: 'right',
      render: (r) => (
        <span style={{ fontVariantNumeric: 'tabular-nums', color: r.aging90Plus > 0 ? '#DC2626' : 'inherit' }}>
          {fmtAmt(r.aging90Plus)}
        </span>
      ),
    },
    {
      key: 'pct90',
      label: '% s/saldo',
      align: 'right',
      render: (r) => {
        const p = r.balanceDue > 0 ? (r.aging90Plus / r.balanceDue) * 100 : 0
        return (
          <span style={{ fontVariantNumeric: 'tabular-nums', color: p > 30 ? '#DC2626' : p > 10 ? '#D97706' : 'var(--c-text-muted)' }}>
            {p > 0 ? fmtPct(p) : '—'}
          </span>
        )
      },
    },
    {
      key: 'lastInvoice',
      label: 'Últ. factura',
      align: 'right',
      render: (r) => fmtDate(r.lastInvoiceDate),
    },
  ]

  const riskCols: ColumnDef<FinanceArAging>[] = [
    {
      key: 'risk',
      label: 'Riesgo',
      render: (r) => <RiskBadge r={r} />,
    },
    {
      key: 'name',
      label: 'Cliente',
      render: (r) => (
        <div>
          <div style={{ fontWeight: 500 }}>{r.cardName ?? r.cardCode}</div>
          <div style={{ fontSize: 11.5, color: 'var(--c-text-faint)' }}>{r.cardCode}</div>
        </div>
      ),
    },
    {
      key: 'balanceDue',
      label: 'Saldo total',
      align: 'right',
      render: (r) => <span style={{ fontVariantNumeric: 'tabular-nums' }}>{fmtAmt(r.balanceDue)}</span>,
    },
    {
      key: '90+',
      label: '+90d',
      align: 'right',
      render: (r) => (
        <span style={{ fontVariantNumeric: 'tabular-nums', color: '#DC2626', fontWeight: 600 }}>
          {fmtAmt(r.aging90Plus)}
        </span>
      ),
    },
    {
      key: 'pct90',
      label: '% s/saldo',
      align: 'right',
      render: (r) => {
        const p = r.balanceDue > 0 ? (r.aging90Plus / r.balanceDue) * 100 : 0
        const color = p > 50 ? '#DC2626' : p > 20 ? '#D97706' : '#D97706'
        return (
          <span style={{ fontVariantNumeric: 'tabular-nums', color, fontWeight: 600 }}>
            {fmtPct(p)}
          </span>
        )
      },
    },
    {
      key: 'overdueAmount',
      label: 'Total vencido',
      align: 'right',
      render: (r) => (
        <span style={{ fontVariantNumeric: 'tabular-nums', color: r.overdueAmount > 0 ? '#DC2626' : 'inherit' }}>
          {fmtAmt(r.overdueAmount)}
        </span>
      ),
    },
    {
      key: 'lastInvoice',
      label: 'Últ. factura',
      align: 'right',
      render: (r) => fmtDate(r.lastInvoiceDate),
    },
  ]

  const apCols: ColumnDef<FinanceApAging>[] = [
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
      key: 'balanceDue',
      label: 'Saldo',
      sortKey: 'balanceDue',
      align: 'right',
      render: (r) => <span style={{ fontVariantNumeric: 'tabular-nums' }}>{fmtAmt(r.balanceDue)}</span>,
    },
    {
      key: 'overdueAmount',
      label: 'Vencido',
      sortKey: 'overdueAmount',
      align: 'right',
      render: (r) => (
        <span style={{ fontVariantNumeric: 'tabular-nums', color: r.overdueAmount > 0 ? '#DC2626' : 'inherit' }}>
          {fmtAmt(r.overdueAmount)}
        </span>
      ),
    },
    {
      key: '0-30',
      label: '0-30d',
      align: 'right',
      render: (r) => <span style={{ fontVariantNumeric: 'tabular-nums' }}>{fmtAmt(r.aging0To30)}</span>,
    },
    {
      key: '31-60',
      label: '31-60d',
      align: 'right',
      render: (r) => <span style={{ fontVariantNumeric: 'tabular-nums' }}>{fmtAmt(r.aging31To60)}</span>,
    },
    {
      key: '90+',
      label: '+90d',
      align: 'right',
      render: (r) => (
        <span style={{ fontVariantNumeric: 'tabular-nums', color: r.aging90Plus > 0 ? '#DC2626' : 'inherit' }}>
          {fmtAmt(r.aging90Plus)}
        </span>
      ),
    },
  ]

  return (
    <div className="cp-page">
      <NativeBiPageHeader
        title="Finanzas"
        description="Cuentas por cobrar y por pagar — vencimientos y aging"
      />

      {execErr ? (
        <NbErrorState message="Error al cargar datos financieros." onRetry={() => refetchExec()} />
      ) : (
        <div className="nb-card-grid">
          <KpiCard
            label="AR vencido"
            value={loadingExec ? '—' : fmtAmt(totalArOv)}
            subLabel="Cuentas por cobrar vencidas"
            loading={loadingExec}
          />
          <KpiCard
            label="% vencido (período)"
            value={loadingExec ? '—' : fmtPct(avgOvPct)}
            loading={loadingExec}
          />
          <KpiCard
            label="Facturas emitidas (30d)"
            value={totalInvCnt}
            loading={loadingExec}
          />
          <KpiCard
            label="Monto facturado (30d)"
            value={loadingExec ? '—' : fmtAmt(totalInvAmt)}
            loading={loadingExec}
          />
        </div>
      )}

      <div className="db-card">
        <div
          className="db-card-header nb-tab-bar"
          style={{ paddingLeft: 4, paddingRight: 16, gap: 0, borderBottom: '1px solid var(--c-border)', overflowX: 'auto' }}
          role="tablist"
          aria-label="Secciones de finanzas"
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
            <NbEmptyState
              message="Sin datos financieros disponibles. Disponible al completar carga histórica."
              icon="table"
            />
          ) : (
            <div style={{ padding: '16px 20px' }}>
              {/* Secondary KPIs */}
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(160px, 1fr))', gap: 12, marginBottom: 24 }}>
                {[
                  { label: 'Riesgo +90d',            value: total90Plus > 0 ? fmtAmt(total90Plus) : '—', highlight: total90Plus > 0 },
                  { label: 'Clientes c/ vencido',     value: withOverdue.length > 0 ? `${withOverdue.length}` : '—', highlight: false },
                  { label: 'Top deudor',              value: topDebtor ? (topDebtor.cardName ?? topDebtor.cardCode) : '—', small: true, highlight: false },
                  { label: 'Deuda promedio cliente',  value: avgDebt > 0 ? fmtAmt(avgDebt) : '—', highlight: false },
                ].map((kpi) => (
                  <div key={kpi.label} className="db-stat-card">
                    <span className="db-stat-label">{kpi.label}</span>
                    <span
                      className="db-stat-value"
                      style={{
                        fontSize: kpi.small ? 13.5 : 20,
                        fontVariantNumeric: 'tabular-nums',
                        color: kpi.highlight ? '#DC2626' : 'inherit',
                        overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap',
                      }}
                    >
                      {kpi.value}
                    </span>
                  </div>
                ))}
              </div>

              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 24, marginBottom: 24 }}>
                {/* Aging bucket visualization */}
                <div>
                  <div style={{ fontSize: 12.5, fontWeight: 600, color: 'var(--c-text-muted)', marginBottom: 14, textTransform: 'uppercase', letterSpacing: '0.04em' }}>
                    Distribución aging AR
                  </div>
                  {bucketTotal > 0 ? (
                    <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
                      <AgingBar label="0 – 30 días"  amount={bucket0to30}  total={bucketTotal} color="#16A34A" />
                      <AgingBar label="31 – 60 días" amount={bucket31to60} total={bucketTotal} color="#D97706" />
                      <AgingBar label="61 – 90 días" amount={bucket61to90} total={bucketTotal} color="#EA580C" />
                      <AgingBar label="+90 días"     amount={bucket90Plus} total={bucketTotal} color="#DC2626" />
                    </div>
                  ) : (
                    <p style={{ fontSize: 13, color: 'var(--c-text-faint)' }}>
                      Disponible al cargar datos de AR aging.
                    </p>
                  )}
                </div>

                {/* Evolution table */}
                <div>
                  <div style={{ fontSize: 12.5, fontWeight: 600, color: 'var(--c-text-muted)', marginBottom: 14, textTransform: 'uppercase', letterSpacing: '0.04em' }}>
                    Evolución — últimos 5 períodos
                  </div>
                  <div className="nb-table-scroll">
                    <table className="db-table" style={{ fontSize: 12.5 }}>
                      <thead>
                        <tr>
                          <th>Fecha</th>
                          <th style={{ textAlign: 'right' }}>AR vencido</th>
                          <th style={{ textAlign: 'right' }}>% venc.</th>
                          <th style={{ textAlign: 'right' }}>Facturas</th>
                        </tr>
                      </thead>
                      <tbody>
                        {execData.slice(-5).reverse().map((d, i) => (
                          <tr key={i}>
                            <td style={{ fontSize: 12.5 }}>{fmtDate(d.periodDate)}</td>
                            <td style={{ textAlign: 'right', fontVariantNumeric: 'tabular-nums', color: d.arOverdue > 0 ? '#DC2626' : 'inherit' }}>
                              {fmtAmt(d.arOverdue)}
                            </td>
                            <td style={{ textAlign: 'right', fontVariantNumeric: 'tabular-nums' }}>
                              {fmtPct(d.arOverduePct)}
                            </td>
                            <td style={{ textAlign: 'right', fontVariantNumeric: 'tabular-nums' }}>
                              {d.newInvoicesCount}
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                </div>
              </div>

              {riskItems.length > 0 && (
                <div style={{ padding: '10px 14px', backgroundColor: '#FEF2F2', border: '1px solid #FECACA', borderRadius: 6, fontSize: 13, color: '#991B1B' }}>
                  <strong>{riskItems.length} cliente(s)</strong> con saldo vencido mayor a 90 días — total en riesgo:{' '}
                  <strong>{fmtAmt(total90Plus)}</strong>. Revisar en la pestaña "Riesgo +90d".
                </div>
              )}
            </div>
          )
        )}

        {/* ── AR ──────────────────────────────────────────────────────────── */}
        {tab === 'ar' && (
          arData?.data.length === 0 && !loadingAr ? (
            <NbEmptyState message="Sin datos de cuentas por cobrar en el período analizado." icon="table" />
          ) : (
            <SortableTable
              data={arData?.data ?? []}
              columns={arCols}
              meta={arData?.meta ?? EMPTY_META}
              sortBy={arP.sortBy}
              sortDir={arP.sortDir}
              onPageChange={(offset) => setArP((p) => ({ ...p, offset }))}
              onSortChange={(sortBy, sortDir) => setArP((p) => ({ ...p, sortBy, sortDir, offset: 0 }))}
              isLoading={loadingAr}
              rowKey={(r) => r.cardCode}
            />
          )
        )}

        {/* ── AP ──────────────────────────────────────────────────────────── */}
        {tab === 'ap' && (
          apData?.data.length === 0 && !loadingAp ? (
            <div style={{ padding: '28px 24px' }}>
              <div style={{ maxWidth: 420, margin: '0 auto', textAlign: 'center' }}>
                <div style={{ fontSize: 28, marginBottom: 12 }}>📄</div>
                <div style={{ fontSize: 15, fontWeight: 600, color: 'var(--c-text)', marginBottom: 8 }}>
                  Cuentas por pagar
                </div>
                <p style={{ fontSize: 13.5, color: 'var(--c-text-muted)', lineHeight: 1.6, marginBottom: 16 }}>
                  Disponible al completar la carga histórica de facturas de proveedor (OPCH).
                  Esta sección mostrará aging, saldos pendientes y alertas de vencimiento.
                </p>
                <div style={{ padding: '10px 16px', backgroundColor: '#F0F9FF', border: '1px solid #BAE6FD', borderRadius: 6, fontSize: 12.5, color: '#0369A1', textAlign: 'left' }}>
                  <strong>Próximamente:</strong> monto AP vencido, proveedor con mayor deuda, aging 30/60/90d.
                </div>
              </div>
            </div>
          ) : (
            <SortableTable
              data={apData?.data ?? []}
              columns={apCols}
              meta={apData?.meta ?? EMPTY_META}
              sortBy={apP.sortBy}
              sortDir={apP.sortDir}
              onPageChange={(offset) => setApP((p) => ({ ...p, offset }))}
              onSortChange={(sortBy, sortDir) => setApP((p) => ({ ...p, sortBy, sortDir, offset: 0 }))}
              isLoading={loadingAp}
              rowKey={(r) => r.supplierCode}
            />
          )
        )}

        {/* ── Risk +90d ───────────────────────────────────────────────────── */}
        {tab === 'risk' && (
          riskItems.length === 0 && !loadingAr ? (
            <NbEmptyState message="Sin cuentas con aging superior a 90 días. ¡Cartera sana!" icon="table" />
          ) : (
            <>
              {riskItems.length > 0 && (
                <div style={{ padding: '12px 20px', backgroundColor: '#FEF2F2', borderBottom: '1px solid #FECACA', display: 'flex', gap: 8, alignItems: 'center' }}>
                  <span style={{ fontSize: 16 }}>🔴</span>
                  <span style={{ fontSize: 13, color: '#991B1B' }}>
                    <strong>{riskItems.length} cliente(s)</strong> con saldo +90 días — exposición total:{' '}
                    <strong>{fmtAmt(total90Plus)}</strong>
                  </span>
                </div>
              )}
              <SortableTable
                data={riskItems}
                columns={riskCols}
                meta={{ limit: riskItems.length, offset: 0, count: riskItems.length, hasMore: false }}
                isLoading={loadingAr}
                rowKey={(r) => r.cardCode}
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
