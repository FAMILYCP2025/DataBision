import React from 'react'
import { useParams, Link, useNavigate } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { getCompanies, updateCompany } from '../api/adminApi'
import { statusBadge } from '../components/Badge'
import { format } from 'date-fns'
import CompanyReportsList from '../components/CompanyReportsList'

export default function CompanyDetailPage() {
  const { id } = useParams<{ id: string }>()
  const companyId = Number(id)
  const navigate = useNavigate()
  const queryClient = useQueryClient()

  const { data: companies = [], isLoading } = useQuery({
    queryKey: ['admin', 'companies'],
    queryFn: getCompanies,
  })

  const company = companies.find((c) => c.id === companyId)

  const [editing, setEditing] = React.useState(false)
  const [name, setName] = React.useState('')
  const [status, setStatus] = React.useState('')
  const [planName, setPlanName] = React.useState('')
  const [userLimit, setUserLimit] = React.useState(10)
  const [error, setError] = React.useState<string | null>(null)
  
  const [activeTab, setActiveTab] = React.useState<'info' | 'reports'>('info')

  React.useEffect(() => {
    if (company) {
      // eslint-disable-next-line react-hooks/set-state-in-effect
      setName(company.name)
      setStatus(company.status)
      setPlanName(company.planName)
      setUserLimit(company.userLimit)
    }
  }, [company])

  const mutation = useMutation({
    mutationFn: () => updateCompany(companyId, { name, status, planName, userLimit }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'companies'] })
      setEditing(false)
    },
    onError: (err: unknown) => {
      const axiosErr = err as { response?: { data?: { message?: string } } }
      setError(axiosErr?.response?.data?.message ?? 'Error al actualizar.')
    },
  })

  if (isLoading) {
    return (
      <div className="db-page db-page--center">
        <div className="db-spinner db-spinner--lg" />
      </div>
    )
  }

  if (!company) {
    return (
      <div className="db-page db-page--center">
        <div className="db-empty-state">
          <svg width="40" height="40" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
            <circle cx="12" cy="12" r="10" /><line x1="12" y1="8" x2="12" y2="12" /><line x1="12" y1="16" x2="12.01" y2="16" />
          </svg>
          <h3>Empresa no encontrada</h3>
          <button className="db-btn db-btn--ghost" onClick={() => navigate('/admin/companies')}>
            Volver a empresas
          </button>
        </div>
      </div>
    )
  }

  return (
    <div className="db-page">
      <div className="db-page-header">
        <div>
          <button className="db-back-btn" onClick={() => navigate(-1)}>← Volver</button>
          <h1 className="db-page-title">{company.name}</h1>
          <p className="db-page-subtitle">Slug: <code className="db-code">{company.slug}</code></p>
        </div>
        <div className="db-header-actions">
          <Link
            to={`/admin/companies/${companyId}/users`}
            className="db-btn db-btn--ghost"
            id="btn-view-users"
          >
            Ver usuarios
          </Link>
          <Link
            to={`/admin/companies/${companyId}/users/new`}
            className="db-btn db-btn--primary"
            id="btn-add-user"
          >
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
              <line x1="12" y1="5" x2="12" y2="19" /><line x1="5" y1="12" x2="19" y2="12" />
            </svg>
            Agregar usuario
          </Link>
        </div>
      </div>

      <div className="db-tabs">
        <button 
          className={`db-tab ${activeTab === 'info' ? 'db-tab--active' : ''}`}
          onClick={() => setActiveTab('info')}
        >
          Información general
        </button>
        <button 
          className={`db-tab ${activeTab === 'reports' ? 'db-tab--active' : ''}`}
          onClick={() => setActiveTab('reports')}
        >
          Reportes Power BI
        </button>
      </div>

      {activeTab === 'info' ? (
        <div className="db-card db-card--form">
          <div className="db-card-header">
            <h2 className="db-card-title">Detalles de la Empresa</h2>
            {!editing && (
              <button
                className="db-btn db-btn--ghost db-btn--sm"
                onClick={() => setEditing(true)}
                id="btn-edit-company"
              >
                Editar
              </button>
            )}
          </div>

        {editing ? (
          <form
            className="db-form"
            onSubmit={(e) => { e.preventDefault(); mutation.mutate() }}
            noValidate
          >
            <div className="db-form-grid">
              <div className="db-field">
                <label htmlFor="edit-name" className="db-label">Nombre</label>
                <input
                  id="edit-name"
                  type="text"
                  className="db-input"
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  required
                />
              </div>
              <div className="db-field">
                <label htmlFor="edit-status" className="db-label">Estado</label>
                <select
                  id="edit-status"
                  className="db-select"
                  value={status}
                  onChange={(e) => setStatus(e.target.value)}
                >
                  <option value="Active">Activa</option>
                  <option value="Suspended">Suspendida</option>
                  <option value="Inactive">Inactiva</option>
                </select>
              </div>
              <div className="db-field">
                <label htmlFor="edit-plan" className="db-label">Plan</label>
                <select
                  id="edit-plan"
                  className="db-select"
                  value={planName}
                  onChange={(e) => setPlanName(e.target.value)}
                >
                  <option value="Basic">Basic</option>
                  <option value="Pro">Pro</option>
                  <option value="Enterprise">Enterprise</option>
                </select>
              </div>
              <div className="db-field">
                <label htmlFor="edit-limit" className="db-label">Límite de Usuarios</label>
                <input
                  id="edit-limit"
                  type="number"
                  min="1"
                  className="db-input"
                  value={userLimit}
                  onChange={(e) => setUserLimit(Number(e.target.value))}
                  required
                />
              </div>
            </div>
            {error && (
              <div className="db-alert db-alert--error" role="alert">
                {error}
              </div>
            )}
            <div className="db-form-actions">
              <button
                type="button"
                className="db-btn db-btn--ghost"
                onClick={() => { setEditing(false); setError(null) }}
              >
                Cancelar
              </button>
              <button
                id="btn-save-company"
                type="submit"
                className="db-btn db-btn--primary"
                disabled={mutation.isPending}
              >
                {mutation.isPending ? <><span className="db-spinner db-spinner--sm" /> Guardando…</> : 'Guardar cambios'}
              </button>
            </div>
          </form>
        ) : (
          <dl className="db-definition-list">
            <div className="db-dl-row">
              <dt>Nombre</dt>
              <dd>{company.name}</dd>
            </div>
            <div className="db-dl-row">
              <dt>Slug</dt>
              <dd><code className="db-code">{company.slug}</code></dd>
            </div>
            <div className="db-dl-row">
              <dt>Estado</dt>
              <dd>{statusBadge(company.status)}</dd>
            </div>
            <div className="db-dl-row">
              <dt>Plan Actual</dt>
              <dd>{company.planName}</dd>
            </div>
            <div className="db-dl-row">
              <dt>Usuarios Activos</dt>
              <dd>
                <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                  <span>{company.currentUsers} / {company.userLimit}</span>
                  <div style={{ flex: 1, height: '6px', backgroundColor: 'var(--color-border)', borderRadius: '3px', overflow: 'hidden', maxWidth: '100px' }}>
                    <div style={{ 
                      width: `${Math.min(100, (company.currentUsers / company.userLimit) * 100)}%`, 
                      height: '100%', 
                      backgroundColor: company.currentUsers >= company.userLimit ? 'var(--color-error)' : 'var(--color-primary)'
                    }} />
                  </div>
                </div>
              </dd>
            </div>
            <div className="db-dl-row">
              <dt>Creada</dt>
              <dd>{format(new Date(company.createdAt), "dd/MM/yyyy 'a las' HH:mm")}</dd>
            </div>
          </dl>
        )}
      </div>
      ) : (
        <CompanyReportsList companyId={companyId} />
      )}
    </div>
  )
}
