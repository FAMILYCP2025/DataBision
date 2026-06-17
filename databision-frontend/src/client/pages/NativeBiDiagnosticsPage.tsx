import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useNativeBiDiagnostics, useNativeBiTableCounts } from '../hooks/useNativeBiDiagnostics'
import { useSyncObjects, useSyncTransformStatus } from '../hooks/useNativeBiSync'
import { useClientAuthStore } from '../store/useClientAuthStore'
import NativeBiPageHeader from '../components/nativebi/NativeBiPageHeader'
import NativeBiStatusBadge from '../components/nativebi/NativeBiStatusBadge'
import NativeBiInfoBanner from '../components/nativebi/NativeBiInfoBanner'
import { NbLoadingSkeleton, NbErrorState, NbEmptyState } from '../components/nativebi/NativeBiState'
import type { SyncStatusLevel } from '../types/nativeBi'

function fmtDatetime(iso: string | null) {
  if (!iso) return '—'
  return new Date(iso).toLocaleString('es-CL')
}

function daysSince(iso: string | null): number | null {
  if (!iso) return null
  const diff = Date.now() - new Date(iso).getTime()
  return Math.floor(diff / (1000 * 60 * 60 * 24))
}

const STATUS_COLOR: Record<SyncStatusLevel, string> = {
  ok:      '#16A34A',
  warning: '#D97706',
  error:   '#DC2626',
  unknown: '#94A3B8',
}

const OBJECT_STATUS_COLOR: Record<string, string> = {
  ok:      '#16A34A',
  warning: '#D97706',
  no_data: '#94A3B8',
}

type Tab = 'sistema' | 'tablas' | 'extraccion' | 'consistencia'

const tabs: { id: Tab; label: string }[] = [
  { id: 'sistema',      label: 'Sistema' },
  { id: 'tablas',       label: 'Conteo por tablas' },
  { id: 'extraccion',   label: 'Extracción' },
  { id: 'consistencia', label: 'Consistencia' },
]

const KNOWN_SCHEMAS = ['stg', 'mart', 'ops', 'cfg'] as const
const SCHEMA_LABEL: Record<string, string> = {
  stg:  'STG — Staging',
  mart: 'MART — Analytícs',
  ops:  'OPS — Operaciones',
  cfg:  'CFG — Config',
}

