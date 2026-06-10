import { useNavigate } from 'react-router-dom'
import { useNativeBiDiagnostics, useNativeBiTableCounts } from '../hooks/useNativeBiDiagnostics'
import { useClientAuthStore } from '../store/useClientAuthStore'
import NativeBiPageHeader from '../components/nativebi/NativeBiPageHeader'
import NativeBiStatusBadge from '../components/nativebi/NativeBiStatusBadge'
import { NbLoadingSkeleton, NbErrorState } from '../components/nativebi/NativeBiState'

function fmtDatetime(iso: string | null) {
  if (!iso) return '—'
  return new Date(iso).toLocaleString('es-CL')
}

export default function NativeBiDiagnosticsPage() {
  const navigate = useNavigate()
  const user = useClientAuthStore((s) => s.user)
  const isAdmin = user?.role === 'CompanyAdmin'

  const {
    data: diag,
    isLoading: loadingDiag,
    isError: errDiag,
    error: diagErr,
    refetch: refetchDiag,
  } = useNativeBiDiagnostics()

  const { data: tableCounts, isLoading: loadingTables } = useNativeBiTableCounts()

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

      {/* Health checks */}
      <div className="db-card">
        <div className="db-card-header">
          <span className="db-card-title">Verificaciones del sistema</span>
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
                    <td colSpan={3} className="db-table-empty">
                      Sin verificaciones disponibles
                    </td>
                  </tr>
                )}
                {diag?.checks.map((c) => (
                  <tr key={c.name}>
                    <td>
                      <code className="db-code">{c.name}</code>
                    </td>
                    <td>
                      <NativeBiStatusBadge status={c.status} label={c.status} />
                    </td>
                    <td style={{ color: 'var(--c-text-muted)', fontSize: 13 }}>
                      {c.detail ?? '—'}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        {diag && (
          <div
            style={{
              padding: '8px 16px',
              borderTop: '1px solid var(--c-border)',
              fontSize: 11.5,
              color: 'var(--c-text-faint)',
            }}
          >
            Generado: {fmtDatetime(diag.generatedAtUtc)}
          </div>
        )}
      </div>

      {/* Table row counts */}
      <div className="db-card">
        <div className="db-card-header">
          <span className="db-card-title">Conteo de filas por tabla</span>
        </div>

        {loadingTables ? (
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
                    <td colSpan={4} className="db-table-empty">
                      Sin datos
                    </td>
                  </tr>
                ) : (
                  tableCounts.tables.map((t) => (
                    <tr key={`${t.schema}.${t.tableName}`}>
                      <td>
                        <span className="db-badge db-badge--info">{t.schema}</span>
                      </td>
                      <td>
                        <code className="db-code">{t.tableName}</code>
                      </td>
                      <td
                        style={{
                          textAlign: 'right',
                          fontVariantNumeric: 'tabular-nums',
                        }}
                      >
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
        )}
      </div>
    </div>
  )
}
