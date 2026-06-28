import { useMemo } from 'react'
import NativeBiPageHeader from '../components/nativebi/NativeBiPageHeader'
import { NbEmptyState, NbLoadingSkeleton } from '../components/nativebi/NativeBiState'
import { NbAreaChart } from '../components/charts'
import type { ChartDataPoint } from '../components/charts'
import { useSalesMartKpi } from '../hooks/useNativeBiSales'
import { usePurchaseMartKpi } from '../hooks/useNativeBiPurchase'
import { useInventoryMartKpi } from '../hooks/useNativeBiInventory'
import { useFinanceMartSummary, useFinanceMartPeriodKpi } from '../hooks/useNativeBiFinance'

// ── Formatters ────────────────────────────────────────────────────────────────

function fmtAmt(n: number | undefined | null): string {
  if (n == null) return '—'
  return new Intl.NumberFormat('es-CL', { style: 'currency', currency: 'CLP', maximumFractionDigits: 0 }).format(n)
}

function fmtNum(n: number | undefined | null): string {
  if (n == null) return '—'
  return n.toLocaleString('es-CL')
}

function fmtPct(n: number | undefined | null): string {
  if (n == null) return '—'
  return (n >= 0 ? '+' : '') + n.toFixed(1)
}

// ── AlertBanner ───────────────────────────────────────────────────────────────

interface AlertBannerProps {
  label: string
  value: string
  status: 'ok' | 'warning' | 'critical'
  sub?: string
}

const ALERT_STYLES: Record<AlertBannerProps['status'], { bg: string; border: string; color: string }> = {
  ok:       { bg: '#F0FDF4', border: '#16A34A', color: '#15803D' },
  warning:  { bg: '#FFFBEB', border: '#D97706', color: '#B45309' },
  critical: { bg: '#FEF2F2', border: '#DC2626', color: '#B91C1C' },
}

function AlertBanner({ label, value, status, sub }: AlertBannerProps) {
  const s = ALERT_STYLES[status]
  return (
    <div
      style={{
        background: s.bg,
        borderLeft: `4px solid ${s.border}`,
        borderRadius: 6,
        padding: '12px 16px',
        display: 'flex',
        flexDirection: 'column',
        gap: 2,
      }}
    >
      <span style={{ fontSize: 12, color: '#64748B', fontWeight: 500 }}>{label}</span>
      <span style={{ fontSize: 18, fontWeight: 700, color: s.color, fontVariantNumeric: 'tabular-nums' }}>
        {value}
      </span>
      {sub && <span style={{ fontSize: 11, color: '#94A3B8' }}>{sub}</span>}
    </div>
  )
}

// ── ModuleCard ────────────────────────────────────────────────────────────────

interface KpiRow {
  label: string
  value: string
  color?: string
}

interface ModuleCardProps {
  title: string
  icon: string
  rows: KpiRow[]
  loading?: boolean
  linkLabel?: string
  linkHref?: string
}

function ModuleCard({ title, icon, rows, loading, linkLabel, linkHref }: ModuleCardProps) {
  return (
    <div
      style={{
        border: '1px solid var(--c-border)',
        borderRadius: 8,
        padding: 20,
        background: '#fff',
        boxShadow: 'var(--shadow-sm)',
        display: 'flex',
        flexDirection: 'column',
        gap: 12,
      }}
    >
      <div style={{ fontSize: 13.5, fontWeight: 600, color: 'var(--c-text)', display: 'flex', alignItems: 'center', gap: 6 }}>
        <span>{icon}</span>
        <span>{title}</span>
      </div>

      {loading ? (
        <NbLoadingSkeleton rows={4} height={20} />
      ) : (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
          {rows.map((row, i) => (
            <div key={i} style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'baseline', gap: 8 }}>
              <span style={{ fontSize: 13, color: 'var(--c-text-muted)' }}>{row.label}</span>
              <span
                style={{
                  fontSize: 14,
                  fontWeight: 600,
                  fontVariantNumeric: 'tabular-nums',
                  color: row.color ?? 'var(--c-text)',
                }}
              >
                {row.value}
              </span>
            </div>
          ))}
        </div>
      )}

      {linkLabel && linkHref && (
        <a
          href={linkHref}
          style={{ fontSize: 13, color: 'var(--brand-primary)', textDecoration: 'none', marginTop: 'auto' }}
        >
          {linkLabel} →
        </a>
      )}
    </div>
  )
}

// ── Page ──────────────────────────────────────────────────────────────────────

