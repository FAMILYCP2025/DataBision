import React, { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { 
  getCompanyModules, 
  getCompanyReports, 
  updateCompanyReportStatus 
} from '../api/adminApi'
import { statusBadge } from './Badge'
import CreateEditReportModal from './CreateEditReportModal'

export default function CompanyReportsList({ companyId }: { companyId: number }) {
  const queryClient = useQueryClient()
  const [selectedModuleId, setSelectedModuleId] = useState<number | null>(null)
  
  // For Create/Edit modal
  const [modalOpen, setModalOpen] = useState(false)
  const [editingReportId, setEditingReportId] = useState<number | null>(null)

  const { data: modules = [], isLoading: loadingModules } = useQuery({
    queryKey: ['admin', 'companies', companyId, 'modules'],
    queryFn: () => getCompanyModules(companyId),
  })

  // Auto-select first module when loaded
  React.useEffect(() => {
    if (modules.length > 0 && !selectedModuleId) {
      // eslint-disable-next-line react-hooks/set-state-in-effect
      setSelectedModuleId(modules[0].id)
    }
  }, [modules, selectedModuleId])

  const { data: reports = [], isLoading: loadingReports } = useQuery({
    queryKey: ['admin', 'companies', companyId, 'modules', selectedModuleId, 'reports'],
    queryFn: () => getCompanyReports(companyId, selectedModuleId!),
    enabled: !!selectedModuleId,
  })

  const toggleStatusMutation = useMutation({
    mutationFn: ({ reportId, isActive }: { reportId: number, isActive: boolean }) => 
      updateCompanyReportStatus(companyId, reportId, isActive),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'companies', companyId, 'modules', selectedModuleId, 'reports'] })
    }
  })

  if (loadingModules) {
    return <div className="db-spinner" />
  }

  return (
    <div className="db-card">
      <div className="db-card-header" style={{ flexDirection: 'column', alignItems: 'flex-start', gap: '1rem' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', width: '100%', alignItems: 'center' }}>
          <h2 className="db-card-title">Reportes configurados</h2>
          <button 
            className="db-btn db-btn--primary db-btn--sm"
            onClick={() => {
              setEditingReportId(null)
              setModalOpen(true)
            }}
            disabled={!selectedModuleId}
          >
            Nuevo Reporte
          </button>
        </div>
        
        {/* Module tabs */}
        <div style={{ display: 'flex', gap: '8px', overflowX: 'auto', paddingBottom: '4px', width: '100%' }}>
          {modules.map(m => (
            <button
              key={m.id}
              onClick={() => setSelectedModuleId(m.id)}
              className={`db-btn db-btn--sm ${selectedModuleId === m.id ? 'db-btn--primary' : 'db-btn--ghost'}`}
            >
              {m.name}
            </button>
          ))}
        </div>
      </div>

      <div className="db-table-container">
        {loadingReports ? (
          <div style={{ padding: '2rem', textAlign: 'center' }}>
            <span className="db-spinner" />
          </div>
        ) : reports.length === 0 ? (
          <div className="db-empty-state" style={{ padding: '3rem 1rem' }}>
            <p>No hay reportes en este módulo.</p>
          </div>
        ) : (
          <table className="db-table">
            <thead>
              <tr>
                <th>Nombre</th>
                <th>Workspace ID</th>
                <th>Report ID</th>
                <th>Estado</th>
                <th className="db-text-right">Acciones</th>
              </tr>
            </thead>
            <tbody>
              {reports.map((r) => (
                <tr key={r.id}>
                  <td>
                    <strong>{r.name}</strong>
                    {r.description && <div style={{ fontSize: '0.85rem', color: 'var(--color-text-light)' }}>{r.description}</div>}
                  </td>
                  <td><code className="db-code" style={{ fontSize: '0.8rem' }}>{r.workspaceId || '-'}</code></td>
                  <td><code className="db-code" style={{ fontSize: '0.8rem' }}>{r.reportId || '-'}</code></td>
                  <td>{statusBadge(r.isActive ? 'Active' : 'Inactive')}</td>
                  <td className="db-text-right">
                    <button
                      className="db-btn db-btn--ghost db-btn--sm"
                      onClick={() => {
                        setEditingReportId(r.id)
                        setModalOpen(true)
                      }}
                    >
                      Editar
                    </button>
                    <button
                      className="db-btn db-btn--ghost db-btn--sm"
                      onClick={() => toggleStatusMutation.mutate({ reportId: r.id, isActive: !r.isActive })}
                    >
                      {r.isActive ? 'Desactivar' : 'Activar'}
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {modalOpen && selectedModuleId && (
        <CreateEditReportModal
          companyId={companyId}
          moduleId={selectedModuleId}
          reportId={editingReportId}
          onClose={() => setModalOpen(false)}
        />
      )}
    </div>
  )
}
