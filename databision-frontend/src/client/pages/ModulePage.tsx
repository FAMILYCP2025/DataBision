import { useParams, Link } from 'react-router-dom'
import { useClientModules, useModuleReports } from '../hooks/useClientData'

export default function ModulePage() {
  const { moduleSlug } = useParams<{ moduleSlug: string }>()
  const { data: modules } = useClientModules()
  const { data: reports, isLoading, isError } = useModuleReports(moduleSlug)

  const currentModule = modules?.find((m) => m.slug === moduleSlug)

  if (isLoading) {
    return (
      <div className="cp-page">
        <div className="cp-page-header">
          <div>
            <div className="cp-skeleton cp-skeleton--title" />
            <div className="cp-skeleton cp-skeleton--sub" />
          </div>
        </div>
        <div className="cp-reports-grid">
          {[1, 2, 3].map((i) => (
            <div key={i} className="cp-report-card cp-report-card--skeleton" />
          ))}
        </div>
      </div>
    )
  }

  if (isError) {
    return (
      <div className="cp-page">
        <div className="cp-empty-state">
          <div className="cp-empty-icon">
            <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
              <circle cx="12" cy="12" r="10" /><line x1="12" y1="8" x2="12" y2="12" /><line x1="12" y1="16" x2="12.01" y2="16" />
            </svg>
          </div>
          <h3>No se pudieron cargar los informes</h3>
          <p>Intenta de nuevo o contacta al administrador.</p>
        </div>
      </div>
    )
  }

  if (!reports || reports.length === 0) {
    return (
      <div className="cp-page">
        <div className="cp-page-header">
          <div>
            <h1 className="cp-page-title">{currentModule?.name ?? moduleSlug}</h1>
            {currentModule?.description && (
              <p className="cp-page-subtitle">{currentModule.description}</p>
            )}
          </div>
        </div>
        <div className="cp-empty-state">
          <div className="cp-empty-icon">
            <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
              <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" /><polyline points="14 2 14 8 20 8" />
            </svg>
          </div>
          <h3>Sin informes disponibles</h3>
          <p>No tienes informes asignados para este módulo. Contacta al administrador de tu empresa.</p>
        </div>
      </div>
    )
  }

  return (
    <div className="cp-page">
      {/* Page header */}
      <div className="cp-page-header">
        <div>
          <h1 className="cp-page-title">{currentModule?.name ?? moduleSlug}</h1>
          {currentModule?.description && (
            <p className="cp-page-subtitle">{currentModule.description}</p>
          )}
        </div>
        <div className="cp-page-header-meta">
          <span className="cp-badge-count">{reports.length} informe{reports.length !== 1 ? 's' : ''}</span>
        </div>
      </div>

      {/* Reports grid */}
      <div className="cp-reports-grid">
        {reports.map((report) => (
          <Link
            key={report.id}
            to={`/client/modules/${moduleSlug}/reports/${report.id}`}
            className="cp-report-card"
          >
            <div className="cp-report-card-icon">
              <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round">
                <rect x="2" y="3" width="20" height="14" rx="2" ry="2" />
                <line x1="8" y1="21" x2="16" y2="21" />
                <line x1="12" y1="17" x2="12" y2="21" />
              </svg>
            </div>
            <div className="cp-report-card-body">
              <h3 className="cp-report-card-title">{report.name}</h3>
              {report.description && (
                <p className="cp-report-card-desc">{report.description}</p>
              )}
            </div>
            <div className="cp-report-card-footer">
              {report.lastUpdated && (
                <span className="cp-report-card-date">
                  <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                    <rect x="3" y="4" width="18" height="18" rx="2" ry="2" /><line x1="16" y1="2" x2="16" y2="6" /><line x1="8" y1="2" x2="8" y2="6" /><line x1="3" y1="10" x2="21" y2="10" />
                  </svg>
                  {report.lastUpdated}
                </span>
              )}
              <span className="cp-report-card-arrow">
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                  <polyline points="9 18 15 12 9 6" />
                </svg>
              </span>
            </div>
          </Link>
        ))}
      </div>
    </div>
  )
}
