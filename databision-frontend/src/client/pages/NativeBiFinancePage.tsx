import { useState, useMemo, type ReactNode } from 'react'
import SortableTable, { type ColumnDef } from '../components/nativebi/SortableTable'
import NativeBiPageHeader from '../components/nativebi/NativeBiPageHeader'
import { NbEmptyState } from '../components/nativebi/NativeBiState'
import { NbAreaChart } from '../components/charts'
import type { ChartDataPoint } from '../components/charts'
import {
  useFinanceMartSummary,
  useFinanceMartArAging,
  useFinanceMartApAging,
  useFinanceMartPeriodKpi,
} from '../hooks/useNativeBiFinance'
import type {
  ArAgingRow,
  ApAgingRow,
  FinancePeriodKpi,
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

type Tab = 'resumen' | 'ar-aging' | 'ap-aging' | 'tendencia'

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

function BucketCell({ value }: { value: number }) {
  if (value <= 0) return <span style={{ color: 'var(--c-text-faint)' }}>—</span>
  return <span style={{ color: '#DC2626', fontVariantNumeric: 'tabular-nums' }}>{fmtAmt(value)}</span>
}

export default function NativeBiFinancePage() {
  const [tab, setTab] = useState<Tab>('resumen')
  const [periodMonths, setPeriodMonths] = useState(12)

  const [arSort, setArSort] = useState<{ key: string; dir: 'asc' | 'desc' }>({ key: 'totalOpen', dir: 'desc' })
  const [apSort, setApSort] = useState<{ key: string; dir: 'asc' | 'desc' }>({ key: 'totalOpen', dir: 'desc' })

  const { data: summary,    isLoading: loadingSummary }   = useFinanceMartSummary()
  const { data: arAging,    isLoading: loadingAr }        = useFinanceMartArAging(100)
  const { data: apAging,    isLoading: loadingAp }        = useFinanceMartApAging(100)
  const { data: periodKpi,  isLoading: loadingPeriod }    = useFinanceMartPeriodKpi(periodMonths)

  // ── Derived ────────────────────────────────────────────────────────────────

  const arChartData: ChartDataPoint[] = (periodKpi ?? []).map(p => ({
    name: `${p.year}-${String(p.month).padStart(2, '0')}`,
    value: p.arNet,
  }))

  const apChartData: ChartDataPoint[] = (periodKpi ?? []).map(p => ({
    name: `${p.year}-${String(p.month).padStart(2, '0')}`,
    value: p.apNet,
  }))

  const sortedAr = useMemo(
    () => localSort(arAging ?? [], arSort.key, arSort.dir),
    [arAging, arSort],
  )
  const sortedAp = useMemo(
    () => localSort(apAging ?? [], apSort.key, apSort.dir),
    [apAging, apSort],
  )

  // ── Columns ────────────────────────────────────────────────────────────────

  const arCols: ColumnDef<ArAgingRow>[] = [
    { key: 'cardName',      label: 'Cliente',        render: r => <span style={{ fontWeight: 500 }}>{r.cardName ?? r.cardCode}</span> },
    { key: 'currentAmount', label: 'Corriente',      sortKey: 'currentAmount', align: 'right', render: r => fmtAmt(r.currentAmount) },
    { key: 'bucket1To30',   label: '1–30 días',      sortKey: 'bucket1To30',   align: 'right', render: r => <BucketCell value={r.bucket1To30} /> },
    { key: 'bucket31To60',  label: '31–60 días',     sortKey: 'bucket31To60',  align: 'right', render: r => <BucketCell value={r.bucket31To60} /> },
    { key: 'bucket61To90',  label: '61–90 días',     sortKey: 'bucket61To90',  align: 'right', render: r => <BucketCell value={r.bucket61To90} /> },
    { key: 'bucket91To120', label: '91–120 días',    sortKey: 'bucket91To120', align: 'right', render: r => <BucketCell value={r.bucket91To120} /> },
    { key: 'bucketOver120', label: '+120 días',      sortKey: 'bucketOver120', align: 'right', render: r => <BucketCell value={r.bucketOver120} /> },
    { key: 'totalOpen',     label: 'Total',          sortKey: 'totalOpen',     align: 'right', render: r => <strong>{fmtAmt(r.totalOpen)}</strong> },
    { key: 'invoiceCount',  label: 'Facturas',       sortKey: 'invoiceCount',  align: 'right', render: r => r.invoiceCount },
    { key: 'oldestDueDate', label: 'Más antigua',    render: r => fmtDate(r.oldestDueDate) },
  ]

  const apCols: ColumnDef<ApAgingRow>[] = [
    { key: 'cardName',      label: 'Proveedor',      render: r => <span style={{ fontWeight: 500 }}>{r.cardName ?? r.cardCode}</span> },
    { key: 'currentAmount', label: 'Corriente',      sortKey: 'currentAmount', align: 'right', render: r => fmtAmt(r.currentAmount) },
    { key: 'bucket1To30',   label: '1–30 días',      sortKey: 'bucket1To30',   align: 'right', render: r => <BucketCell value={r.bucket1To30} /> },
    { key: 'bucket31To60',  label: '31–60 días',     sortKey: 'bucket31To60',  align: 'right', render: r => <BucketCell value={r.bucket31To60} /> },
    { key: 'bucket61To90',  label: '61–90 días',     sortKey: 'bucket61To90',  align: 'right', render: r => <BucketCell value={r.bucket61To90} /> },
    { key: 'bucket91To120', label: '91–120 días',    sortKey: 'bucket91To120', align: 'right', render: r => <BucketCell value={r.bucket91To120} /> },
    { key: 'bucketOver120', label: '+120 días',      sortKey: 'bucketOver120', align: 'right', render: r => <BucketCell value={r.bucketOver120} /> },
    { key: 'totalOpen',     label: 'Total',          sortKey: 'totalOpen',     align: 'right', render: r => <strong>{fmtAmt(r.totalOpen)}</strong> },
    { key: 'invoiceCount',  label: 'Facturas',       sortKey: 'invoiceCount',  align: 'right', render: r => r.invoiceCount },
    { key: 'oldestDueDate', label: 'Más antigua',    render: r => fmtDate(r.oldestDueDate) },
  ]

  const periodCols: ColumnDef<FinancePeriodKpi>[] = [
    { key: 'period',        label: 'Período',        render: r => `${r.year}-${String(r.month).padStart(2, '0')}` },
    { key: 'arBilled',      label: 'AR Facturado',   sortKey: 'arBilled',      align: 'right', render: r => fmtAmt(r.arBilled) },
    { key: 'arCreditMemo',  label: 'NC AR',          sortKey: 'arCreditMemo',  align: 'right', render: r => fmtAmt(r.arCreditMemo) },
    { key: 'arNet',         label: 'AR Neto',        sortKey: 'arNet',         align: 'right', render: r => <strong>{fmtAmt(r.arNet)}</strong> },
    { key: 'arInvoiceCount',label: 'Fact. AR',       sortKey: 'arInvoiceCount',align: 'right', render: r => r.arInvoiceCount },
    { key: 'apBilled',      label: 'AP Facturado',   sortKey: 'apBilled',      align: 'right', render: r => fmtAmt(r.apBilled) },
    { key: 'apCreditMemo',  label: 'NC AP',          sortKey: 'apCreditMemo',  align: 'right', render: r => fmtAmt(r.apCreditMemo) },
    { key: 'apNet',         label: 'AP Neto',        sortKey: 'apNet',         align: 'right', render: r => <strong>{fmtAmt(r.apNet)}</strong> },
    { key: 'apInvoiceCount',label: 'Fact. AP',       sortKey: 'apInvoiceCount',align: 'right', render: r => r.apInvoiceCount },
  ]

  return (
    <div className="nb-page">
      <NativeBiPageHeader
        title="Finanzas MART"
        description="AR Aging, AP Aging y KPIs financieros por período"
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
        <TabButton label="Resumen"    active={tab === 'resumen'}    onClick={() => setTab('resumen')} />
        <TabButton label="AR Aging"   active={tab === 'ar-aging'}   onClick={() => setTab('ar-aging')} />
        <TabButton label="AP Aging"   active={tab === 'ap-aging'}   onClick={() => setTab('ap-aging')} />
        <TabButton label="Tendencia"  active={tab === 'tendencia'}  onClick={() => setTab('tendencia')} />
      </div>

      {/* ── Tab: Resumen ────────────────────────────────────────────────────── */}
      {tab === 'resumen' && (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 20 }}>
          <p style={{ fontSize: 12.5, color: 'var(--c-text-muted)', margin: 0, fontWeight: 600, letterSpacing: '0.04em', textTransform: 'uppercase' }}>
            Cuentas por Cobrar (AR)
          </p>
          <div className="nb-stat-grid">
            <StatCard label="Total AR abierto"    value={`$${fmtAmt(summary?.totalOpenAr ?? 0)}`}    loading={loadingSummary} />
            <StatCard label="AR vencido"          value={`$${fmtAmt(summary?.totalOverdueAr ?? 0)}`}  loading={loadingSummary} />
            <StatCard label="DSO (días)"          value={summary?.dsoDays != null ? summary.dsoDays.toFixed(0) : '—'} loading={loadingSummary} sub="Days Sales Outstanding" />
            <StatCard label="Clientes con deuda"  value={summary?.arCustomerCount ?? 0}               loading={loadingSummary} />
          </div>

          <p style={{ fontSize: 12.5, color: 'var(--c-text-muted)', margin: '4px 0 0', fontWeight: 600, letterSpacing: '0.04em', textTransform: 'uppercase' }}>
            Cuentas por Pagar (AP)
          </p>
          <div className="nb-stat-grid">
            <StatCard label="Total AP abierto"       value={`$${fmtAmt(summary?.totalOpenAp ?? 0)}`}    loading={loadingSummary} />
            <StatCard label="AP vencido"             value={`$${fmtAmt(summary?.totalOverdueAp ?? 0)}`}  loading={loadingSummary} />
            <StatCard label="DPO (días)"             value={summary?.dpoDays != null ? summary.dpoDays.toFixed(0) : '—'} loading={loadingSummary} sub="Days Payable Outstanding" />
            <StatCard label="Proveedores con deuda"  value={summary?.apSupplierCount ?? 0}               loading={loadingSummary} />
          </div>
        </div>
      )}

      {/* ── Tab: AR Aging ───────────────────────────────────────────────────── */}
      {tab === 'ar-aging' && (
        <div className="db-card" style={{ overflow: 'hidden' }}>
          {loadingAr ? (
            <div className="cp-skeleton" style={{ height: 300, margin: 16, borderRadius: 6 }} />
          ) : sortedAr.length === 0 ? (
            <NbEmptyState message="Sin cuentas por cobrar abiertas." icon="table" />
          ) : (
            <SortableTable<ArAgingRow>
              data={sortedAr}
              columns={arCols}
              meta={fakeMeta(sortedAr.length)}
              sortBy={arSort.key}
              sortDir={arSort.dir}
              onPageChange={() => {}}
              onSortChange={(key, dir) => setArSort({ key, dir })}
              rowKey={r => r.cardCode}
            />
          )}
        </div>
      )}

      {/* ── Tab: AP Aging ───────────────────────────────────────────────────── */}
      {tab === 'ap-aging' && (
        <div className="db-card" style={{ overflow: 'hidden' }}>
          {loadingAp ? (
            <div className="cp-skeleton" style={{ height: 300, margin: 16, borderRadius: 6 }} />
          ) : sortedAp.length === 0 ? (
            <NbEmptyState message="Sin cuentas por pagar abiertas." icon="table" />
          ) : (
            <SortableTable<ApAgingRow>
              data={sortedAp}
              columns={apCols}
              meta={fakeMeta(sortedAp.length)}
              sortBy={apSort.key}
              sortDir={apSort.dir}
              onPageChange={() => {}}
              onSortChange={(key, dir) => setApSort({ key, dir })}
              rowKey={r => r.cardCode}
            />
          )}
        </div>
      )}

      {/* ── Tab: Tendencia ──────────────────────────────────────────────────── */}
      {tab === 'tendencia' && (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 20 }}>
          <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
            <span style={{ fontSize: 13, color: 'var(--c-text-muted)' }}>Período:</span>
            {([6, 12, 24] as const).map(m => (
              <button
                key={m}
                onClick={() => setPeriodMonths(m)}
                className={`db-btn db-btn--sm ${periodMonths === m ? 'db-btn--primary' : 'db-btn--ghost'}`}
              >
                {m} meses
              </button>
            ))}
          </div>

          <div className="db-card" style={{ padding: 20 }}>
            <h3 style={{ fontSize: 13.5, fontWeight: 600, margin: '0 0 16px', color: 'var(--c-text)' }}>
              AR Neto vs. AP Neto por mes
            </h3>
            {loadingPeriod ? (
              <div className="cp-skeleton" style={{ height: 220, borderRadius: 6 }} />
            ) : arChartData.length === 0 ? (
              <NbEmptyState message="Sin datos para el período seleccionado." icon="chart" />
            ) : (
              <NbAreaChart
                series={[
                  { name: 'AR Neto', data: arChartData },
                  { name: 'AP Neto', data: apChartData },
                ]}
                height={220}
              />
            )}
          </div>

          <div className="db-card" style={{ overflow: 'hidden' }}>
            {loadingPeriod ? (
              <div className="cp-skeleton" style={{ height: 200, margin: 16, borderRadius: 6 }} />
            ) : (periodKpi ?? []).length === 0 ? (
              <NbEmptyState message="Sin datos de período disponibles." icon="table" />
            ) : (
              <SortableTable<FinancePeriodKpi>
                data={periodKpi ?? []}
                columns={periodCols}
                meta={fakeMeta((periodKpi ?? []).length)}
                onPageChange={() => {}}
                onSortChange={() => {}}
                rowKey={r => `${r.year}-${r.month}`}
              />
            )}
          </div>
        </div>
      )}
    </div>
  )
}
