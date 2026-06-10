import { useDashboardSummary, useDashboardSalesDaily } from '../hooks/useNativeBiDashboard'
import KpiCard from '../components/nativebi/KpiCard'
import SalesBarChart from '../components/nativebi/SalesBarChart'
import TopCustomersTable from '../components/nativebi/TopCustomersTable'
import SyncStatusWidget from '../components/nativebi/SyncStatusWidget'
import NativeBiPageHeader from '../components/nativebi/NativeBiPageHeader'
import { NbErrorState } from '../components/nativebi/NativeBiState'

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
      <NativeBiPageHeader
        title="Dashboard"
        description={updatedAt ? `Datos actualizados: ${updatedAt}` : 'Resumen ejecutivo de ventas'}
        actions={<SyncStatusWidget />}
      />

      {errorSummary && (
        <NbErrorState message="Error al cargar el resumen. Intenta recargar la página." />
      )}

      {/* KPI Cards */}
      <div className="nb-card-grid">
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
