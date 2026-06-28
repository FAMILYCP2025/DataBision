import { useState } from 'react'
import { useDashboardSummary, useDashboardSalesDaily } from '../hooks/useNativeBiDashboard'
import { useBiOperationsAlerts } from '../hooks/useProcessBi'
import KpiCard from '../components/nativebi/KpiCard'
import SalesBarChart from '../components/nativebi/SalesBarChart'
import TopCustomersTable from '../components/nativebi/TopCustomersTable'
import SyncStatusWidget from '../components/nativebi/SyncStatusWidget'
import NativeBiPageHeader from '../components/nativebi/NativeBiPageHeader'
import { NbErrorState, NbEmptyState } from '../components/nativebi/NativeBiState'
import { NativeBiOnboardingBanner } from '../components/nativebi/NativeBiInfoBanner'
import SortableTable, { type ColumnDef } from '../components/nativebi/SortableTable'
import type { OperationAlert } from '../types/processBi'
import type { NbPagedMeta, PaginationParams } from '../types/nativeBi'

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

function fmtUtc(iso: string | null) {
  if (!iso) return '—'
  try {
    return new Date(iso).toLocaleString('es-CL', {
      day: '2-digit', month: 'short', year: 'numeric',
      hour: '2-digit', minute: '2-digit',
    })
  } catch {
    return iso
  }
}

type Tab = 'resumen' | 'tendencia' | 'clientes' | 'alertas'

const LIMIT = 20
const EMPTY_META: NbPagedMeta = { limit: LIMIT, offset: 0, count: 0, hasMore: false }

const SEVERITY_COLOR: Record<string, string> = {
  critical: '#DC2626',
  warning:  '#D97706',
  info:     '#2563EB',
}

const tabs: { id: Tab; label: string }[] = [
  { id: 'resumen',   label: 'Resumen' },
  { id: 'tendencia', label: 'Tendencia' },
  { id: 'clientes',  label: 'Top clientes' },
  { id: 'alertas',   label: 'Alertas ejecutivas' },
]

function TabBar({ tab, setTab, alertCount }: { tab: Tab; setTab: (t: Tab) => void; alertCount: number }) {
  return (
    <div
      className="db-card-header nb-tab-bar"
      style={{ paddingLeft: 4, paddingRight: 16, gap: 0, borderBottom: '1px solid var(--c-border)', overflowX: 'auto' }}
      role="tablist"
      aria-label="Secciones del dashboard"
    >
      {tabs.map((t) => (
        <button
          key={t.id}
          role="tab"
          aria-selected={tab === t.id}
          onClick={() => setTab(t.id)}
          style={{
            padding: '0 16px',
            height: 44,
            background: 'none',
            border: 'none',
            borderBottom: tab === t.id ? '2px solid var(--brand-primary, #2563EB)' : '2px solid transparent',
            color: tab === t.id ? 'var(--brand-primary, #2563EB)' : 'var(--c-text-muted)',
            fontWeight: tab === t.id ? 600 : 500,
            fontSize: 13.5,
            cursor: 'pointer',
            fontFamily: 'inherit',
            marginBottom: -1,
            transition: 'color 150ms, border-color 150ms',
            whiteSpace: 'nowrap',
          }}
        >
          {t.label}
          {t.id === 'alertas' && alertCount > 0 && (
            <span
              style={{
                marginLeft: 6,
                display: 'inline-block',
                minWidth: 18,
                height: 18,
                borderRadius: 9,
                backgroundColor: '#DC2626',
                color: '#fff',
                fontSize: 11,
                fontWeight: 700,
                lineHeight: '18px',
                textAlign: 'center',
                padding: '0 4px',
                verticalAlign: 'middle',
              }}
            >
              {alertCount}
            </span>
          )}
        </button>
      ))}
    </div>
  )
}

