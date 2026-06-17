import { useState } from 'react'
import NativeBiPageHeader from '../components/nativebi/NativeBiPageHeader'
import SortableTable, { type ColumnDef } from '../components/nativebi/SortableTable'
import { NbErrorState, NbEmptyState } from '../components/nativebi/NativeBiState'
import NativeBiHealthScore from '../components/nativebi/NativeBiHealthScore'
import NativeBiInfoBanner from '../components/nativebi/NativeBiInfoBanner'
import {
  useBiOperationsPipelineHealth,
  useBiOperationsAlerts,
  useBiOperationsDataQuality,
} from '../hooks/useProcessBi'
import type { OperationAlert, OperationDataQuality } from '../types/processBi'
import type { NbPagedMeta, PaginationParams } from '../types/nativeBi'

function fmtUtc(iso: string | null) {
  if (!iso) return '—'
  try {
    return new Date(iso).toLocaleString('es-CL', {
      day: '2-digit', month: 'short', year: 'numeric',
      hour: '2-digit', minute: '2-digit',
    })
  } catch { return iso }
}

const SEVERITY_COLOR: Record<string, string> = {
  critical: '#DC2626',
  warning:  '#D97706',
  info:     '#2563EB',
}

const STATUS_COLOR: Record<string, string> = {
  ok:      '#16A34A',
  warning: '#D97706',
  error:   '#DC2626',
  unknown: '#94A3B8',
}

type Tab = 'pipeline' | 'alerts' | 'dq' | 'runs'

const LIMIT = 20
const EMPTY_META: NbPagedMeta = { limit: LIMIT, offset: 0, count: 0, hasMore: false }

function initPag(): PaginationParams { return { limit: LIMIT, offset: 0 } }

const tabs: { id: Tab; label: string }[] = [
  { id: 'pipeline', label: 'Pipeline' },
  { id: 'alerts',   label: 'Alertas' },
  { id: 'dq',       label: 'Calidad de datos' },
  { id: 'runs',     label: 'Historial runs' },
]

function StatusBadge({ status }: { status: string }) {
  return (
    <span style={{
      display: 'inline-flex', alignItems: 'center', gap: 5,
      padding: '3px 10px', borderRadius: 4, fontSize: 12, fontWeight: 600,
      color: '#fff', backgroundColor: STATUS_COLOR[status] ?? '#94A3B8',
    }}>
      <span style={{ width: 6, height: 6, borderRadius: '50%', backgroundColor: 'rgba(255,255,255,0.6)', display: 'inline-block' }} />
      {status.toUpperCase()}
    </span>
  )
}

function SeverityBadge({ severity }: { severity: string }) {
  return (
    <span style={{
      display: 'inline-block', padding: '2px 8px', borderRadius: 4,
      fontSize: 11.5, fontWeight: 700, color: '#fff', textTransform: 'uppercase',
      backgroundColor: SEVERITY_COLOR[severity] ?? '#94A3B8',
    }}>
      {severity}
    </span>
  )
}

function ProcessCard({ label, status, lastRun }: { label: string; status: string; lastRun: string | null }) {
  const color = STATUS_COLOR[status] ?? '#94A3B8'
  return (
    <div style={{
      border: `1px solid ${status === 'ok' ? '#BBF7D0' : status === 'warning' ? '#FDE68A' : status === 'error' ? '#FECACA' : 'var(--c-border)'}`,
      borderLeft: `4px solid ${color}`,
      borderRadius: 8, padding: '14px 18px',
      backgroundColor: status === 'ok' ? '#F0FDF4' : status === 'warning' ? '#FFFBEB' : status === 'error' ? '#FEF2F2' : 'var(--c-surface)',
      minWidth: 220, flex: 1,
    }}>
      <div style={{ fontSize: 11.5, color: 'var(--c-text-muted)', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '0.04em', marginBottom: 8 }}>{label}</div>
      <StatusBadge status={status} />
      <div style={{ fontSize: 11.5, color: 'var(--c-text-faint)', marginTop: 8 }}>
        Último run: {fmtUtc(lastRun)}
      </div>
    </div>
  )
}

