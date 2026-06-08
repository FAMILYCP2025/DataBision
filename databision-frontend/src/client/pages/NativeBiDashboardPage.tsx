import { useDashboardSummary, useDashboardSalesDaily } from '../hooks/useNativeBiDashboard'
import KpiCard from '../components/nativebi/KpiCard'
import SalesBarChart from '../components/nativebi/SalesBarChart'
import TopCustomersTable from '../components/nativebi/TopCustomersTable'
import SyncStatusWidget from '../components/nativebi/SyncStatusWidget'

function fmtAmt(n: number) {
  return n.toLocaleString('es-CL', { maximumFractionDigits: 0 })
}

function fmtDate(iso: string | null) {
  if (!iso) return undefined
  return new Date(iso + 'T00:00:00').toLocaleDateString('es-CL', {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
  })
}

export default function NativeBiDashboardPage() {
  const { data: summary, isLoading: loadingSummary, isError: errorSummary } = useDashboardSummary()
  const { data: salesDaily, isLoading: loadingChart } = useDashboardSalesDaily(30)

  const updatedAt = summary?.transformedAtUtc
    ? new Date(summary.transformedAtUtc).toLocaleString('es-CL')
    : null

  return (
    <div className="cp-page">
      <div className="cp-page-header">
        <div>
          <h1 className="cp-page-title">Dashboard</h1>
          <p className="cp-page-subtitle">
            {updatedAt ? `Datos actualizados: ${updatedAt}` : 'Resumen ejecutivo de ventas'}
          </p>
        </div>
        <SyncStatusWidget />
      </div>

      {errorSummary && (
        <div className="db-alert db-alert--error">
          Error al cargar el resumen. Intenta recargar la página.
        </div>
      )}

      {/* KPI Cards */}
      <div className="db-stats-grid">
        <KpiCard
          label="Ventas netas"
          value={summary ? fmtAmt(summary.netSalesAmount) : '—'}
          loading={loadingSummary}
        />
        <KpiCard
          label="Facturas"
          value={summary ? summary.invoiceCount : '—'}
          loading={loadingSummary}
        />
        <KpiCard
          label="Clientes activos"
          value={summary ? summary.activeCustomers : '—'}
          loading={loadingSummary}
        />
        <KpiCard
          label="Ticket promedio"
          value={summary ? fmtAmt(summary.avgTicketAmount) : '—'}
          subLabel={fmtDate(summary?.lastInvoiceDate ?? null) ? `Última factura: ${fmtDate(summary?.lastInvoiceDate ?? null)}` : undefined}
          loading={loadingSummary}
        />
      </div>

      {/* Sales Chart */}
      <div className="db-card">
        <div className="db-card-header">
          <span className="db-card-title">Ventas netas — últimos 30 días</span>
        </div>
        <div style={{ padding: '16px 20px 20px' }}>
          {loadingChart ? (
            <div className="cp-skeleton" style={{ height: 120, borderRadius: 4 }} />
          ) : (
            <SalesBarChart data={salesDaily ?? []} height={120} />
          )}
        </div>
      </div>

      {/* Top Customers */}
      <div className="db-card">
        <div className="db-card-header">
          <span className="db-card-title">Top clientes por ventas netas</span>
        </div>
        <TopCustomersTable />
      </div>
    </div>
  )
}