export default function NativeBiDashboardPage() {
  const [tab, setTab] = useState<Tab>('resumen')
  const [alertP] = useState<PaginationParams>({ limit: LIMIT, offset: 0 })

  const { data: summary, isLoading: loadingSummary, isError: errorSummary } = useDashboardSummary()
  const { data: salesDaily, isLoading: loadingChart } = useDashboardSalesDaily(365)
  const { data: alertData, isLoading: loadingAlerts } = useBiOperationsAlerts(alertP)

  const updatedAt = summary?.transformedAtUtc
    ? new Date(summary.transformedAtUtc).toLocaleString('es-CL')
    : null

  const alertCount = alertData?.meta.count ?? 0

  const alertCols: ColumnDef<OperationAlert>[] = [
    {
      key: 'severity',
      label: 'Severidad',
      render: (r) => (
        <span
          style={{
            display: 'inline-block',
            padding: '2px 8px',
            borderRadius: 4,
            fontSize: 11.5,
            fontWeight: 600,
            color: '#fff',
            backgroundColor: SEVERITY_COLOR[r.severity] ?? '#94A3B8',
            textTransform: 'uppercase',
          }}
        >
          {r.severity}
        </span>
      ),
    },
    {
      key: 'ruleCode',
      label: 'Regla',
      render: (r) => <span style={{ fontFamily: 'monospace', fontSize: 12 }}>{r.ruleCode}</span>,
    },
    {
      key: 'message',
      label: 'Mensaje',
      render: (r) => <span>{r.message ?? r.ruleCode}</span>,
    },
    {
      key: 'triggeredAt',
      label: 'Disparada',
      align: 'right',
      render: (r) => <span style={{ fontSize: 12 }}>{fmtUtc(r.triggeredAtUtc)}</span>,
    },
  ]

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

      <NativeBiOnboardingBanner visible={!loadingSummary && !errorSummary && !summary} />

      <div className="db-card">
        <TabBar tab={tab} setTab={setTab} alertCount={alertCount} />

        {/* ── Resumen ─────────────────────────────────────────────────────── */}
        {tab === 'resumen' && (
          <div style={{ padding: '20px 20px 4px' }}>
            <div className="nb-card-grid" style={{ marginBottom: 24 }}>
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
                subLabel={
                  fmtDate(summary?.lastInvoiceDate ?? null)
                    ? `Última factura: ${fmtDate(summary?.lastInvoiceDate ?? null)}`
                    : undefined
                }
                loading={loadingSummary}
              />
            </div>

            {/* Mini chart preview */}
            <div style={{ marginBottom: 24 }}>
              <div style={{ fontSize: 13, fontWeight: 600, color: 'var(--c-text)', marginBottom: 12 }}>
                Ventas netas — últimos 12 meses
              </div>
              {loadingChart ? (
                <div className="cp-skeleton" style={{ height: 90, borderRadius: 4 }} />
              ) : !salesDaily || salesDaily.length === 0 ? (
                <NbEmptyState
                  message="No hay facturas sincronizadas aún. Ejecuta --object OINV --send"
                  icon="chart"
                />
              ) : (
                <SalesBarChart data={salesDaily ?? []} height={90} />
              )}
            </div>
          </div>
        )}

        {/* ── Tendencia ───────────────────────────────────────────────────── */}
        {tab === 'tendencia' && (
          <div style={{ padding: '20px 20px' }}>
            <div style={{ fontSize: 13, fontWeight: 600, color: 'var(--c-text)', marginBottom: 16 }}>
              Evolución de ventas netas — últimos 12 meses
            </div>
            {loadingChart ? (
              <div className="cp-skeleton" style={{ height: 160, borderRadius: 4 }} />
            ) : !salesDaily || salesDaily.length === 0 ? (
              <NbEmptyState
                message="No hay facturas sincronizadas aún. Ejecuta --object OINV --send"
                icon="chart"
              />
            ) : (
              <>
                <SalesBarChart data={salesDaily} height={160} />

                {/* Resumen de últimos 3 períodos */}
                <div style={{ marginTop: 28 }}>
                  <div style={{ fontSize: 12.5, fontWeight: 600, color: 'var(--c-text-muted)', marginBottom: 10, textTransform: 'uppercase', letterSpacing: '0.04em' }}>
                    Últimos períodos
                  </div>
                  <div className="nb-table-scroll">
                    <table className="db-table">
                      <thead>
                        <tr>
                          <th>Fecha</th>
                          <th style={{ textAlign: 'right' }}>Ventas netas</th>
                        </tr>
                      </thead>
                      <tbody>
                        {salesDaily.slice(-6).reverse().map((d) => (
                          <tr key={d.salesDate}>
                            <td style={{ fontSize: 13 }}>
                              {fmtDate(d.salesDate)}
                            </td>
                            <td style={{ textAlign: 'right', fontVariantNumeric: 'tabular-nums', fontSize: 13 }}>
                              {fmtAmt(d.netSalesAmount)}
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                </div>
              </>
            )}
          </div>
        )}

        {/* ── Top clientes ────────────────────────────────────────────────── */}
        {tab === 'clientes' && (
          <div>
            <div style={{ padding: '16px 20px 8px' }}>
              <div style={{ fontSize: 13, fontWeight: 600, color: 'var(--c-text)' }}>
                Top clientes por ventas netas
              </div>
            </div>
            {!loadingSummary && !errorSummary && summary && summary.activeCustomers === 0 ? (
              <div style={{ padding: '0 20px 20px' }}>
                <NbEmptyState
                  message="No hay clientes sincronizados. Ejecuta --object OCRD --send"
                  icon="chart"
                />
              </div>
            ) : (
              <TopCustomersTable />
            )}
          </div>
        )}

        {/* ── Alertas ejecutivas ──────────────────────────────────────────── */}
        {tab === 'alertas' && (
          alertData?.data.length === 0 && !loadingAlerts ? (
            <NbEmptyState
              message="Sin alertas activas. El pipeline opera sin incidencias registradas."
              icon="chart"
            />
          ) : (
            <SortableTable
              data={alertData?.data ?? []}
              columns={alertCols}
              meta={alertData?.meta ?? EMPTY_META}
              onPageChange={() => {}}
              onSortChange={() => {}}
              isLoading={loadingAlerts}
              rowKey={(r) => String(r.id)}
            />
          )
        )}
      </div>
    </div>
  )
}
