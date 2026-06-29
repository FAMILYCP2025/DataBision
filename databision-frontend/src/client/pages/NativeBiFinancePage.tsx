import { useState, useMemo, type ReactNode } from 'react'
import { useSearchParams } from 'react-router-dom'
import { Download } from 'lucide-react'
import { exportXlsx } from '../utils/exportXlsx'
import { useDateRangeFilter, filterByRange, shiftRangeByYear } from '../hooks/useDateRangeFilter'
import DateRangeSelector from '../components/nativebi/DateRangeSelector'
import SortableTable, { type ColumnDef } from '../components/nativebi/SortableTable'
import NativeBiPageHeader from '../components/nativebi/NativeBiPageHeader'
import { NbEmptyState } from '../components/nativebi/NativeBiState'
import { NbAreaChart } from '../components/charts'
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

const VALID_TABS: Tab[] = ['resumen', 'ar-aging', 'ap-aging', 'tendencia']
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

function BucketCell({ value }: { value: number }) {
  if (value <= 0) return <span style={{ color: 'var(--c-text-faint)' }}>—</span>
  return <span style={{ color: '#DC2626', fontVariantNumeric: 'tabular-nums' }}>{fmtAmt(value)}</span>
}

export default function NativeBiFinancePage() {
  const [searchParams] = useSearchParams()
  const initialTab = searchParams.get('tab') as Tab | null
  const [tab, setTab] = useState<Tab>(
    initialTab && (VALID_TABS as string[]).includes(initialTab)
      ? (initialTab as Tab)
      : DEFAULT_TAB
  )
  const { range, setFrom, setTo } = useDateRangeFilter(12)
  const [yoyEnabled, setYoyEnabled] = useState(false)

  const [arSort, setArSort] = useState<{ key: string; dir: 'asc' | 'desc' }>({ key: 'totalOpen', dir: 'desc' })
  const [apSort, setApSort] = useState<{ key: string; dir: 'asc' | 'desc' }>({ key: 'totalOpen', dir: 'desc' })

  const { data: summary,    isLoading: loadingSummary }   = useFinanceMartSummary()
  const { data: arAging,    isLoading: loadingAr }        = useFinanceMartArAging(100)
  const { data: apAging,    isLoading: loadingAp }        = useFinanceMartApAging(100)
  const { data: periodKpi,  isLoading: loadingPeriod }    = useFinanceMartPeriodKpi(24)

  // ── Derived ────────────────────────────────────────────────────────────────

  const filteredPeriod = useMemo(
    () => filterByRange(periodKpi ?? [], range),
    [periodKpi, range],
  )

  const prevYearRange = useMemo(() => shiftRangeByYear(range), [range])

  const filteredPrevYear = useMemo(
    () => yoyEnabled ? filterByRange(periodKpi ?? [], prevYearRange) : [],
    [yoyEnabled, periodKpi, prevYearRange],
  )

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
          <div style={{ display: 'flex', justifyContent: 'flex-end', padding: '8px 12px' }}>
            <button
              onClick={() => exportXlsx('finanzas-ar-aging', 'AR Aging', sortedAr.map(r => ({
                'Código': r.cardCode,
                'Cliente': r.cardName,
                'Corriente': r.currentAmount,
                '1–30 días': r.bucket1To30,
                '31–60 días': r.bucket31To60,
                '61–90 días': r.bucket61To90,
                '91–120 días': r.bucket91To120,
                '+120 días': r.bucketOver120,
                'Total': r.totalOpen,
                'Facturas': r.invoiceCount,
                'Más antigua': r.oldestDueDate,
              })))}
              disabled={sortedAr.length === 0}
              style={{ display: 'inline-flex', alignItems: 'center', gap: 6, padding: '6px 14px', borderRadius: 6, border: '1px solid var(--c-border)', background: '#fff', color: 'var(--c-text)', fontSize: 13, fontWeight: 500, cursor: 'pointer', fontFamily: 'inherit' }}
            >
              <Download size={14} />
              Exportar Excel
            </button>
          </div>
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
          <div style={{ display: 'flex', justifyContent: 'flex-end', padding: '8px 12px' }}>
            <button
              onClick={() => exportXlsx('finanzas-ap-aging', 'AP Aging', sortedAp.map(r => ({
                'Código': r.cardCode,
                'Proveedor': r.cardName,
                'Corriente': r.currentAmount,
                '1–30 días': r.bucket1To30,
                '31–60 días': r.bucket31To60,
                '61–90 días': r.bucket61To90,
                '91–120 días': r.bucket91To120,
                '+120 días': r.bucketOver120,
                'Total': r.totalOpen,
                'Facturas': r.invoiceCount,
                'Más antigua': r.oldestDueDate,
              })))}
              disabled={sortedAp.length === 0}
              style={{ display: 'inline-flex', alignItems: 'center', gap: 6, padding: '6px 14px', borderRadius: 6, border: '1px solid var(--c-border)', background: '#fff', color: 'var(--c-text)', fontSize: 13, fontWeight: 500, cursor: 'pointer', fontFamily: 'inherit' }}
            >
              <Download size={14} />
              Exportar Excel
            </button>
          </div>
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
          <div style={{ display: 'flex', alignItems: 'center', gap: 16, flexWrap: 'wrap' }}>
            <DateRangeSelector range={range} onFromChange={setFrom} onToChange={setTo} />
            <label style={{ display: 'flex', alignItems: 'center', gap: 6, cursor: 'pointer', fontSize: 13, color: 'var(--c-text-muted)' }}>
              <input
                type="checkbox"
                checked={yoyEnabled}
                onChange={e => setYoyEnabled(e.target.checked)}
                style={{ accentColor: 'var(--brand-primary)', width: 14, height: 14 }}
              />
              vs año anterior
            </label>
          </div>

          <div className="db-card" style={{ padding: 20 }}>
            <h3 style={{ fontSize: 13.5, fontWeight: 600, margin: '0 0 16px', color: 'var(--c-text)' }}>
              AR Neto vs. AP Neto por mes
            </h3>
            {loadingPeriod ? (
              <div className="cp-skeleton" style={{ height: 220, borderRadius: 6 }} />
            ) : filteredPeriod.length === 0 ? (
              <NbEmptyState message="Sin datos para el período seleccionado." icon="chart" />
            ) : (
              <NbAreaChart
                series={[
                  { name: `AR Neto ${range.fromYear}–${range.toYear}`,
                    data: filteredPeriod.map(r => ({ name: `${r.year}-${String(r.month).padStart(2, '0')}`, value: r.arNet })) },
                  { name: `AP Neto ${range.fromYear}–${range.toYear}`,
                    data: filteredPeriod.map(r => ({ name: `${r.year}-${String(r.month).padStart(2, '0')}`, value: r.apNet })) },
                  ...(yoyEnabled && filteredPrevYear.length > 0 ? [
                    { name: `AR Neto ${range.fromYear - 1}–${range.toYear - 1}`,
                      data: filteredPrevYear.map(r => ({ name: `${r.year}-${String(r.month).padStart(2, '0')}`, value: r.arNet })),
                      color: '#94A3B8' },
                    { name: `AP Neto ${range.fromYear - 1}–${range.toYear - 1}`,
                      data: filteredPrevYear.map(r => ({ name: `${r.year}-${String(r.month).padStart(2, '0')}`, value: r.apNet })),
                      color: '#CBD5E1' },
                  ] : []),
                ]}
                height={220}
              />
            )}
          </div>

          <div className="db-card" style={{ overflow: 'hidden' }}>
            {loadingPeriod ? (
              <div className="cp-skeleton" style={{ height: 200, margin: 16, borderRadius: 6 }} />
            ) : filteredPeriod.length === 0 ? (
              <NbEmptyState message="Sin datos de período disponibles." icon="table" />
            ) : (
              <SortableTable<FinancePeriodKpi>
                data={filteredPeriod}
                columns={periodCols}
                meta={fakeMeta(filteredPeriod.length)}
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
