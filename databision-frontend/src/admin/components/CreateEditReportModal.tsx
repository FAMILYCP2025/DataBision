import { useState, useEffect } from 'react'
import { useMutation, useQueryClient, useQuery } from '@tanstack/react-query'
import { createCompanyReport, updateCompanyReport, getCompanyReports } from '../api/adminApi'

interface Props {
  companyId: number
  moduleId: number
  reportId: number | null
  onClose: () => void
}

export default function CreateEditReportModal({ companyId, moduleId, reportId, onClose }: Props) {
  const queryClient = useQueryClient()
  
  const { data: reports = [] } = useQuery({
    queryKey: ['admin', 'companies', companyId, 'modules', moduleId, 'reports'],
    queryFn: () => getCompanyReports(companyId, moduleId),
    enabled: !!reportId, // only fetch if we're editing
  })
  
  const report = reportId ? reports.find(r => r.id === reportId) : null

  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [workspaceId, setWorkspaceId] = useState('')
  const [pbiReportId, setPbiReportId] = useState('')
  const [datasetId, setDatasetId] = useState('')
  const [embedUrl, setEmbedUrl] = useState('')
  const [isActive, setIsActive] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (report) {
      // eslint-disable-next-line react-hooks/set-state-in-effect
      setName(report.name)
      setDescription(report.description || '')
      setWorkspaceId(report.workspaceId || '')
      setPbiReportId(report.reportId || '')
      setDatasetId(report.datasetId || '')
      setEmbedUrl(report.embedUrl || '')
      setIsActive(report.isActive)
    }
  }, [report])

  const mutation = useMutation({
    mutationFn: () => {
      const payload = {
        name,
        description,
        workspaceId,
        reportId: pbiReportId,
        datasetId,
        embedUrl,
        isActive
      }
      if (reportId) {
        return updateCompanyReport(companyId, reportId, payload)
      } else {
        return createCompanyReport(companyId, moduleId, payload)
      }
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'companies', companyId, 'modules', moduleId, 'reports'] })
      onClose()
    },
    onError: (err: unknown) => {
      const axiosErr = err as { response?: { data?: { message?: string } } }
      setError(axiosErr?.response?.data?.message ?? 'Error al guardar reporte.')
    }
  })

  return (
    <div className="db-modal-overlay">
      <div className="db-modal">
        <div className="db-modal-header">
          <h2 className="db-modal-title">{reportId ? 'Editar Reporte' : 'Nuevo Reporte'}</h2>
          <button className="db-modal-close" onClick={onClose}>×</button>
        </div>
        <div className="db-modal-content">
          <form
            id="report-form"
            className="db-form"
            onSubmit={(e) => {
              e.preventDefault()
              mutation.mutate()
            }}
          >
            <div className="db-field">
              <label className="db-label">Nombre del Reporte</label>
              <input
                type="text"
                className="db-input"
                value={name}
                onChange={(e) => setName(e.target.value)}
                required
              />
            </div>
            
            <div className="db-field">
              <label className="db-label">Descripción</label>
              <textarea
                className="db-input"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                rows={2}
              />
            </div>

            <div className="db-form-grid">
              <div className="db-field">
                <label className="db-label">Workspace ID (Power BI)</label>
                <input
                  type="text"
                  className="db-input"
                  value={workspaceId}
                  onChange={(e) => setWorkspaceId(e.target.value)}
                  placeholder="ej. f3f4..."
                />
              </div>
              <div className="db-field">
                <label className="db-label">Report ID (Power BI)</label>
                <input
                  type="text"
                  className="db-input"
                  value={pbiReportId}
                  onChange={(e) => setPbiReportId(e.target.value)}
                  placeholder="ej. a1b2..."
                />
              </div>
              <div className="db-field">
                <label className="db-label">Dataset ID (Opcional)</label>
                <input
                  type="text"
                  className="db-input"
                  value={datasetId}
                  onChange={(e) => setDatasetId(e.target.value)}
                />
              </div>
              <div className="db-field">
                <label className="db-label">Estado</label>
                <select 
                  className="db-select"
                  value={isActive ? 'true' : 'false'}
                  onChange={(e) => setIsActive(e.target.value === 'true')}
                >
                  <option value="true">Activo</option>
                  <option value="false">Inactivo</option>
                </select>
              </div>
            </div>

            <div className="db-field">
              <label className="db-label">Embed URL</label>
              <input
                type="url"
                className="db-input"
                value={embedUrl}
                onChange={(e) => setEmbedUrl(e.target.value)}
                placeholder="https://app.powerbi.com/reportEmbed?reportId=..."
              />
            </div>

            {error && (
              <div className="db-alert db-alert--error">{error}</div>
            )}
          </form>
        </div>
        <div className="db-modal-footer">
          <button type="button" className="db-btn db-btn--ghost" onClick={onClose}>
            Cancelar
          </button>
          <button
            type="submit"
            form="report-form"
            className="db-btn db-btn--primary"
            disabled={mutation.isPending}
          >
            {mutation.isPending ? 'Guardando...' : 'Guardar Reporte'}
          </button>
        </div>
      </div>
    </div>
  )
}
