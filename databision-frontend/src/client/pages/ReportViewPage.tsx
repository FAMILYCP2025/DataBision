import { useParams, Link } from 'react-router-dom'
import { useReportById } from '../hooks/useClientData'
import ReportEmbedContainer from '../components/ReportEmbedContainer'

export default function ReportViewPage() {
  const { moduleSlug, reportId } = useParams<{ moduleSlug: string; reportId: string }>()
  const { data: report, isLoading, isError } = useReportById(moduleSlug, reportId ? Number(reportId) : undefined)

  return (
    <div className="cp-page">
      {/* Back link */}
      <Link to={`/client/modules/${moduleSlug}`} className="cp-back-link">
        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
          <polyline points="15 18 9 12 15 6" />
        </svg>
        Volver al módulo
      </Link>

      {isLoading && (
        <div className="cp-page-header">
          <div className="cp-skeleton cp-skeleton--title" />
          <div className="cp-skeleton cp-skeleton--sub" />
        </div>
      )}

      {isError && (
        <div className="cp-empty-state">
          <div className="cp-empty-icon">
            <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
              <circle cx="12" cy="12" r="10" /><line x1="12" y1="8" x2="12" y2="12" /><line x1="12" y1="16" x2="12.01" y2="16" />
            </svg>
          </div>
          <h3>Informe no disponible</h3>
          <p>No tienes acceso a este informe o ya no está disponible.</p>
        </div>
      )}

      {report && (
        <>
          <div className="cp-page-header">
            <div>
              <h1 className="cp-page-title">{report.name}</h1>
              {report.description && (
                <p className="cp-page-subtitle">{report.description}</p>
              )}
            </div>
          </div>
          <ReportEmbedContainer report={report} />
        </>
      )}

      {!isLoading && !isError && !report && (
        <div className="cp-empty-state">
          <div className="cp-empty-icon">
            <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
              <circle cx="12" cy="12" r="10" /><line x1="12" y1="8" x2="12" y2="12" /><line x1="12" y1="16" x2="12.01" y2="16" />
            </svg>
          </div>
          <h3>Informe no disponible</h3>
          <p>No tienes acceso a este informe o ya no está disponible.</p>
        </div>
      )}
    </div>
  )
}
