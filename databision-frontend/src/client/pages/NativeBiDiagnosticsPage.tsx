import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useNativeBiDiagnostics, useNativeBiTableCounts } from '../hooks/useNativeBiDiagnostics'
import { useSyncObjects, useSyncTransformStatus } from '../hooks/useNativeBiSync'
import { useClientAuthStore } from '../store/useClientAuthStore'
import NativeBiPageHeader from '../components/nativebi/NativeBiPageHeader'
import NativeBiStatusBadge from '../components/nativebi/NativeBiStatusBadge'
import { NbLoadingSkeleton, NbErrorState, NbEmptyState } from '../components/nativebi/NativeBiState'
import type { SyncStatusLevel } from '../types/nativeBi'

function fmtDatetime(iso: string | null) {
  if (!iso) return '—'
  return new Date(iso).toLocaleString('es-CL')
}

const STATUS_COLOR: Record<SyncStatusLevel, string> = {
  ok:      '#16A34A',
  warning: '#D97706',
  error:   '#DC2626',
  unknown: '#94A3B8',
}

const OBJECT_STATUS_COLOR: Record<string, string> = {
  ok:       '#16A34A',
  warning:  '#D97706',
  no_data:  '#94A3B8',
}

type Tab = 'sistema' | 'tablas' | 'extraccion' | 'consistencia'

const tabs: { id: Tab; label: string }[] = [
  { id: 'sistema',     label: 'Sistema' },
  { id: 'tablas',      label: 'Conteo por tablas' },
  { id: 'extraccion',  label: 'Extracción' },
  { id: 'consistencia', label: 'Consistencia' },
]

