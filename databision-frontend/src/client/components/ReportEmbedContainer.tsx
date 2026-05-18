import { useEmbedConfig } from '../hooks/useClientData'
import type { ClientReport } from '../api/clientApi'

interface Props {
  report: ClientReport
}

// ── State: Loading ─────────────────────────────────────────────────────────
function EmbedSkeleton() {
  return (
    <div className="cp-report-embed">
      <div className="cp-report-embed__skeleton">
        <div className="cp-skeleton cp-skeleton--embed-header" />
        <div className="cp-skeleton cp-skeleton--embed-body" />
      </div>
    </div>
  )
}

// ── State: Forbidden (403) ─────────────────────────────────────────────────
function EmbedForbidden() {
  return (
    <div className="cp-report-embed cp-report-embed--state">
      <div className="cp-report-embed__icon cp-report-embed__icon--denied">
        <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
          <circle cx="12" cy="12" r="10" />
          <line x1="4.93" y1="4.93" x2="19.07" y2="19.07" />
        </svg>
      </div>
      <h3 className="cp-report-embed__title">Acceso denegado</h3>
      <p className="cp-report-embed__desc">No tienes permiso para ver este informe. Contacta a tu administrador.</p>
    </div>
  )
}

// ── State: Not configured (501) ────────────────────────────────────────────
function EmbedNotConfigured({ report }: { report: ClientReport }) {
  return (
    <div className="cp-report-embed cp-report-embed--state">
      <div className="cp-report-embed__icon cp-report-embed__icon--pending">
        <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
          <rect x="2" y="3" width="20" height="14" rx="2" />
          <line x1="8" y1="21" x2="16" y2="21" />
          <line x1="12" y1="17" x2="12" y2="21" />
        </svg>
      </div>
      <h3 className="cp-report-embed__title">{report.name}</h3>
      {report.description && <p className="cp-report-embed__desc">{report.description}</p>}
      <div className="cp-report-embed__badge cp-report-embed__badge--pending">
        <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
          <circle cx="12" cy="12" r="10" />
          <polyline points="12 6 12 12 16 14" />
        </svg>
        Power BI — configuración en progreso
      </div>
      <div className="cp-report-embed__meta">
        {report.lastUpdated && (
          <span className="cp-report-embed__meta-item">
            <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
              <rect x="3" y="4" width="18" height="18" rx="2" /><line x1="16" y1="2" x2="16" y2="6" /><line x1="8" y1="2" x2="8" y2="6" /><line x1="3" y1="10" x2="21" y2="10" />
            </svg>
            Actualizado {report.lastUpdated}
          </span>
        )}
        <span className="cp-report-embed__meta-item">
          <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
            <path d="M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z" />
          </svg>
          Power BI Embedded
        </span>
      </div>
    </div>
  )
}

// ── State: Error (unexpected) ──────────────────────────────────────────────
function EmbedError({ onRetry }: { onRetry: () => void }) {
  return (
    <div className="cp-report-embed cp-report-embed--state">
      <div className="cp-report-embed__icon cp-report-embed__icon--error">
        <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
          <path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z" />
          <line x1="12" y1="9" x2="12" y2="13" /><line x1="12" y1="17" x2="12.01" y2="17" />
        </svg>
      </div>
      <h3 className="cp-report-embed__title">Error al cargar el informe</h3>
      <p className="cp-report-embed__desc">Ocurrió un error inesperado. Inténtalo de nuevo.</p>
      <button className="cp-btn cp-btn--secondary cp-report-embed__retry" onClick={onRetry}>
        Reintentar
      </button>
    </div>
  )
}

// ── State: Ready (embed config received) ───────────────────────────────────
// Phase 3 will replace this with <PowerBIEmbed /> from powerbi-client-react.
function EmbedReady({ report }: { report: ClientReport }) {
  return (
    <div className="cp-report-embed cp-report-embed--state cp-report-embed--ready">
      <div className="cp-report-embed__icon cp-report-embed__icon--ready">
        <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
          <polyline points="22 12 18 12 15 21 9 3 6 12 2 12" />
        </svg>
      </div>
      <h3 className="cp-report-embed__title">{report.name}</h3>
      {report.description && <p className="cp-report-embed__desc">{report.description}</p>}
      <div className="cp-report-embed__badge cp-report-embed__badge--ready">
        <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
          <polyline points="20 6 9 17 4 12" />
        </svg>
        Power BI — listo para integrar
      </div>
    </div>
  )
}

// ── Main container ─────────────────────────────────────────────────────────
export default function ReportEmbedContainer({ report }: Props) {
  const { data: config, isLoading, isError, error, refetch } = useEmbedConfig(report.id)

  if (isLoading) return <EmbedSkeleton />

  if (isError) {
    const status = (error as { response?: { status?: number } })?.response?.status
    if (status === 403) return <EmbedForbidden />
    if (status === 501) return <EmbedNotConfigured report={report} />
    return <EmbedError onRetry={() => void refetch()} />
  }

  if (config) return <EmbedReady report={report} />

  return <EmbedNotConfigured report={report} />
}