export default function NativeBiDiagnosticsPage() {
  const navigate = useNavigate()
  const user     = useClientAuthStore((s) => s.user)
  const isAdmin  = user?.role === 'CompanyAdmin'
  const [tab, setTab] = useState<Tab>('sistema')

  const {
    data: diag, isLoading: loadingDiag, isError: errDiag, error: diagErr, refetch: refetchDiag,
  } = useNativeBiDiagnostics()
  const { data: tableCounts, isLoading: loadingTables } = useNativeBiTableCounts()
  const { data: syncObjects,    isLoading: loadingObjects }   = useSyncObjects()
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
          <button className="db-btn db-btn--ghost" style={{ marginTop: 16 }} onClick={() => navigate('/client/bi/dashboard')}>
            Volver al dashboard
          </button>
        </div>
      </div>
    )
  }

  // Computed for tabs badge
  const errorChecks   = diag?.checks.filter((c) => c.status === 'error').length ?? 0
  const warningChecks = diag?.checks.filter((c) => c.status === 'warning').length ?? 0

  // Schema grouping for tables
  const allTables = tableCounts?.tables ?? []
  const tablesBySchema = allTables.reduce<Record<string, typeof allTables>>((acc, t) => {
    const s = t.schema ?? 'unknown';
    (acc[s] = acc[s] ?? []).push(t)
    return acc
  }, {})
  const schemaOrder = [...KNOWN_SCHEMAS, ...Object.keys(tablesBySchema).filter((s) => !KNOWN_SCHEMAS.includes(s as never))]

  // Extraction completeness
  const totalObjects   = syncObjects?.length ?? 0
  const okObjects      = syncObjects?.filter((o) => o.status === 'ok').length ?? 0
  const totalIngested  = syncObjects?.reduce((s, o) => s + o.totalRowsIngested, 0) ?? 0

  // Consistency freshness
  const martDays  = daysSince(transformStatus?.martTransformedAtUtc ?? null)
  const stgDays   = daysSince(transformStatus?.stgTransformedAtUtc ?? null)
  const hasData   = (transformStatus?.martTables ?? []).some((t) => t.rowCount > 0)
  const emptyTbls = (transformStatus?.martTables ?? []).filter((t) => t.rowCount === 0).length

  return (
    <div className="cp-page">
      <NativeBiPageHeader
        title="Diagnósticos"
        description="Estado del pipeline de datos — vista técnica"
        actions={
          <button className="db-btn db-btn--ghost db-btn--sm" aria-label="Actualizar diagnósticos" onClick={() => void refetchDiag()}>
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
                padding: '0 16px', height: 44, background: 'none', border: 'none',
                borderBottom: tab === t.id ? '2px solid var(--brand-primary, #2563EB)' : '2px solid transparent',
                color: tab === t.id ? 'var(--brand-primary, #2563EB)' : 'var(--c-text-muted)',
                fontWeight: tab === t.id ? 600 : 500, fontSize: 13.5,
                cursor: 'pointer', fontFamily: 'inherit', marginBottom: -1,
                transition: 'color 150ms, border-color 150ms', whiteSpace: 'nowrap',
              }}
            >
              {t.label}
              {t.id === 'sistema' && diag && (
                <span style={{
                  marginLeft: 6, display: 'inline-block', width: 8, height: 8,
                  borderRadius: '50%', backgroundColor: STATUS_COLOR[diag.status] ?? '#94A3B8',
                  verticalAlign: 'middle',
                }} />
              )}
              {t.id === 'sistema' && (errorChecks + warningChecks) > 0 && (
                <span style={{
                  marginLeft: 4, display: 'inline-block', minWidth: 18, height: 18,
                  borderRadius: 9, backgroundColor: errorChecks > 0 ? '#DC2626' : '#D97706',
                  color: '#fff', fontSize: 11, fontWeight: 700, lineHeight: '18px',
                  textAlign: 'center', padding: '0 4px', verticalAlign: 'middle',
                }}>
                  {errorChecks + warningChecks}
                </span>
              )}
            </button>
          ))}
        </div>

        {/* ── Sistema ─────────────────────────────────────────────────────── */}
        {tab === 'sistema' && (
          <>
            {/* Overall status header */}
            {diag && (
              <div style={{ padding: '14px 20px', borderBottom: '1px solid var(--c-border)', display: 'flex', alignItems: 'center', gap: 16, flexWrap: 'wrap' }}>
                <NativeBiStatusBadge status={diag.status} />
                <div style={{ display: 'flex', gap: 20 }}>
                  {errorChecks > 0 && (
                    <span style={{ fontSize: 13, color: '#DC2626', fontWeight: 600 }}>{errorChecks} error(es)</span>
                  )}
                  {warningChecks > 0 && (
                    <span style={{ fontSize: 13, color: '#D97706', fontWeight: 600 }}>{warningChecks} advertencia(s)</span>
                  )}
                  {errorChecks === 0 && warningChecks === 0 && (
                    <span style={{ fontSize: 13, color: '#16A34A', fontWeight: 600 }}>Todos los checks OK</span>
                  )}
                  <span style={{ fontSize: 12.5, color: 'var(--c-text-faint)' }}>Generado: {fmtDatetime(diag.generatedAtUtc)}</span>
                </div>
              </div>
            )}

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
                      <tr key={c.name} style={{ backgroundColor: c.status === 'error' ? '#FEF2F2' : c.status === 'warning' ? '#FFFBEB' : 'transparent' }}>
                        <td><code className="db-code">{c.name}</code></td>
                        <td><NativeBiStatusBadge status={c.status} label={c.status} /></td>
                        <td style={{ color: 'var(--c-text-muted)', fontSize: 13 }}>{c.detail ?? '—'}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </>
        )}

        {/* ── Conteo por tablas ────────────────────────────────────────────── */}
        {tab === 'tablas' && (
          loadingTables ? (
            <NbLoadingSkeleton rows={1} height={180} />
          ) : !tableCounts?.tables.length ? (
            <NbEmptyState message="Sin conteos de tablas disponibles. Disponible al completar primera carga." icon="table" />
          ) : (
            <div style={{ padding: '16px 20px' }}>
              {/* Schema summary row */}
              <div style={{ display: 'flex', gap: 12, flexWrap: 'wrap', marginBottom: 20 }}>
                {schemaOrder.filter((s) => tablesBySchema[s]).map((schema) => {
                  const rows = tablesBySchema[schema]
                  const totalRows = rows.reduce((s, t) => s + t.rowCount, 0)
                  const emptyCount = rows.filter((t) => t.rowCount === 0).length
                  return (
                    <div key={schema} style={{ padding: '10px 16px', backgroundColor: 'var(--c-bg)', border: '1px solid var(--c-border)', borderRadius: 6 }}>
                      <div style={{ fontSize: 11.5, fontWeight: 700, color: 'var(--c-text-muted)', textTransform: 'uppercase', marginBottom: 4 }}>
                        {SCHEMA_LABEL[schema] ?? schema.toUpperCase()}
                      </div>
                      <div style={{ fontSize: 13.5, fontWeight: 600, fontVariantNumeric: 'tabular-nums' }}>
                        {totalRows.toLocaleString('es-CL')} filas
                      </div>
                      <div style={{ fontSize: 11.5, color: 'var(--c-text-faint)', marginTop: 2 }}>
                        {rows.length} tabla(s){emptyCount > 0 ? ` · ${emptyCount} vacía(s)` : ''}
                      </div>
                    </div>
                  )
                })}
              </div>

              {/* Grouped table listing */}
              {schemaOrder.filter((s) => tablesBySchema[s]).map((schema) => (
                <div key={schema} style={{ marginBottom: 24 }}>
                  <div style={{ fontSize: 12, fontWeight: 600, color: 'var(--c-text-muted)', textTransform: 'uppercase', letterSpacing: '0.04em', marginBottom: 8, padding: '0 4px' }}>
                    {SCHEMA_LABEL[schema] ?? schema}
                  </div>
                  <div className="nb-table-scroll">
                    <table className="db-table">
                      <thead>
                        <tr>
                          <th>Tabla</th>
                          <th style={{ textAlign: 'right' }}>Filas</th>
                          <th>Transformado</th>
                        </tr>
                      </thead>
                      <tbody>
                        {tablesBySchema[schema].map((t) => (
                          <tr key={`${t.schema}.${t.tableName}`} style={{ backgroundColor: t.rowCount === 0 ? '#FEF2F2' : 'transparent' }}>
                            <td>
                              <code className="db-code">{t.tableName}</code>
                              {t.rowCount === 0 && (
                                <span style={{ marginLeft: 8, fontSize: 11, color: '#DC2626', fontWeight: 600 }}>VACÍA</span>
                              )}
                            </td>
                            <td style={{ textAlign: 'right', fontVariantNumeric: 'tabular-nums', color: t.rowCount === 0 ? '#DC2626' : 'inherit', fontWeight: t.rowCount === 0 ? 600 : 400 }}>
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
              ))}
            </div>
          )
        )}

        {/* ── Extracción ───────────────────────────────────────────────────── */}
        {tab === 'extraccion' && (
          loadingObjects ? (
            <NbLoadingSkeleton rows={6} height={44} />
          ) : !syncObjects || syncObjects.length === 0 ? (
            <NbEmptyState
              message="Sin datos de extracción. Disponible al completar la primera carga desde SAP."
              icon="table"
            />
          ) : (
            <>
              {/* Completeness header */}
              <div style={{ padding: '12px 20px', borderBottom: '1px solid var(--c-border)', display: 'flex', gap: 24, flexWrap: 'wrap', alignItems: 'center' }}>
                <div>
                  <span style={{ fontSize: 11.5, color: 'var(--c-text-muted)', fontWeight: 600, display: 'block', marginBottom: 2 }}>Objetos activos</span>
                  <span style={{ fontSize: 15, fontWeight: 700, color: okObjects === totalObjects ? '#16A34A' : '#D97706' }}>
                    {okObjects} / {totalObjects}
                  </span>
                </div>
                <div>
                  <span style={{ fontSize: 11.5, color: 'var(--c-text-muted)', fontWeight: 600, display: 'block', marginBottom: 2 }}>Total filas ingestadas</span>
                  <span style={{ fontSize: 15, fontWeight: 700, fontVariantNumeric: 'tabular-nums' }}>{totalIngested.toLocaleString('es-CL')}</span>
                </div>
                <div style={{ flex: 1, minWidth: 120 }}>
                  <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: 11.5, color: 'var(--c-text-muted)', marginBottom: 4 }}>
                    <span>Completitud</span>
                    <span>{totalObjects > 0 ? Math.round((okObjects / totalObjects) * 100) : 0}%</span>
                  </div>
                  <div style={{ height: 6, backgroundColor: 'var(--c-border)', borderRadius: 3 }}>
                    <div style={{
                      width: `${totalObjects > 0 ? (okObjects / totalObjects) * 100 : 0}%`,
                      height: '100%', borderRadius: 3,
                      backgroundColor: okObjects === totalObjects ? '#16A34A' : '#D97706',
                      transition: 'width 400ms ease',
                    }} />
                  </div>
                </div>
              </div>
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
                      <tr key={obj.sapObject} style={{ backgroundColor: obj.status === 'no_data' ? '#FFFBEB' : 'transparent' }}>
                        <td><code className="db-code" style={{ fontWeight: 700 }}>{obj.sapObject}</code></td>
                        <td>
                          <span style={{
                            display: 'inline-block', padding: '2px 8px', borderRadius: 4,
                            fontSize: 11.5, fontWeight: 600, color: '#fff',
                            backgroundColor: OBJECT_STATUS_COLOR[obj.status] ?? '#94A3B8',
                          }}>
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
            </>
          )
        )}

        {/* ── Consistencia ─────────────────────────────────────────────────── */}
        {tab === 'consistencia' && (
          loadingTransform ? (
            <NbLoadingSkeleton rows={5} height={44} />
          ) : !transformStatus || transformStatus.martTables.length === 0 ? (
            <NbEmptyState
              message="Sin datos de consistencia. Disponible al completar la carga histórica."
              icon="table"
            />
          ) : (
            <div>
              {/* Freshness header */}
              <div style={{ padding: '12px 20px', borderBottom: '1px solid var(--c-border)', display: 'flex', gap: 28, flexWrap: 'wrap', alignItems: 'center' }}>
                {[
                  { label: 'Último transform STG', ts: transformStatus.stgTransformedAtUtc, days: stgDays },
                  { label: 'Último transform MART', ts: transformStatus.martTransformedAtUtc, days: martDays },
                ].map((item) => (
                  <div key={item.label}>
                    <span style={{ fontSize: 11.5, color: 'var(--c-text-muted)', fontWeight: 500, display: 'block', marginBottom: 2 }}>{item.label}</span>
                    <span style={{ fontSize: 13.5, fontVariantNumeric: 'tabular-nums' }}>{fmtDatetime(item.ts)}</span>
                    {item.days !== null && (
                      <span style={{ fontSize: 11.5, marginLeft: 8, color: item.days > 3 ? '#D97706' : '#16A34A', fontWeight: 600 }}>
                        ({item.days}d atrás)
                      </span>
                    )}
                  </div>
                ))}
                <div>
                  <span style={{ fontSize: 11.5, color: 'var(--c-text-muted)', fontWeight: 500, display: 'block', marginBottom: 2 }}>Tablas con datos</span>
                  <span style={{ fontSize: 13.5, fontWeight: 600, color: hasData ? '#16A34A' : '#D97706' }}>
                    {transformStatus.martTables.filter((t) => t.rowCount > 0).length} / {transformStatus.martTables.length}
                  </span>
                </div>
              </div>

              {/* Recommendations */}
              {emptyTbls > 0 && (
                <div style={{ padding: '12px 20px', borderBottom: '1px solid var(--c-border)' }}>
                  <NativeBiInfoBanner
                    variant="warning"
                    title={`${emptyTbls} tabla(s) MART sin datos`}
                    message="Verificar que el extractor SAP completó la carga de los objetos requeridos para este módulo."
                  />
                </div>
              )}

              <div className="nb-table-scroll">
                <table className="db-table">
                  <thead>
                    <tr>
                      <th>Tabla MART</th>
                      <th style={{ textAlign: 'right' }}>Filas</th>
                      <th>Estado</th>
                      <th>Transformado</th>
                    </tr>
                  </thead>
                  <tbody>
                    {transformStatus.martTables.map((t) => (
                      <tr key={t.tableName} style={{ backgroundColor: t.rowCount === 0 ? '#FFFBEB' : 'transparent' }}>
                        <td><code className="db-code">{t.tableName}</code></td>
                        <td style={{ textAlign: 'right', fontVariantNumeric: 'tabular-nums', color: t.rowCount === 0 ? '#D97706' : 'inherit', fontWeight: t.rowCount === 0 ? 600 : 400 }}>
                          {t.rowCount.toLocaleString('es-CL')}
                        </td>
                        <td>
                          {t.rowCount > 0 ? (
                            <span style={{ fontSize: 12, color: '#16A34A', fontWeight: 600 }}>Con datos</span>
                          ) : (
                            <span style={{ fontSize: 12, color: '#D97706', fontWeight: 600 }}>Vacía</span>
                          )}
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
