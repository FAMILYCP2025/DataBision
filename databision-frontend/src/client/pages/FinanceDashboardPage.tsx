import { useState } from 'react'
import NativeBiPageHeader from '../components/nativebi/NativeBiPageHeader'
import KpiCard from '../components/nativebi/KpiCard'
import SortableTable, { type ColumnDef } from '../components/nativebi/SortableTable'
import { NbErrorState, NbEmptyState } from '../components/nativebi/NativeBiState'
import NativeBiFilterBar from '../components/nativebi/NativeBiFilterBar'
import {
  useBiFinanceExecutive,
  useBiFinanceArAging,
  useBiFinanceApAging,
  useBiIncomeStatement,
  useBiBalanceSheet,
  useBiEbitda,
  useBiChartOfAccounts,
  useBiFinanceValidations,
} from '../hooks/useProcessBi'
import { useNativeBiFilters } from '../hooks/useNativeBiFilters'
import type { FinanceArAging, FinanceApAging } from '../types/processBi'
import type { NbPagedMeta, PaginationParams } from '../types/nativeBi'
import type { NativeBiFilterDefinition } from '../types/nativeBiFilters'
import { NbStackedBarChart, NbAreaChart, NbLineChart } from '../components/charts'
import NativeBiFinanceReadinessPanel from '../components/nativebi/NativeBiFinanceReadinessPanel'

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

type Tab = 'resumen' | 'ar' | 'ap' | 'risk' | 'tendencia' | 'resultados' | 'balance' | 'ebitda' | 'cuentas' | 'validaciones'

const LIMIT = 20
const EMPTY_META: NbPagedMeta = { limit: LIMIT, offset: 0, count: 0, hasMore: false }

function initPag(sortBy: string): PaginationParams {
  return { limit: LIMIT, offset: 0, sortBy, sortDir: 'desc' }
}

const tabs: { id: Tab; label: string }[] = [
  { id: 'resumen',      label: 'Resumen' },
  { id: 'ar',           label: 'Cuentas por cobrar' },
  { id: 'ap',           label: 'Cuentas por pagar' },
  { id: 'risk',         label: 'Riesgo +90d' },
  { id: 'tendencia',    label: 'Tendencia' },
  { id: 'resultados',   label: 'Estado de Resultados' },
  { id: 'balance',      label: 'Balance General' },
  { id: 'ebitda',       label: 'EBITDA' },
  { id: 'cuentas',      label: 'Plan de Cuentas' },
  { id: 'validaciones', label: 'Validaciones' },
]

const FINANCE_FILTER_DEFS: NativeBiFilterDefinition[] = [
  { key: 'year',  label: 'Año', type: 'year',  source: 'static', modules: ['finance'] },
  { key: 'month', label: 'Mes', type: 'month', source: 'static', modules: ['finance'], isAdvanced: true, placeholder: 'Todos' },
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

function FinancialDataPending({
  title,
  description,
  requiredTables,
}: {
  title: string
  description: string
  requiredTables: string[]
}) {
  return (
    <div style={{ padding: 40, textAlign: 'center' }}>
      <div style={{
        display: 'inline-flex', alignItems: 'center', justifyContent: 'center',
        width: 56, height: 56, borderRadius: 12, backgroundColor: '#EFF6FF',
        marginBottom: 16,
      }}>
        <svg width="24" height="24" viewBox="0 0 24 24" fill="none">
          <path d="M9 5H7a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2V7a2 2 0 0 0-2-2h-2M9 5a2 2 0 0 0 2 2h2a2 2 0 0 0 2-2M9 5a2 2 0 0 0 2-2h2a2 2 0 0 0 2 2" stroke="#2563EB" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
          <path d="M9 12h6M9 16h4" stroke="#2563EB" strokeWidth="2" strokeLinecap="round"/>
        </svg>
      </div>
      <h3 style={{ fontSize: 16, fontWeight: 600, color: 'var(--c-text)', marginBottom: 8 }}>{title}</h3>
      <p style={{ fontSize: 13.5, color: 'var(--c-text-muted)', maxWidth: 420, margin: '0 auto 20px' }}>
        {description}
      </p>
      <div style={{
        display: 'inline-block', textAlign: 'left', padding: '16px 20px',
        border: '1px solid var(--c-border)', borderRadius: 8,
        backgroundColor: 'var(--c-surface)', maxWidth: 480,
      }}>
        <p style={{ fontSize: 12, fontWeight: 600, color: 'var(--c-text-muted)', marginBottom: 8, textTransform: 'uppercase', letterSpacing: '0.5px' }}>
          Tablas MART requeridas
        </p>
        {requiredTables.map(t => (
          <div key={t} style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 6 }}>
            <div style={{ width: 6, height: 6, borderRadius: '50%', backgroundColor: '#D97706', flexShrink: 0 }} />
            <code style={{ fontSize: 12.5, color: 'var(--c-text)', fontFamily: 'monospace' }}>{t}</code>
          </div>
        ))}
      </div>
      <p style={{ fontSize: 12, color: 'var(--c-text-faint)', marginTop: 16 }}>
        Habilitación estimada: Sprint 13 — Módulo contable MART
      </p>
    </div>
  )
}