export default function OperationsDashboardPage() {
  const [tab, setTab] = useState<Tab>('pipeline')
  const [alertP, setAlertP] = useState<PaginationParams>(initPag())
  const [dqP, setDqP]       = useState<PaginationParams>(initPag())

  const { data: health, isLoading: loadingHealth, error: healthErr, refetch: refetchHealth } = useBiOperationsPipelineHealth()
  const { data: alertData, isLoading: loadingAlerts } = useBiOperationsAlerts(alertP)
  const { data: dqData,    isLoading: loadingDq }     = useBiOperationsDataQuality(dqP)

  const alerts          = alertData?.data ?? []
  const criticalCount   = alerts.filter((a) => a.severity === 'critical').length
  const warningCount    = alerts.filter((a) => a.severity === 'warning').length
  const infoCount       = alerts.filter((a) => a.severity === 'info').length

  const dqIssues        = dqData?.data ?? []
  const criticalDq      = dqIssues.filter((d) => d.severity === 'critical').length
  const totalDqRows     = dqIssues.reduce((s, d) => s + d.affectedRows, 0)

  const alertCols: ColumnDef<OperationAlert>[] = [
    {
      key: 'severity',
      label: 'Severidad',
      render: (r) => <SeverityBadge severity={r.severity} />,
    },
    {
      key: 'ruleCode',
      label: 'Regla',
      render: (r) => <span style={{ fontFamily: 'monospace', fontSize: 12 }}>{r.ruleCode}</span>,
    },
    {
      key: 'message',
      label: 'Mensaje',
      render: (r) => <span style={{ fontSize: 13 }}>{r.message ?? r.ruleCode}</span>,
    },
    {
      key: 'triggeredValue',
      label: 'Valor',
      align: 'right',
      render: (r) => (
        <span style={{ fontVariantNumeric: 'tabular-nums', color: 'var(--c-text-muted)' }}>
          {r.triggeredValue ?? '—'}
        </span>
      ),
    },
    {
      key: 'triggeredAt',
      label: 'Disparada',
      align: 'right',
      render: (r) => <span style={{ fontSize: 12 }}>{fmtUtc(r.triggeredAtUtc)}</span>,
    },
  ]

  const dqCols: ColumnDef<OperationDataQuality>[] = [
    {
      key: 'severity',
      label: 'Severidad',
      render: (r) => <SeverityBadge severity={r.severity} />,
    },
    {
      key: 'sapObject',
      label: 'Objeto SAP',
      render: (r) => <span style={{ fontFamily: 'monospace', fontSize: 12, fontWeight: 600 }}>{r.sapObject}</span>,
    },
    {
      key: 'issueType',
      label: 'Tipo',
      render: (r) => <span style={{ fontSize: 12.5 }}>{r.issueType}</span>,
    },
    {
      key: 'description',
      label: 'Descripción',
      render: (r) => <span style={{ fontSize: 13 }}>{r.description}</span>,
    },
    {
      key: 'affectedRows',
      label: 'Filas afectadas',
      align: 'right',
      render: (r) => (
        <span style={{ fontVariantNumeric: 'tabular-nums', color: r.affectedRows > 0 ? '#DC2626' : 'inherit' }}>
          {r.affectedRows.toLocaleString('es-CL')}
        </span>
      ),
    },
    {
      key: 'detectedAt',
      label: 'Detectado',
      align: 'right',
      render: (r) => <span style={{ fontSize: 12 }}>{fmtUtc(r.detectedAtUtc)}</span>,
    },
  ]

  return (
    <div className="cp-page">
      <NativeBiPageHeader
        title="Operaciones"
        description="Salud del pipeline de datos, alertas activas y calidad de datos"
      />

      <div className="db-card">
        <div
          className="db-card-header nb-tab-bar"
          style={{ paddingLeft: 4, paddingRight: 16, gap: 0, borderBottom: '1px solid var(--c-border)', overflowX: 'auto' }}
          role="tablist"
          aria-label="Secciones de operaciones"
        >
          {tabs.map((t) => (
            <button
              key={t.id}
              role="tab"
              aria-selected={tab === t.id}
              onClick={() => setTab(t.id)}
              style={{
                padding: '0 16px', height: 44, background: 'none', border: 'none',
                borderBottom: tab === t.id ? '2px solid var(--brand-primary, #2563EB)' : '2px solid transparent',
                color: tab === t.id ? 'var(--brand-primary, #2563EB)' : 'var(--c-text-muted)',
                fontWeight: tab === t.id ? 600 : 500, fontSize: 13.5,
                cursor: 'pointer', fontFamily: 'inherit', marginBottom: -1,
                transition: 'color 150ms, border-color 150ms',
              }}
            >
              {t.label}
              {t.id === 'alerts' && alertData && alertData.meta.count > 0 && (
                <span style={{
                  marginLeft: 6, display: 'inline-block', minWidth: 18, height: 18,
                  borderRadius: 9, backgroundColor: criticalCount > 0 ? '#DC2626' : '#D97706',
                  color: '#fff', fontSize: 11, fontWeight: 700, lineHeight: '18px',
                  textAlign: 'center', padding: '0 4px', verticalAlign: 'middle',
                }}>
                  {alertData.meta.count}
                </span>
              )}
              {t.id === 'dq' && dqData && dqData.meta.count > 0 && (
                <span style={{
                  marginLeft: 6, display: 'inline-block', minWidth: 18, height: 18,
                  borderRadius: 9, backgroundColor: criticalDq > 0 ? '#DC2626' : '#D97706',
                  color: '#fff', fontSize: 11, fontWeight: 700, lineHeight: '18px',
                  textAlign: 'center', padding: '0 4px', verticalAlign: 'middle',
                }}>
                  {dqData.meta.count}
                </span>
              )}
            </button>
          ))}
        </div>

        {/* ── Pipeline ────────────────────────────────────────────────────── */}
        {tab === 'pipeline' && (
          healthErr ? (
            <div style={{ padding: '16px 20px' }}>
              <NbErrorState message="Error al cargar estado del pipeline." onRetry={() => refetchHealth()} />
            </div>
          ) : loadingHealth ? (
            <div style={{ padding: 24 }}>
              {Array.from({ length: 4 }).map((_, i) => (
                <div key={i} className="cp-skeleton" style={{ height: 44, borderRadius: 6, marginBottom: 8 }} />
              ))}
            </div>
          ) : !health ? (
            <div style={{ padding: '24px 20px' }}>
              <NbEmptyState
                message="Sin datos del pipeline disponibles. Disponible al completar la primera ejecución del extractor."
                icon="chart"
              />
            </div>
          ) : (
            <div style={{ padding: '20px 24px' }}>
              {/* Health score hero */}
              <div style={{ display: 'grid', gridTemplateColumns: 'auto 1fr', gap: 32, alignItems: 'center', marginBottom: 28, padding: '20px 24px', backgroundColor: 'var(--c-bg)', borderRadius: 8, border: '1px solid var(--c-border)' }}>
                <NativeBiHealthScore score={health.healthScore} size="lg" />
                <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(130px, 1fr))', gap: 12 }}>
                  {[
                    { label: 'Alertas activas',      value: health.activeAlerts,      color: health.activeAlerts > 0 ? '#DC2626' : '#16A34A' },
                    { label: 'Objetos extraídos',    value: health.objectsExtracted,  color: 'var(--c-text)' },
                    { label: 'Errores DQ sin resolver', value: health.dqErrorsUnresolved, color: health.dqErrorsUnresolved > 0 ? '#D97706' : '#16A34A' },
                  ].map((kpi) => (
                    <div key={kpi.label} style={{ textAlign: 'center' }}>
                      <div style={{ fontSize: 24, fontWeight: 700, color: kpi.color, fontVariantNumeric: 'tabular-nums' }}>
                        {kpi.value.toLocaleString('es-CL')}
                      </div>
                      <div style={{ fontSize: 11.5, color: 'var(--c-text-muted)', fontWeight: 500, marginTop: 2 }}>{kpi.label}</div>
                    </div>
                  ))}
                </div>
              </div>

              {/* Process status cards */}
              <div style={{ fontSize: 12, fontWeight: 600, color: 'var(--c-text-muted)', marginBottom: 12, textTransform: 'uppercase', letterSpacing: '0.04em' }}>
                Estado de procesos
              </div>
              <div style={{ display: 'flex', gap: 16, flexWrap: 'wrap', marginBottom: health.activeAlerts > 0 ? 20 : 0 }}>
                <ProcessCard label="Extractor" status={health.extractorStatus} lastRun={health.lastExtractorRunUtc} />
                <ProcessCard label="Transform" status={health.transformStatus} lastRun={health.lastTransformRunUtc} />
              </div>

              {health.activeAlerts > 0 && (
                <div style={{ marginTop: 20 }}>
                  <NativeBiInfoBanner
                    variant="warning"
                    message={`${health.activeAlerts} alerta(s) activa(s) en el pipeline. Revisa la pestaña "Alertas" para detalles.`}
                  />
                </div>
              )}
            </div>
          )
        )}

        {/* ── Alertas ─────────────────────────────────────────────────────── */}
        {tab === 'alerts' && (
          alerts.length === 0 && !loadingAlerts ? (
            <div style={{ padding: '0 0 8px' }}>
              <NbEmptyState message="Sin alertas activas. El pipeline opera sin incidencias." icon="chart" />
            </div>
          ) : (
            <>
              {(criticalCount + warningCount + infoCount > 0) && (
                <div style={{ padding: '12px 20px', borderBottom: '1px solid var(--c-border)', display: 'flex', gap: 16, alignItems: 'center', flexWrap: 'wrap' }}>
                  <span style={{ fontSize: 12.5, fontWeight: 600, color: 'var(--c-text-muted)' }}>Resumen:</span>
                  {criticalCount > 0 && (
                    <span style={{ display: 'inline-flex', alignItems: 'center', gap: 5, fontSize: 12.5 }}>
                      <span style={{ width: 8, height: 8, borderRadius: '50%', backgroundColor: '#DC2626', display: 'inline-block' }} />
                      <strong style={{ color: '#DC2626' }}>{criticalCount}</strong>&nbsp;crítica(s)
                    </span>
                  )}
                  {warningCount > 0 && (
                    <span style={{ display: 'inline-flex', alignItems: 'center', gap: 5, fontSize: 12.5 }}>
                      <span style={{ width: 8, height: 8, borderRadius: '50%', backgroundColor: '#D97706', display: 'inline-block' }} />
                      <strong style={{ color: '#D97706' }}>{warningCount}</strong>&nbsp;advertencia(s)
                    </span>
                  )}
                  {infoCount > 0 && (
                    <span style={{ display: 'inline-flex', alignItems: 'center', gap: 5, fontSize: 12.5 }}>
                      <span style={{ width: 8, height: 8, borderRadius: '50%', backgroundColor: '#2563EB', display: 'inline-block' }} />
                      <strong style={{ color: '#2563EB' }}>{infoCount}</strong>&nbsp;informativa(s)
                    </span>
                  )}
                </div>
              )}
              <SortableTable
                data={alerts}
                columns={alertCols}
                meta={alertData?.meta ?? EMPTY_META}
                onPageChange={(offset) => setAlertP((p) => ({ ...p, offset }))}
                onSortChange={() => {}}
                isLoading={loadingAlerts}
                rowKey={(r) => String(r.id)}
              />
            </>
          )
        )}

        {/* ── Calidad de datos ────────────────────────────────────────────── */}
        {tab === 'dq' && (
          dqIssues.length === 0 && !loadingDq ? (
            <div style={{ padding: '0 0 8px' }}>
              <NbEmptyState message="Sin problemas de calidad detectados. Los datos del pipeline son consistentes." icon="table" />
            </div>
          ) : (
            <>
              {dqIssues.length > 0 && (
                <div style={{ padding: '12px 20px', borderBottom: '1px solid var(--c-border)', display: 'flex', gap: 20, alignItems: 'center', flexWrap: 'wrap' }}>
                  <span style={{ fontSize: 12.5, fontWeight: 600, color: 'var(--c-text-muted)' }}>
                    {dqIssues.length} issue(s) detectado(s)
                  </span>
                  {criticalDq > 0 && (
                    <span style={{ fontSize: 12.5, color: '#DC2626', fontWeight: 600 }}>
                      {criticalDq} crítico(s)
                    </span>
                  )}
                  {totalDqRows > 0 && (
                    <span style={{ fontSize: 12.5, color: 'var(--c-text-muted)' }}>
                      {totalDqRows.toLocaleString('es-CL')} filas afectadas
                    </span>
                  )}
                </div>
              )}
              <SortableTable
                data={dqIssues}
                columns={dqCols}
                meta={dqData?.meta ?? EMPTY_META}
                onPageChange={(offset) => setDqP((p) => ({ ...p, offset }))}
                onSortChange={() => {}}
                isLoading={loadingDq}
                rowKey={(r) => String(r.id)}
              />
            </>
          )
        )}

        {/* ── Historial runs ──────────────────────────────────────────────── */}
        {tab === 'runs' && (
          !health && !loadingHealth ? (
            <div style={{ padding: '24px 20px' }}>
              <NbEmptyState
                message="Sin datos de pipeline disponibles. Disponible al completar primera ejecución del extractor."
                icon="chart"
              />
            </div>
          ) : loadingHealth ? (
            <div style={{ padding: 24 }}>
              {Array.from({ length: 4 }).map((_, i) => (
                <div key={i} className="cp-skeleton" style={{ height: 44, borderRadius: 6, marginBottom: 8 }} />
              ))}
            </div>
          ) : health ? (
            <div style={{ padding: '20px 24px' }}>
              <div style={{ display: 'flex', flexDirection: 'column', gap: 12, maxWidth: 600 }}>
                {[
                  {
                    label: 'Extractor',
                    status: health.extractorStatus,
                    lastRun: health.lastExtractorRunUtc,
                    detail: `${health.objectsExtracted.toLocaleString('es-CL')} objetos extraídos`,
                  },
                  {
                    label: 'Transform',
                    status: health.transformStatus,
                    lastRun: health.lastTransformRunUtc,
                    detail: `Health score: ${health.healthScore} / 100`,
                  },
                ].map((proc) => (
                  <div key={proc.label} style={{
                    display: 'flex', alignItems: 'center', gap: 16,
                    padding: '14px 18px', borderRadius: 8, border: '1px solid var(--c-border)',
                    backgroundColor: 'var(--c-surface)',
                  }}>
                    <div style={{ width: 44, height: 44, borderRadius: '50%', backgroundColor: STATUS_COLOR[proc.status] ?? '#94A3B8', display: 'flex', alignItems: 'center', justifyContent: 'center', flexShrink: 0 }}>
                      <span style={{ color: '#fff', fontSize: 18 }}>
                        {proc.status === 'ok' ? '✓' : proc.status === 'error' ? '✕' : proc.status === 'warning' ? '!' : '?'}
                      </span>
                    </div>
                    <div style={{ flex: 1 }}>
                      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                        <span style={{ fontWeight: 600, fontSize: 14 }}>{proc.label}</span>
                        <StatusBadge status={proc.status} />
                      </div>
                      <div style={{ fontSize: 12.5, color: 'var(--c-text-muted)', marginTop: 4 }}>
                        Último run: {fmtUtc(proc.lastRun)} · {proc.detail}
                      </div>
                    </div>
                  </div>
                ))}
                <div style={{ fontSize: 11.5, color: 'var(--c-text-faint)', marginTop: 4 }}>
                  Última actualización: {fmtUtc(health.updatedAtUtc)}
                </div>
              </div>
            </div>
          ) : null
        )}
      </div>
    </div>
  )
}