export default function NativeBiMartOverviewPage() {
  const kpiSales     = useSalesMartKpi()
  const kpiPurchase  = usePurchaseMartKpi()
  const kpiInventory = useInventoryMartKpi()
  const summary      = useFinanceMartSummary()
  const periodKpi    = useFinanceMartPeriodKpi(12)

  // ── Alert status helpers ──────────────────────────────────────────────────

  const arOverdue    = summary.data?.totalOverdueAr ?? 0
  const arTotal      = summary.data?.totalOpenAr     ?? 0
  const apOverdue    = summary.data?.totalOverdueAp  ?? 0
  const apTotal      = summary.data?.totalOpenAp     ?? 0
  const itemsBelowMin      = kpiInventory.data?.itemsBelowMin      ?? 0
  const overdueOrders      = kpiPurchase.data?.overdueOrdersCount  ?? 0

  function arStatus(): AlertBannerProps['status'] {
    if (arOverdue === 0) return 'ok'
    if (arTotal > 0 && arOverdue >= arTotal * 0.3) return 'critical'
    return 'warning'
  }

  function apStatus(): AlertBannerProps['status'] {
    if (apOverdue === 0) return 'ok'
    if (apTotal > 0 && apOverdue >= apTotal * 0.3) return 'critical'
    return 'warning'
  }

  function belowMinStatus(): AlertBannerProps['status'] {
    if (itemsBelowMin === 0) return 'ok'
    if (itemsBelowMin > 5) return 'critical'
    return 'warning'
  }

  function overdueOrdersStatus(): AlertBannerProps['status'] {
    if (overdueOrders === 0) return 'ok'
    if (overdueOrders > 3) return 'critical'
    return 'warning'
  }

  // ── Chart data ────────────────────────────────────────────────────────────

  const arData = useMemo<ChartDataPoint[]>(
    () => (periodKpi.data ?? []).map(p => ({
      name: `${p.year}-${String(p.month).padStart(2, '0')}`,
      value: p.arNet,
    })),
    [periodKpi.data]
  )

  const apData = useMemo<ChartDataPoint[]>(
    () => (periodKpi.data ?? []).map(p => ({
      name: `${p.year}-${String(p.month).padStart(2, '0')}`,
      value: p.apNet,
    })),
    [periodKpi.data]
  )

  // ── Ventas rows ───────────────────────────────────────────────────────────

  const growthPctSales = kpiSales.data?.growthPct
  const salesRows: KpiRow[] = [
    { label: 'Ventas LTM',   value: fmtAmt(kpiSales.data?.netSalesLtm) },
    {
      label: 'Crecimiento',
      value: kpiSales.data == null ? '—' : `${fmtPct(growthPctSales)}%`,
      color: growthPctSales == null ? undefined
        : growthPctSales > 0 ? '#16A34A'
        : growthPctSales < 0 ? '#DC2626'
        : 'inherit',
    },
    { label: 'Ticket prom.',  value: fmtAmt(kpiSales.data?.avgTicketLtm) },
    { label: 'OV abiertas',   value: fmtNum(kpiSales.data?.openOrdersCount) },
  ]

  // ── Compras rows ──────────────────────────────────────────────────────────

  const growthPctPurchase = kpiPurchase.data?.growthPct
  const purchaseRows: KpiRow[] = [
    { label: 'Compras LTM',  value: fmtAmt(kpiPurchase.data?.grossPurchasesLtm) },
    {
      label: 'Crecimiento',
      value: kpiPurchase.data == null ? '—' : `${fmtPct(growthPctPurchase)}%`,
      color: growthPctPurchase == null ? undefined
        : growthPctPurchase > 0 ? '#16A34A'
        : growthPctPurchase < 0 ? '#DC2626'
        : 'inherit',
    },
    { label: 'OC abiertas',  value: fmtNum(kpiPurchase.data?.openOrdersCount) },
    {
      label: 'OC vencidas',
      value: fmtNum(kpiPurchase.data?.overdueOrdersCount),
      color: (kpiPurchase.data?.overdueOrdersCount ?? 0) > 0 ? '#DC2626' : undefined,
    },
  ]

  // ── Inventario rows ───────────────────────────────────────────────────────

  const inventoryRows: KpiRow[] = [
    { label: 'Valor stock',    value: fmtAmt(kpiInventory.data?.totalStockValue) },
    { label: 'Ítems totales',  value: fmtNum(kpiInventory.data?.totalItems) },
    {
      label: 'Ítems bajo mín.',
      value: fmtNum(kpiInventory.data?.itemsBelowMin),
      color: (kpiInventory.data?.itemsBelowMin ?? 0) > 0 ? '#DC2626' : undefined,
    },
    { label: 'Slow moving',    value: fmtNum(kpiInventory.data?.slowMovingItemsCount) },
  ]

  // ── Finanzas rows ─────────────────────────────────────────────────────────

  const financeRows: KpiRow[] = [
    { label: 'AR abierto',  value: fmtAmt(summary.data?.totalOpenAr) },
    { label: 'AP abierto',  value: fmtAmt(summary.data?.totalOpenAp) },
    {
      label: 'DSO',
      value: summary.data?.dsoDays != null ? `${summary.data.dsoDays.toFixed(0)} días` : '—',
    },
    {
      label: 'DPO',
      value: summary.data?.dpoDays != null ? `${summary.data.dpoDays.toFixed(0)} días` : '—',
    },
  ]

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 32, padding: '0 0 40px' }}>
      <NativeBiPageHeader
        title="Resumen Ejecutivo"
        description="Vista consolidada de Ventas, Compras, Inventario y Finanzas"
      />

      {/* ── Alertas ── */}
      <section>
        <h2 style={{ fontSize: 13, fontWeight: 600, color: 'var(--c-text-muted)', textTransform: 'uppercase', letterSpacing: '0.06em', margin: '0 0 12px' }}>
          Alertas críticas
        </h2>
        <div
          style={{
            display: 'grid',
            gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))',
            gap: 12,
          }}
        >
          <AlertBanner
            label="AR Vencido"
            value={fmtAmt(arOverdue)}
            status={arStatus()}
            sub={arTotal > 0 ? `${((arOverdue / arTotal) * 100).toFixed(0)}% del AR total` : undefined}
          />
          <AlertBanner
            label="AP Vencido"
            value={fmtAmt(apOverdue)}
            status={apStatus()}
            sub={apTotal > 0 ? `${((apOverdue / apTotal) * 100).toFixed(0)}% del AP total` : undefined}
          />
          <AlertBanner
            label="Ítems bajo stock mín."
            value={fmtNum(itemsBelowMin)}
            status={belowMinStatus()}
          />
          <AlertBanner
            label="OC vencidas"
            value={fmtNum(overdueOrders)}
            status={overdueOrdersStatus()}
          />
        </div>
      </section>

      {/* ── Scorecard ── */}
      <section>
        <h2 style={{ fontSize: 13, fontWeight: 600, color: 'var(--c-text-muted)', textTransform: 'uppercase', letterSpacing: '0.06em', margin: '0 0 12px' }}>
          Scorecard ejecutivo
        </h2>
        <div
          style={{
            display: 'grid',
            gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))',
            gap: 16,
          }}
        >
          <ModuleCard
            title="Ventas"
            icon="📈"
            rows={salesRows}
            loading={kpiSales.isLoading}
            linkLabel="Ver detalle"
            linkHref="/client/bi/sales"
          />
          <ModuleCard
            title="Compras"
            icon="🛒"
            rows={purchaseRows}
            loading={kpiPurchase.isLoading}
            linkLabel="Ver detalle"
            linkHref="/client/bi/purchase"
          />
          <ModuleCard
            title="Inventario"
            icon="📦"
            rows={inventoryRows}
            loading={kpiInventory.isLoading}
            linkLabel="Ver detalle"
            linkHref="/client/bi/inventory-mart"
          />
          <ModuleCard
            title="Finanzas"
            icon="💰"
            rows={financeRows}
            loading={summary.isLoading}
            linkLabel="Ver detalle"
            linkHref="/client/bi/finance-mart"
          />
        </div>
      </section>

      {/* ── Tendencia ── */}
      <section>
        <h2 style={{ fontSize: 13, fontWeight: 600, color: 'var(--c-text-muted)', textTransform: 'uppercase', letterSpacing: '0.06em', margin: '0 0 12px' }}>
          AR Neto vs. AP Neto — últimos 12 meses
        </h2>
        <div
          style={{
            border: '1px solid var(--c-border)',
            borderRadius: 8,
            padding: '20px 20px 12px',
            background: '#fff',
            boxShadow: 'var(--shadow-sm)',
          }}
        >
          {periodKpi.isLoading ? (
            <NbLoadingSkeleton rows={5} height={36} />
          ) : arData.length === 0 ? (
            <NbEmptyState message="Sin datos de tendencia disponibles." icon="chart" />
          ) : (
            <NbAreaChart
              series={[
                { name: 'AR Neto', data: arData },
                { name: 'AP Neto', data: apData },
              ]}
              height={220}
            />
          )}
        </div>
      </section>
    </div>
  )
}
