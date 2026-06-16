import { useState } from 'react'
import NativeBiPageHeader from '../components/nativebi/NativeBiPageHeader'
import KpiCard from '../components/nativebi/KpiCard'
import SortableTable, { type ColumnDef } from '../components/nativebi/SortableTable'
import { NbErrorState, NbEmptyState } from '../components/nativebi/NativeBiState'
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
  } catch {
    return iso
  }
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

type Tab = 'alerts' | 'dq' | 'runs'

const LIMIT = 20
const EMPTY_META: NbPagedMeta = { limit: LIMIT, offset: 0, count: 0, hasMore: false }

function initPag(): PaginationParams {
  return { limit: LIMIT, offset: 0 }
}

const tabs: { id: Tab; label: string }[] = [
  { id: 'alerts', label: 'Alertas' },
  { id: 'dq', label: 'Calidad de datos' },
  { id: 'runs', label: 'Historial runs' },
]

function StatusDot({ status }: { status: string }) {
  return (
    <span
      style={{
        display: 'inline-block',
        width: 8,
        height: 8,
        borderRadius: '50%',
        backgroundColor: STATUS_COLOR[status] ?? '#94A3B8',
        marginRight: 6,
        verticalAlign: 'middle',
      }}
    />
  )
}

export default function OperationsDashboardPage() {
  const [tab, setTab] = useState<Tab>('alerts')
  const [alertP, setAlertP] = useState<PaginationParams>(initPag())
  const [dqP, setDqP] = useState<PaginationParams>(initPag())

  const { data: health, isLoading: loadingHealth, error: healthErr, refetch: refetchHealth } = useBiOperationsPipelineHealth()
  const { data: alertData, isLoading: loadingAlerts } = useBiOperationsAlerts(alertP)
  const { data: dqData, isLoading: loadingDq } = useBiOperationsDataQuality(dqP)

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
      key: 'sapObject',
      label: 'Objeto SAP',
      render: (r) => <span style={{ fontFamily: 'monospace', fontSize: 12, fontWeight: 600 }}>{r.sapObject}</span>,
    },
    {
      key: 'issueType',
      label: 'Tipo',
      render: (r) => <span>{r.issueType}</span>,
    },
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
          }}
        >
          {r.severity}
        </span>
      ),
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
      render: (r) => <span style={{ fontVariantNumeric: 'tabular-nums' }}>{r.affectedRows}</span>,
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

      {healthErr ? (
        <NbErrorState
          message="Error al cargar estado del pipeline."
          onRetry={() => refetchHealth()}
        />
      ) : (
        <>
          {/* Health summary */}
          <div className="nb-card-grid">
            <KpiCard
              label="Health score"
              value={loadingHealth ? '—' : `${health?.healthScore ?? 0} / 100`}
              loading={loadingHealth}
            />
            <KpiCard
              label="Alertas activas"
              value={health?.activeAlerts ?? 0}
              loading={loadingHealth}
            />
            <KpiCard
              label="Objetos extraídos"
              value={health?.objectsExtracted ?? 0}
              loading={loadingHealth}
            />
            <KpiCard
              label="Errores DQ sin resolver"
              value={health?.dqErrorsUnresolved ?? 0}
              loading={loadingHealth}
            />
          </div>

          {/* Status row */}
          {!loadingHealth && health && (
            <div
              className="db-card"
              style={{ padding: '12px 20px', display: 'flex', gap: 32, flexWrap: 'wrap', marginBottom: 0 }}
            >
              <div style={{ display: 'flex', flexDirection: 'column', gap: 3 }}>
                <span style={{ fontSize: 11.5, color: 'var(--c-text-muted)', fontWeight: 500 }}>Extractor</span>
                <span style={{ fontSize: 13.5, fontWeight: 600 }}>
                  <StatusDot status={health.extractorStatus} />
                  {health.extractorStatus.toUpperCase()}
                </span>
                <span style={{ fontSize: 11.5, color: 'var(--c-text-faint)' }}>
                  {fmtUtc(health.lastExtractorRunUtc)}
                </span>
              </div>
              <div style={{ display: 'flex', flexDirection: 'column', gap: 3 }}>
                <span style={{ fontSize: 11.5, color: 'var(--c-text-muted)', fontWeight: 500 }}>Transform</span>
                <span style={{ fontSize: 13.5, fontWeight: 600 }}>
                  <StatusDot status={health.transformStatus} />
                  {health.transformStatus.toUpperCase()}
                </span>
                <span style={{ fontSize: 11.5, color: 'var(--c-text-faint)' }}>
                  {fmtUtc(health.lastTransformRunUtc)}
                </span>
              </div>
            </div>
          )}
        </>
      )}

      {/* Tabbed tables */}
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
              }}
            >
              {t.label}
              {t.id === 'alerts' && alertData && alertData.meta.count > 0 && (
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
                  {alertData.meta.count}
                </span>
              )}
            </button>
          ))}
        </div>

        {tab === 'alerts' && (
          alertData?.data.length === 0 && !loadingAlerts ? (
            <NbEmptyState message="Sin alertas activas. El pipeline no presenta alertas pendientes." icon="chart" />
          ) : (
            <SortableTable
              data={alertData?.data ?? []}
              columns={alertCols}
              meta={alertData?.meta ?? EMPTY_META}
              onPageChange={(offset) => setAlertP((p) => ({ ...p, offset }))}
              onSortChange={() => {}}
              isLoading={loadingAlerts}
              rowKey={(r) => String(r.id)}
            />
          )
        )}

        {tab === 'dq' && (
          dqData?.data.length === 0 && !loadingDq ? (
            <NbEmptyState message="Sin problemas de calidad de datos detectados." icon="table" />
          ) : (
            <SortableTable
              data={dqData?.data ?? []}
              columns={dqCols}
              meta={dqData?.meta ?? EMPTY_META}
              onPageChange={(offset) => setDqP((p) => ({ ...p, offset }))}
              onSortChange={() => {}}
              isLoading={loadingDq}
              rowKey={(r) => String(r.id)}
            />
          )
        )}

        {tab === 'runs' && (
          !health ? (
            <NbEmptyState message="Sin datos de pipeline disponibles." icon="chart" />
          ) : (
            <div style={{ padding: '20px 24px' }}>
              <div
                style={{
                  display: 'grid',
                  gridTemplateColumns: '220px 1fr',
                  gap: '10px 16px',
                  fontSize: 13,
                  lineHeight: 1.6,
                  maxWidth: 560,
                }}
              >
                <span style={{ color: 'var(--c-text-muted)', fontWeight: 500 }}>Último extractor run</span>
                <span>{fmtUtc(health.lastExtractorRunUtc)}</span>
                <span style={{ color: 'var(--c-text-muted)', fontWeight: 500 }}>Estado extractor</span>
                <span style={{ fontWeight: 600, color: STATUS_COLOR[health.extractorStatus] ?? '#94A3B8' }}>
                  {health.extractorStatus.toUpperCase()}
                </span>
                <span style={{ color: 'var(--c-text-muted)', fontWeight: 500 }}>Último transform run</span>
                <span>{fmtUtc(health.lastTransformRunUtc)}</span>
                <span style={{ color: 'var(--c-text-muted)', fontWeight: 500 }}>Estado transform</span>
                <span style={{ fontWeight: 600, color: STATUS_COLOR[health.transformStatus] ?? '#94A3B8' }}>
                  {health.transformStatus.toUpperCase()}
                </span>
                <span style={{ color: 'var(--c-text-muted)', fontWeight: 500 }}>Objetos extraídos</span>
                <span style={{ fontVariantNumeric: 'tabular-nums' }}>
                  {health.objectsExtracted.toLocaleString('es-CL')}
                </span>
                <span style={{ color: 'var(--c-text-muted)', fontWeight: 500 }}>Health score</span>
                <span style={{ fontVariantNumeric: 'tabular-nums', fontWeight: 600 }}>
                  {health.healthScore} / 100
                </span>
                <span style={{ color: 'var(--c-text-muted)', fontWeight: 500 }}>Última actualización</span>
                <span style={{ color: 'var(--c-text-faint)', fontSize: 12 }}>{fmtUtc(health.updatedAtUtc)}</span>
              </div>
            </div>
          )
        )}
      </div>
    </div>
  )
}