export default function FinanceDashboardPage() {
  const { filters, setFilter, resetFilter, resetAll, hasActiveFilters } = useNativeBiFilters('finance')
  const [tab, setTab] = useState<Tab>('resumen')
  const [arP, setArP] = useState<PaginationParams>(initPag('overdueAmount'))
  const [apP, setApP] = useState<PaginationParams>(initPag('overdueAmount'))

  const { data: execData, isLoading: loadingExec, error: execErr, refetch: refetchExec } = useBiFinanceExecutive(30)
  const { data: arData,   isLoading: loadingAr } = useBiFinanceArAging(arP)
  const { data: apData,   isLoading: loadingAp } = useBiFinanceApAging(apP)

  const { data: isData,  isLoading: loadingIs  } = useBiIncomeStatement({
    year:  filters.year  ? Number(filters.year)  : undefined,
    month: filters.month ? Number(filters.month) : undefined,
  })
  const { data: bsData  } = useBiBalanceSheet()
  const { data: ebData,  isLoading: loadingEb  } = useBiEbitda(24)
  const { data: coaData } = useBiChartOfAccounts(false)
  const { data: valData, isLoading: loadingVal, isError: valError } = useBiFinanceValidations()

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

      <NativeBiFilterBar
        filters={filters}
        definitions={FINANCE_FILTER_DEFS}
        onFilterChange={setFilter}
        onFilterReset={resetFilter}
        onResetAll={resetAll}
        hasActiveFilters={hasActiveFilters}
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
              {/* Executive header */}
              <div style={{ marginBottom: 20 }}>
                <h2 style={{ fontSize: 18, fontWeight: 700, color: 'var(--c-text)', marginBottom: 4 }}>
                  Finanzas — Vista ejecutiva
                </h2>
                <p style={{ fontSize: 13, color: 'var(--c-text-muted)' }}>
                  Posición financiera neta, riesgo de cartera y tendencia de flujo
                </p>
              </div>

              {/* Readiness panel — only visible when MART data is incomplete */}
              <NativeBiFinanceReadinessPanel />

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

              {/* AR vs AP trend chart */}
              <div style={{ marginBottom: 24 }}>
                <p style={{ fontSize: 13, fontWeight: 600, marginBottom: 8, color: 'var(--c-text)' }}>
                  Tendencia CxC / CxP
                </p>
                <NbAreaChart
                  series={[
                    {
                      name: 'CxC (AR)',
                      data: execData.map((d) => ({
                        name: d.periodDate,
                        value: d.arTotal,
                      })),
                      color: '#2563EB',
                    },
                    {
                      name: 'CxP (AP)',
                      data: execData.map((d) => ({
                        name: d.periodDate,
                        value: d.apTotal ?? 0,
                      })),
                      color: '#DC2626',
                    },
                  ]}
                  height={200}
                  loading={loadingExec}
                  valueFormatter={(v) => v.toLocaleString('es-CL', { maximumFractionDigits: 0 })}
                />
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
            <>
              {arData && arData.data.length > 0 && (
                <div style={{ padding: '16px 20px 0' }}>
                  <p style={{ fontSize: 13, fontWeight: 600, marginBottom: 8, color: 'var(--c-text)' }}>
                    Distribución por vencimiento — Top 10 clientes
                  </p>
                  <NbStackedBarChart
                    categories={arData.data.slice(0, 10).map((r) => r.cardName ?? r.cardCode)}
                    series={[
                      { name: 'Al día',  data: arData.data.slice(0, 10).map((r) => Math.max(0, r.balanceDue - r.overdueAmount)), color: '#16A34A' },
                      { name: '1–30d',   data: arData.data.slice(0, 10).map((r) => r.aging0To30),   color: '#D97706' },
                      { name: '31–60d',  data: arData.data.slice(0, 10).map((r) => r.aging31To60),  color: '#EA580C' },
                      { name: '+90d',    data: arData.data.slice(0, 10).map((r) => r.aging90Plus),  color: '#DC2626' },
                    ]}
                    horizontal={true}
                    height={280}
                    loading={loadingAr}
                    valueFormatter={(v) => v.toLocaleString('es-CL', { maximumFractionDigits: 0 })}
                  />
                  <div style={{ height: 1, background: 'var(--c-border)', margin: '12px 0 0' }} />
                </div>
              )}
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
            </>
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
            <>
              {apData && apData.data.length > 0 && (
                <div style={{ padding: '16px 20px 0' }}>
                  <p style={{ fontSize: 13, fontWeight: 600, marginBottom: 8, color: 'var(--c-text)' }}>
                    Distribución por vencimiento — Top 10 proveedores
                  </p>
                  <NbStackedBarChart
                    categories={apData.data.slice(0, 10).map((r) => r.supplierName ?? r.supplierCode)}
                    series={[
                      { name: 'Al día',  data: apData.data.slice(0, 10).map((r) => Math.max(0, r.balanceDue - r.overdueAmount)), color: '#16A34A' },
                      { name: '1–30d',   data: apData.data.slice(0, 10).map((r) => r.aging0To30),   color: '#D97706' },
                      { name: '31–60d',  data: apData.data.slice(0, 10).map((r) => r.aging31To60),  color: '#EA580C' },
                      { name: '+90d',    data: apData.data.slice(0, 10).map((r) => r.aging90Plus),  color: '#DC2626' },
                    ]}
                    horizontal={true}
                    height={280}
                    loading={loadingAp}
                    valueFormatter={(v) => v.toLocaleString('es-CL', { maximumFractionDigits: 0 })}
                  />
                  <div style={{ height: 1, background: 'var(--c-border)', margin: '12px 0 0' }} />
                </div>
              )}
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
            </>
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

        {/* ── Tendencia ───────────────────────────────────────────────────── */}
        {tab === 'tendencia' && (
          <div style={{ padding: 20 }}>
            <p style={{ fontSize: 13, color: 'var(--c-text-muted)', marginBottom: 16 }}>
              Evolución histórica de posición financiera neta (CxC − CxP)
            </p>
            {(execData ?? []).length > 0 ? (
              <>
                <NbLineChart
                  series={[
                    {
                      name: 'Posición neta (CxC − CxP)',
                      data: (execData ?? []).map((d) => ({
                        name: d.periodDate,
                        value: d.arTotal - (d.apTotal ?? 0),
                      })),
                      color: '#7C3AED',
                    },
                  ]}
                  height={280}
                  loading={loadingExec}
                  valueFormatter={(v) => v.toLocaleString('es-CL', { maximumFractionDigits: 0 })}
                />
                <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(200px, 1fr))', gap: 12, marginTop: 20 }}>
                  {(execData ?? []).slice(0, 12).map((d, i) => (
                    <div key={i} className="db-stat-card">
                      <span className="db-stat-label">{fmtDate(d.periodDate)}</span>
                      <span className="db-stat-value" style={{ fontSize: 18, fontVariantNumeric: 'tabular-nums' }}>
                        {(d.arTotal - (d.apTotal ?? 0)).toLocaleString('es-CL', { maximumFractionDigits: 0 })}
                      </span>
                      <span style={{ fontSize: 11.5, color: 'var(--c-text-faint)' }}>
                        CxC {d.arTotal.toLocaleString('es-CL', { maximumFractionDigits: 0 })} · CxP {(d.apTotal ?? 0).toLocaleString('es-CL', { maximumFractionDigits: 0 })}
                      </span>
                    </div>
                  ))}
                </div>
              </>
            ) : (
              <NbEmptyState message="Sin datos de tendencia disponibles." icon="chart" />
            )}
          </div>
        )}

        {/* ── Estado de Resultados ─────────────────────────────────────────── */}
        {tab === 'resultados' && (
          !isData || isData.length === 0 ? (
            <FinancialDataPending
              title="Estado de Resultados"
              description="Requiere extracción y transformación del libro diario SAP B1. Ejecutar: dotnet run -- --object OACT --send && --object OJDT --send, luego SELECT * FROM mart.refresh_accounting_all('company-id')."
              requiredTables={[
                'mart.income_statement_summary — Vacía: ejecutar refresh_accounting_all()',
                'cfg.account_classification_rules — Poblar con clasificación por empresa',
              ]}
            />
          ) : (
            <div style={{ padding: '16px 20px' }}>
              {/* KPI Row */}
              {(() => {
                const latest = isData[isData.length - 1]
                return (
                  <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(160px, 1fr))', gap: 12, marginBottom: 20 }}>
                    {[
                      { label: 'Ingresos', value: fmtAmt(latest.revenue), highlight: false },
                      { label: 'Utilidad bruta', value: fmtAmt(latest.grossProfit), sub: `${latest.grossProfitPct.toFixed(1)}% margen`, highlight: false },
                      { label: 'EBIT', value: fmtAmt(latest.operatingIncome), sub: `${latest.operatingPct.toFixed(1)}% margen`, highlight: false },
                      { label: 'Utilidad neta', value: fmtAmt(latest.netIncome), sub: `${latest.netPct.toFixed(1)}% margen`, highlight: latest.netIncome < 0 },
                    ].map((k) => (
                      <div key={k.label} className="db-stat-card">
                        <span className="db-stat-label">{k.label}</span>
                        <span className="db-stat-value" style={{ fontSize: 18, fontVariantNumeric: 'tabular-nums', color: k.highlight ? '#DC2626' : 'inherit' }}>
                          {k.value}
                        </span>
                        {k.sub && <span style={{ fontSize: 11.5, color: 'var(--c-text-faint)' }}>{k.sub}</span>}
                      </div>
                    ))}
                  </div>
                )
              })()}
              {/* Trend chart */}
              {isData.length > 1 && (
                <div style={{ marginBottom: 20 }}>
                  <p style={{ fontSize: 13, fontWeight: 600, marginBottom: 8, color: 'var(--c-text)' }}>Tendencia mensual</p>
                  <NbAreaChart
                    series={[
                      { name: 'Ingresos',       data: isData.map((d) => ({ name: `${d.periodYear}-${String(d.periodMonth).padStart(2,'0')}`, value: d.revenue })),        color: '#2563EB' },
                      { name: 'Costo de ventas',data: isData.map((d) => ({ name: `${d.periodYear}-${String(d.periodMonth).padStart(2,'0')}`, value: d.cogs })),           color: '#DC2626' },
                      { name: 'Utilidad neta',  data: isData.map((d) => ({ name: `${d.periodYear}-${String(d.periodMonth).padStart(2,'0')}`, value: d.netIncome })),     color: '#16A34A' },
                    ]}
                    height={200}
                    loading={loadingIs}
                    valueFormatter={(v) => v.toLocaleString('es-CL', { maximumFractionDigits: 0 })}
                  />
                </div>
              )}
              {/* P&L detail table for latest period */}
              <p style={{ fontSize: 13, fontWeight: 600, marginBottom: 8, color: 'var(--c-text)' }}>
                Detalle — {isData[isData.length - 1].periodYear}/{String(isData[isData.length - 1].periodMonth).padStart(2,'0')}
              </p>
              <div className="nb-table-scroll">
                <table className="db-table" style={{ fontSize: 13 }}>
                  <thead>
                    <tr><th>Línea</th><th style={{ textAlign: 'right' }}>Monto</th><th style={{ textAlign: 'right' }}>% Ingreso</th></tr>
                  </thead>
                  <tbody>
                    {[
                      { key: 'revenue',      label: 'Ingresos',             bold: true  },
                      { key: 'cogs',         label: 'Costo de ventas',      bold: false },
                      { key: 'gross_profit', label: 'Utilidad bruta',       bold: true, computed: true },
                      { key: 'opex',         label: 'Gastos operacionales', bold: false },
                      { key: 'operating',    label: 'EBIT',                 bold: true, computed: true },
                      { key: 'financial',    label: 'Res. financiero',      bold: false },
                      { key: 'tax',          label: 'Impuesto',             bold: false },
                      { key: 'net',          label: 'Utilidad neta',        bold: true, computed: true },
                    ].map(({ key, label, bold, computed }) => {
                      const d = isData[isData.length - 1]
                      const amounts: Record<string, number> = {
                        revenue: d.revenue, cogs: d.cogs, gross_profit: d.grossProfit,
                        opex: d.opex, operating: d.operatingIncome,
                        financial: d.financial, tax: d.tax, net: d.netIncome,
                      }
                      const pcts: Record<string, number> = {
                        revenue: 100, cogs: d.grossProfitPct > 0 ? 100 - d.grossProfitPct : 0,
                        gross_profit: d.grossProfitPct, opex: d.revenue > 0 ? d.opex / d.revenue * 100 : 0,
                        operating: d.operatingPct, financial: d.revenue > 0 ? d.financial / d.revenue * 100 : 0,
                        tax: d.revenue > 0 ? d.tax / d.revenue * 100 : 0, net: d.netPct,
                      }
                      const amt = amounts[key] ?? 0
                      return (
                        <tr key={key} style={{ background: computed ? '#F8FAFC' : undefined }}>
                          <td style={{ fontWeight: bold ? 600 : 400, paddingLeft: computed ? 8 : 24 }}>{label}</td>
                          <td style={{ textAlign: 'right', fontVariantNumeric: 'tabular-nums', fontWeight: bold ? 600 : 400, color: amt < 0 ? '#DC2626' : 'inherit' }}>
                            {fmtAmt(amt)}
                          </td>
                          <td style={{ textAlign: 'right', fontVariantNumeric: 'tabular-nums', color: 'var(--c-text-muted)', fontSize: 12.5 }}>
                            {(pcts[key] ?? 0).toFixed(1)}%
                          </td>
                        </tr>
                      )
                    })}
                  </tbody>
                </table>
              </div>
            </div>
          )
        )}

        {/* ── Balance General ──────────────────────────────────────────────── */}
        {tab === 'balance' && (
          !bsData || bsData.length === 0 ? (
            <FinancialDataPending
              title="Balance General"
              description="Requiere extracción y transformación del libro diario SAP B1, más clasificación de cuentas en cfg.account_classification_rules con statement_line de balance sheet."
              requiredTables={[
                'mart.balance_sheet_summary — Vacía: ejecutar refresh_accounting_all()',
                'cfg.account_classification_rules — Debe incluir current_assets, current_liabilities, equity, etc.',
              ]}
            />
          ) : (
            <div style={{ padding: '16px 20px' }}>
              {(() => {
                const snap = bsData[0]
                const imbalanceOk = Math.abs(snap.imbalance) < 1
                return (
                  <>
                    {!imbalanceOk && (
                      <div style={{ padding: '10px 14px', backgroundColor: '#FEF2F2', border: '1px solid #FECACA', borderRadius: 6, fontSize: 13, color: '#991B1B', marginBottom: 16 }}>
                        Desequilibrio detectado: activos − (pasivos + patrimonio) = <strong>{fmtAmt(snap.imbalance)}</strong>. Revisar clasificación de cuentas.
                      </div>
                    )}
                    <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(180px, 1fr))', gap: 12, marginBottom: 20 }}>
                      {[
                        { label: 'Total activos',     value: fmtAmt(snap.totalAssets),      color: '#2563EB' },
                        { label: 'Total pasivos',     value: fmtAmt(snap.totalLiabilities), color: '#DC2626' },
                        { label: 'Patrimonio',        value: fmtAmt(snap.totalEquity),      color: '#16A34A' },
                        { label: 'Fecha corte',       value: fmtDate(snap.snapshotDate),    color: 'inherit'  },
                      ].map((k) => (
                        <div key={k.label} className="db-stat-card">
                          <span className="db-stat-label">{k.label}</span>
                          <span className="db-stat-value" style={{ fontSize: 17, fontVariantNumeric: 'tabular-nums', color: k.color }}>
                            {k.value}
                          </span>
                        </div>
                      ))}
                    </div>
                    <div className="nb-table-scroll">
                      <table className="db-table" style={{ fontSize: 13 }}>
                        <thead>
                          <tr><th>Categoría</th><th>Subcategoría</th><th style={{ textAlign: 'right' }}>Monto</th></tr>
                        </thead>
                        <tbody>
                          {snap.entries.map((e, i) => {
                            const catLabels: Record<string, string> = {
                              current_assets: 'Activo circulante', non_current_assets: 'Activo fijo',
                              current_liabilities: 'Pasivo circulante', non_current_liabilities: 'Pasivo largo plazo',
                              equity: 'Patrimonio', unclassified: 'Sin clasificar',
                            }
                            return (
                              <tr key={i}>
                                <td style={{ fontWeight: 500 }}>{catLabels[e.category] ?? e.category}</td>
                                <td style={{ fontSize: 12, color: 'var(--c-text-muted)' }}>{e.subCategory || '—'}</td>
                                <td style={{ textAlign: 'right', fontVariantNumeric: 'tabular-nums', color: e.amount < 0 ? '#DC2626' : 'inherit' }}>
                                  {fmtAmt(e.amount)}
                                </td>
                              </tr>
                            )
                          })}
                        </tbody>
                      </table>
                    </div>
                  </>
                )
              })()}
            </div>
          )
        )}

        {/* ── EBITDA / Rentabilidad ────────────────────────────────────────── */}
        {tab === 'ebitda' && (
          !ebData || ebData.length === 0 ? (
            <FinancialDataPending
              title="EBITDA / Rentabilidad"
              description="Requiere extracción del libro diario SAP B1 y clasificación de cuentas de resultado en cfg.account_classification_rules (revenue, cogs, opex, financial, tax)."
              requiredTables={[
                'mart.ebitda_summary — Vacía: ejecutar refresh_accounting_all()',
                'cfg.account_classification_rules — Poblar con statement_line revenue/cogs/opex',
              ]}
            />
          ) : (
            <div style={{ padding: '16px 20px' }}>
              {(() => {
                const latest = ebData[ebData.length - 1]
                return (
                  <>
                    <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(150px, 1fr))', gap: 12, marginBottom: 20 }}>
                      {[
                        { label: 'Ingresos',       value: fmtAmt(latest.revenue),      sub: '' },
                        { label: 'Margen bruto',   value: fmtAmt(latest.grossProfit),  sub: `${latest.ebitdaMargin.toFixed(1)}% EBITDA` },
                        { label: 'EBITDA',         value: fmtAmt(latest.ebitda),       sub: `${latest.ebitdaMargin.toFixed(1)}% margen` },
                        { label: 'Utilidad neta',  value: fmtAmt(latest.netIncome),    sub: `${latest.netMargin.toFixed(1)}% margen` },
                      ].map((k) => (
                        <div key={k.label} className="db-stat-card">
                          <span className="db-stat-label">{k.label}</span>
                          <span className="db-stat-value" style={{ fontSize: 17, fontVariantNumeric: 'tabular-nums' }}>{k.value}</span>
                          {k.sub && <span style={{ fontSize: 11.5, color: 'var(--c-text-faint)' }}>{k.sub}</span>}
                        </div>
                      ))}
                    </div>
                    {ebData.length > 1 && (
                      <div style={{ marginBottom: 20 }}>
                        <p style={{ fontSize: 13, fontWeight: 600, marginBottom: 8, color: 'var(--c-text)' }}>Evolución EBITDA y Utilidad neta</p>
                        <NbLineChart
                          series={[
                            { name: 'EBITDA',        data: ebData.map((d) => ({ name: `${d.periodYear}-${String(d.periodMonth).padStart(2,'0')}`, value: d.ebitda })),    color: '#2563EB' },
                            { name: 'Utilidad neta', data: ebData.map((d) => ({ name: `${d.periodYear}-${String(d.periodMonth).padStart(2,'0')}`, value: d.netIncome })), color: '#16A34A' },
                          ]}
                          height={220}
                          loading={loadingEb}
                          valueFormatter={(v) => v.toLocaleString('es-CL', { maximumFractionDigits: 0 })}
                        />
                      </div>
                    )}
                    <div className="nb-table-scroll">
                      <table className="db-table" style={{ fontSize: 12.5 }}>
                        <thead>
                          <tr>
                            <th>Período</th><th style={{ textAlign: 'right' }}>Ingresos</th>
                            <th style={{ textAlign: 'right' }}>EBITDA</th><th style={{ textAlign: 'right' }}>% EBITDA</th>
                            <th style={{ textAlign: 'right' }}>Ut. neta</th><th style={{ textAlign: 'right' }}>% Neto</th>
                          </tr>
                        </thead>
                        <tbody>
                          {[...ebData].reverse().map((d, i) => (
                            <tr key={i}>
                              <td style={{ fontVariantNumeric: 'tabular-nums' }}>{d.periodYear}/{String(d.periodMonth).padStart(2,'0')}</td>
                              <td style={{ textAlign: 'right', fontVariantNumeric: 'tabular-nums' }}>{fmtAmt(d.revenue)}</td>
                              <td style={{ textAlign: 'right', fontVariantNumeric: 'tabular-nums', color: d.ebitda < 0 ? '#DC2626' : 'inherit' }}>{fmtAmt(d.ebitda)}</td>
                              <td style={{ textAlign: 'right', fontVariantNumeric: 'tabular-nums' }}>{fmtPct(d.ebitdaMargin)}</td>
                              <td style={{ textAlign: 'right', fontVariantNumeric: 'tabular-nums', color: d.netIncome < 0 ? '#DC2626' : 'inherit' }}>{fmtAmt(d.netIncome)}</td>
                              <td style={{ textAlign: 'right', fontVariantNumeric: 'tabular-nums' }}>{fmtPct(d.netMargin)}</td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  </>
                )
              })()}
            </div>
          )
        )}

        {/* ── Plan de Cuentas ──────────────────────────────────────────────── */}
        {tab === 'cuentas' && (
          !coaData || coaData.length === 0 ? (
            <FinancialDataPending
              title="Plan de Cuentas"
              description="Requiere extracción del maestro de cuentas SAP B1 (OACT). Ejecutar: dotnet run -- --object OACT --send, luego SELECT * FROM mart.refresh_accounting_all('company-id')."
              requiredTables={[
                'mart.gl_accounts — Vacía: ejecutar refresh_accounting_all()',
                'mart.account_balances — Vacía: ejecutar refresh_accounting_all()',
              ]}
            />
          ) : (
            <div style={{ padding: '8px 0' }}>
              <div style={{ padding: '8px 20px 0', display: 'flex', gap: 16, alignItems: 'center', flexWrap: 'wrap', borderBottom: '1px solid var(--c-border)', paddingBottom: 12, marginBottom: 0 }}>
                <span style={{ fontSize: 13, color: 'var(--c-text-muted)' }}>
                  {coaData.length} cuentas — saldos acumulados (débitos − créditos)
                </span>
              </div>
              <div className="nb-table-scroll">
                <table className="db-table" style={{ fontSize: 12.5 }}>
                  <thead>
                    <tr>
                      <th>Código</th><th>Nombre</th><th>Tipo</th><th>Clasificación</th>
                      <th style={{ textAlign: 'right' }}>Saldo</th>
                    </tr>
                  </thead>
                  <tbody>
                    {coaData.map((a) => {
                      const stLabels: Record<string, string> = {
                        revenue: 'Ingreso', cogs: 'Costo', opex: 'Gasto op.',
                        current_assets: 'Act. cir.', non_current_assets: 'Act. fijo',
                        current_liabilities: 'Pas. cir.', non_current_liabilities: 'Pas. L/P',
                        equity: 'Patrimonio', financial: 'Financiero', tax: 'Impuesto',
                        unclassified: '—',
                      }
                      const indent = (a.level ?? 0) * 12
                      return (
                        <tr key={a.code}>
                          <td style={{ fontFamily: 'monospace', fontSize: 12, paddingLeft: 20 + indent }}>{a.code}</td>
                          <td style={{ paddingLeft: indent, maxWidth: 260, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                            {a.name ?? '—'}
                          </td>
                          <td style={{ fontSize: 12, color: 'var(--c-text-muted)' }}>{a.accountType ?? '—'}</td>
                          <td>
                            {a.statementLine && (
                              <span style={{ padding: '2px 7px', borderRadius: 4, backgroundColor: '#EFF6FF', color: '#1D4ED8', fontSize: 11.5, fontWeight: 500 }}>
                                {stLabels[a.statementLine] ?? a.statementLine}
                              </span>
                            )}
                          </td>
                          <td style={{ textAlign: 'right', fontVariantNumeric: 'tabular-nums', color: a.balance < 0 ? '#DC2626' : 'inherit' }}>
                            {fmtAmt(a.balance)}
                          </td>
                        </tr>
                      )
                    })}
                  </tbody>
                </table>
              </div>
            </div>
          )
        )}

        {/* ── Validaciones ────────────────────────────────────────────────── */}
        {tab === 'validaciones' && (
          loadingVal ? (
            <div style={{ padding: 40, textAlign: 'center', color: 'var(--c-text-muted)', fontSize: 13 }}>Cargando validaciones…</div>
          ) : valError || !valData ? (
            <div style={{ padding: 40, textAlign: 'center' }}>
              <p style={{ fontSize: 13.5, color: 'var(--c-text-muted)', marginBottom: 8 }}>No se pudieron cargar las validaciones.</p>
              <p style={{ fontSize: 12, color: 'var(--c-text-faint)' }}>Verifique que la base de datos de staging está disponible y que la empresa tiene datos contables.</p>
            </div>
          ) : (
            <div style={{ padding: '16px 20px' }}>
              {/* Health Score KPI Row */}
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(160px, 1fr))', gap: 12, marginBottom: 20 }}>
                {[
                  {
                    label: 'Health Score',
                    value: `${valData.healthScore}/100`,
                    color: valData.healthScore >= 80 ? '#16A34A' : valData.healthScore >= 50 ? '#D97706' : '#DC2626',
                    sub: valData.healthStatus === 'ok' ? 'Sin problemas críticos' : valData.healthStatus === 'warning' ? 'Requiere revisión' : 'Atención inmediata',
                  },
                  { label: 'Issues críticos',   value: String(valData.criticalIssues), color: valData.criticalIssues > 0 ? '#DC2626' : '#16A34A', sub: '' },
                  { label: 'Issues warning',    value: String(valData.warningIssues),  color: valData.warningIssues  > 0 ? '#D97706' : '#16A34A', sub: '' },
                  { label: 'Cuentas sin clas.', value: String(valData.unclassifiedAccounts), color: valData.unclassifiedAccounts > 0 ? '#D97706' : '#16A34A', sub: 'Postables' },
                ].map((k) => (
                  <div key={k.label} className="db-stat-card">
                    <span className="db-stat-label">{k.label}</span>
                    <span className="db-stat-value" style={{ fontSize: 22, fontVariantNumeric: 'tabular-nums', color: k.color }}>{k.value}</span>
                    {k.sub && <span style={{ fontSize: 11.5, color: 'var(--c-text-faint)' }}>{k.sub}</span>}
                  </div>
                ))}
              </div>

              {/* Last period + orphan info */}
              <div style={{ display: 'flex', gap: 16, marginBottom: 20, flexWrap: 'wrap', fontSize: 13, color: 'var(--c-text-muted)' }}>
                <span>Último período: <strong style={{ color: 'var(--c-text)' }}>{valData.lastPeriodValidated ?? 'Sin datos'}</strong></span>
                <span>·</span>
                <span>Líneas huérfanas: <strong style={{ color: valData.orphanJournalLines > 100 ? '#D97706' : 'var(--c-text)' }}>{valData.orphanJournalLines}</strong></span>
              </div>

              {/* Issues list */}
              {valData.issues.length === 0 ? (
                <div style={{ padding: '24px', backgroundColor: '#F0FDF4', border: '1px solid #BBF7D0', borderRadius: 8, marginBottom: 20 }}>
                  <p style={{ fontSize: 14, fontWeight: 600, color: '#15803D', marginBottom: 4 }}>Sin issues detectados</p>
                  <p style={{ fontSize: 13, color: '#166534' }}>Los datos contables superan todas las validaciones automáticas.</p>
                </div>
              ) : (
                <div style={{ display: 'flex', flexDirection: 'column', gap: 10, marginBottom: 24 }}>
                  {valData.issues.map((issue, i) => {
                    const sevColors: Record<string, { bg: string; border: string; text: string; badge: string }> = {
                      critical: { bg: '#FEF2F2', border: '#FECACA', text: '#991B1B', badge: '#DC2626' },
                      warning:  { bg: '#FFFBEB', border: '#FDE68A', text: '#92400E', badge: '#D97706' },
                      info:     { bg: '#EFF6FF', border: '#BFDBFE', text: '#1E40AF', badge: '#2563EB' },
                    }
                    const s = sevColors[issue.severity] ?? sevColors.info
                    return (
                      <div key={i} style={{ backgroundColor: s.bg, border: `1px solid ${s.border}`, borderRadius: 8, padding: '12px 16px' }}>
                        <div style={{ display: 'flex', alignItems: 'center', gap: 10, marginBottom: 4 }}>
                          <span style={{ display: 'inline-block', padding: '1px 8px', borderRadius: 4, backgroundColor: s.badge, color: '#fff', fontSize: 11, fontWeight: 700, textTransform: 'uppercase' }}>
                            {issue.severity}
                          </span>
                          <span style={{ fontSize: 13.5, fontWeight: 600, color: s.text }}>{issue.title}</span>
                          {issue.count > 1 && (
                            <span style={{ marginLeft: 'auto', fontSize: 12, color: s.text, fontVariantNumeric: 'tabular-nums' }}>({issue.count})</span>
                          )}
                        </div>
                        <p style={{ fontSize: 13, color: s.text, margin: 0 }}>{issue.description}</p>
                        {issue.period && (
                          <p style={{ fontSize: 12, color: s.text, opacity: 0.7, marginTop: 4 }}>Período: {issue.period}</p>
                        )}
                      </div>
                    )
                  })}
                </div>
              )}

              {/* Balance reconciliation */}
              {valData.reconciliation && (
                <div style={{ border: '1px solid var(--c-border)', borderRadius: 8, overflow: 'hidden' }}>
                  <div style={{ padding: '10px 16px', backgroundColor: 'var(--c-surface-subtle, #F8FAFC)', borderBottom: '1px solid var(--c-border)', display: 'flex', alignItems: 'center', gap: 12 }}>
                    <span style={{ fontSize: 13, fontWeight: 600, color: 'var(--c-text)' }}>
                      Conciliación del Balance — {valData.reconciliation.snapshotDate ?? 'Sin fecha'}
                    </span>
                    <span style={{ fontSize: 12, padding: '2px 8px', borderRadius: 4,
                      backgroundColor: valData.reconciliation.isBalanced ? '#F0FDF4' : '#FEF2F2',
                      color: valData.reconciliation.isBalanced ? '#15803D' : '#991B1B',
                      fontWeight: 600,
                    }}>
                      {valData.reconciliation.isBalanced ? 'Cuadra' : 'Desbalance'}
                    </span>
                  </div>
                  <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 0 }}>
                    {[
                      { label: 'Total Activos',   value: valData.reconciliation.totalAssets },
                      { label: 'Total Pasivos',    value: valData.reconciliation.totalLiabilities },
                      { label: 'Patrimonio',       value: valData.reconciliation.totalEquity },
                      { label: 'Desbalance',       value: valData.reconciliation.imbalance, highlight: valData.reconciliation.imbalance > 0.01 },
                    ].map((k, i) => (
                      <div key={i} style={{ padding: '12px 16px', borderRight: i < 3 ? '1px solid var(--c-border)' : 'none' }}>
                        <div style={{ fontSize: 11.5, color: 'var(--c-text-muted)', marginBottom: 4 }}>{k.label}</div>
                        <div style={{ fontSize: 16, fontWeight: 600, fontVariantNumeric: 'tabular-nums', color: k.highlight ? '#DC2626' : 'var(--c-text)' }}>
                          {fmtAmt(k.value)}
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
              )}
            </div>
          )
        )}

      </div>
    </div>
  )
}