export default function NativeBiDiagnosticsPage() {
  const navigate = useNavigate()
  const user = useClientAuthStore((s) => s.user)
  const isAdmin = user?.role === 'CompanyAdmin'
  const [tab, setTab] = useState<Tab>('sistema')

  const {
    data: diag,
    isLoading: loadingDiag,
    isError: errDiag,
    error: diagErr,
    refetch: refetchDiag,
  } = useNativeBiDiagnostics()

  const { data: tableCounts, isLoading: loadingTables } = useNativeBiTableCounts()
  const { data: syncObjects, isLoading: loadingObjects } = useSyncObjects()
  const { data: transformStatus, isLoading: loadingTransform } = useSyncTransformStatus()

  if (!isAdmin) {
    return (
      <div className="cp-page cp-page--center">
        <div className="cp-empty-state">
          <div className="cp-empty-icon">
            <svg width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
              <rect x="3" y="11" width="18" height="11" rx="2" ry="2" />
              <path d="M7 11V7a5 5 0 0 1 10 0v4" />
            </svg>
          </div>
          <h3>Acceso restringido</h3>
          <p>Esta sección solo está disponible para administradores de empresa.</p>
          <button
            className="db-btn db-btn--ghost"
            style={{ marginTop: 16 }}
            onClick={() => navigate('/client/bi/dashboard')}
          >
            Volver al dashboard
          </button>
        </div>
      </div>
    )
  }

  return (
    <div className="cp-page">
      <NativeBiPageHeader
        title="Diagnósticos"
        description="Estado del pipeline de datos — vista técnica"
        actions={
          <button
            className="db-btn db-btn--ghost db-btn--sm"
            aria-label="Actualizar diagnósticos"
            onClick={() => void refetchDiag()}
          >
            Actualizar
          </button>
        }
      />

      <div className="db-card">
        {/* Tab bar */}
        <div
          className="db-card-header nb-tab-bar"
          style={{ paddingLeft: 4, paddingRight: 16, gap: 0, borderBottom: '1px solid var(--c-border)', overflowX: 'auto' }}
          role="tablist"
          aria-label="Secciones de diagnósticos"
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
              {t.id === 'sistema' && diag && (
                <span
                  style={{
                    marginLeft: 6,
                    display: 'inline-block',
                    minWidth: 8,
                    height: 8,
                    borderRadius: '50%',
                    backgroundColor: STATUS_COLOR[diag.status] ?? '#94A3B8',
                    verticalAlign: 'middle',
                  }}
                />
              )}
            </button>
          ))}
        </div>

        {/* ── Sistema ─────────────────────────────────────────────────────── */}
        {tab === 'sistema' && (
          <>
            <div style={{ display: 'flex', alignItems: 'center', gap: 12, padding: '12px 20px 0' }}>
              {diag && <NativeBiStatusBadge status={diag.status} />}
            </div>
            {loadingDiag ? (
              <NbLoadingSkeleton rows={5} height={44} />
            ) : errDiag ? (
              <NbErrorState
                message={`Error al cargar diagnósticos: ${diagErr instanceof Error ? diagErr.message : 'Error desconocido'}`}
                onRetry={() => void refetchDiag()}
              />
            ) : (
              <div className="nb-table-scroll">
                <table className="db-table">
                  <thead>
                    <tr>
                      <th>Verificación</th>
                      <th>Estado</th>
                      <th>Detalle</th>
                    </tr>
                  </thead>
                  <tbody>
                    {diag?.checks.length === 0 && (
                      <tr>
                        <td colSpan={3} className="db-table-empty">Sin verificaciones disponibles</td>
                      </tr>
                    )}
                    {diag?.checks.map((c) => (
                      <tr key={c.name}>
                        <td><code className="db-code">{c.name}</code></td>
                        <td><NativeBiStatusBadge status={c.status} label={c.status} /></td>
                        <td style={{ color: 'var(--c-text-muted)', fontSize: 13 }}>{c.detail ?? '—'}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
            {diag && (
              <div style={{ padding: '8px 16px', borderTop: '1px solid var(--c-border)', fontSize: 11.5, color: 'var(--c-text-faint)' }}>
                Generado: {fmtDatetime(diag.generatedAtUtc)}
              </div>
            )}
          </>
        )}

        {/* ── Conteo por tablas ────────────────────────────────────────────── */}
        {tab === 'tablas' && (
          loadingTables ? (
            <NbLoadingSkeleton rows={1} height={180} />
          ) : (
            <div className="nb-table-scroll">
              <table className="db-table">
                <thead>
                  <tr>
                    <th>Schema</th>
                    <th>Tabla</th>
                    <th style={{ textAlign: 'right' }}>Filas</th>
                    <th>Transformado</th>
                  </tr>
                </thead>
                <tbody>
                  {!tableCounts?.tables.length ? (
                    <tr>
                      <td colSpan={4} className="db-table-empty">Sin datos</td>
                    </tr>
                  ) : (
                    tableCounts.tables.map((t) => (
                      <tr key={`${t.schema}.${t.tableName}`}>
                        <td><span className="db-badge db-badge--info">{t.schema}</span></td>
                        <td><code className="db-code">{t.tableName}</code></td>
                        <td style={{ textAlign: 'right', fontVariantNumeric: 'tabular-nums' }}>
                          {t.rowCount.toLocaleString('es-CL')}
                        </td>
                        <td style={{ color: 'var(--c-text-muted)', fontSize: 13 }}>
                          {fmtDatetime(t.transformedAtUtc)}
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          )
        )}

        {/* ── Extracción ───────────────────────────────────────────────────── */}
        {tab === 'extraccion' && (
          loadingObjects ? (
            <NbLoadingSkeleton rows={6} height={44} />
          ) : !syncObjects || syncObjects.length === 0 ? (
            <NbEmptyState
              message="Sin datos de extracción disponibles. Disponible al completar la primera carga desde SAP."
              icon="table"
            />
          ) : (
            <div className="nb-table-scroll">
              <table className="db-table">
                <thead>
                  <tr>
                    <th>Objeto SAP</th>
                    <th>Estado</th>
                    <th style={{ textAlign: 'right' }}>Filas ingestadas</th>
                    <th>Watermark</th>
                    <th>Último run exitoso</th>
                  </tr>
                </thead>
                <tbody>
                  {syncObjects.map((obj) => (
                    <tr key={obj.sapObject}>
                      <td><code className="db-code" style={{ fontWeight: 700 }}>{obj.sapObject}</code></td>
                      <td>
                        <span
                          style={{
                            display: 'inline-block',
                            padding: '2px 8px',
                            borderRadius: 4,
                            fontSize: 11.5,
                            fontWeight: 600,
                            color: '#fff',
                            backgroundColor: OBJECT_STATUS_COLOR[obj.status] ?? '#94A3B8',
                          }}
                        >
                          {obj.status.toUpperCase()}
                        </span>
                      </td>
                      <td style={{ textAlign: 'right', fontVariantNumeric: 'tabular-nums' }}>
                        {obj.totalRowsIngested.toLocaleString('es-CL')}
                      </td>
                      <td style={{ color: 'var(--c-text-muted)', fontSize: 13 }}>{obj.watermarkDate ?? '—'}</td>
                      <td style={{ color: 'var(--c-text-muted)', fontSize: 13 }}>
                        {fmtDatetime(obj.lastSuccessfulRunUtc)}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )
        )}

        {/* ── Consistencia ─────────────────────────────────────────────────── */}
        {tab === 'consistencia' && (
          loadingTransform ? (
            <NbLoadingSkeleton rows={5} height={44} />
          ) : !transformStatus || transformStatus.martTables.length === 0 ? (
            <NbEmptyState
              message="Sin datos de consistencia disponibles. Disponible al completar la carga histórica."
              icon="table"
            />
          ) : (
            <div>
              {(transformStatus.martTransformedAtUtc || transformStatus.stgTransformedAtUtc) && (
                <div style={{ padding: '12px 20px', borderBottom: '1px solid var(--c-border)', display: 'flex', gap: 32, flexWrap: 'wrap' }}>
                  <div style={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
                    <span style={{ fontSize: 11.5, color: 'var(--c-text-muted)', fontWeight: 500 }}>Último transform STG</span>
                    <span style={{ fontSize: 13, fontVariantNumeric: 'tabular-nums' }}>{fmtDatetime(transformStatus.stgTransformedAtUtc)}</span>
                  </div>
                  <div style={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
                    <span style={{ fontSize: 11.5, color: 'var(--c-text-muted)', fontWeight: 500 }}>Último transform MART</span>
                    <span style={{ fontSize: 13, fontVariantNumeric: 'tabular-nums' }}>{fmtDatetime(transformStatus.martTransformedAtUtc)}</span>
                  </div>
                </div>
              )}
              <div className="nb-table-scroll">
                <table className="db-table">
                  <thead>
                    <tr>
                      <th>Tabla MART</th>
                      <th style={{ textAlign: 'right' }}>Filas</th>
                      <th>Transformado</th>
                    </tr>
                  </thead>
                  <tbody>
                    {transformStatus.martTables.map((t) => (
                      <tr key={t.tableName}>
                        <td><code className="db-code">{t.tableName}</code></td>
                        <td style={{ textAlign: 'right', fontVariantNumeric: 'tabular-nums' }}>
                          {t.rowCount.toLocaleString('es-CL')}
                        </td>
                        <td style={{ color: 'var(--c-text-muted)', fontSize: 13 }}>
                          {fmtDatetime(t.transformedAtUtc)}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          )
        )}
      </div>
    </div>
  )
}
