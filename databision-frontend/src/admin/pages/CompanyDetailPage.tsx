import React from 'react'
import { useParams, Link, useNavigate } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  getCompanies, updateCompany,
  getNativeBiFilters, upsertNativeBiFilter,
  getNativeBiItemUdfFilters, upsertNativeBiItemUdfFilter,
  getNativeBiDimensions, upsertNativeBiDimension,
  type NativeBiFilterConfig, type NativeBiItemUdfFilterConfig, type NativeBiDimensionConfig,
} from '../api/adminApi'
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
  const [analyticsCompanyId, setAnalyticsCompanyId] = React.useState<string>('')
  const [error, setError] = React.useState<string | null>(null)
  
  const [activeTab, setActiveTab] = React.useState<'info' | 'reports' | 'nativebi'>('info')

  React.useEffect(() => {
    if (company) {
      // eslint-disable-next-line react-hooks/set-state-in-effect
      setName(company.name)
      setStatus(company.status)
      setPlanName(company.planName)
      setUserLimit(company.userLimit)
      setAnalyticsCompanyId(company.analyticsCompanyId ?? '')
    }
  }, [company])

  const mutation = useMutation({
    mutationFn: () => updateCompany(companyId, {
      name, status, planName, userLimit,
      analyticsCompanyId: analyticsCompanyId.trim() || null,
    }),
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
        <button
          className={`db-tab ${activeTab === 'nativebi' ? 'db-tab--active' : ''}`}
          onClick={() => setActiveTab('nativebi')}
        >
          Native BI — Configuración avanzada
        </button>
      </div>

      {activeTab === 'nativebi' && <NativeBiConfigTab companyId={companyId} />}

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
              <div className="db-field" style={{ gridColumn: '1 / -1' }}>
                <label htmlFor="edit-analytics-id" className="db-label">Analytics Company ID</label>
                <input
                  id="edit-analytics-id"
                  type="text"
                  className="db-input"
                  value={analyticsCompanyId}
                  onChange={(e) => setAnalyticsCompanyId(e.target.value)}
                  placeholder="ej. ksdepor-analytics (Supabase MART company_id)"
                />
                <p style={{ fontSize: '12px', color: 'var(--color-text-muted)', marginTop: '4px' }}>
                  Identificador en la base de datos MART de Supabase. Requerido para Native BI.
                </p>
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
              <dt>Analytics Company ID</dt>
              <dd>
                {company.analyticsCompanyId
                  ? <code className="db-code">{company.analyticsCompanyId}</code>
                  : <span style={{ color: 'var(--color-text-muted)' }}>No configurado</span>}
              </dd>
            </div>
            <div className="db-dl-row">
              <dt>Creada</dt>
              <dd>{format(new Date(company.createdAt), "dd/MM/yyyy 'a las' HH:mm")}</dd>
            </div>
          </dl>
        )}
      </div>
      ) : activeTab === 'reports' ? (
        <CompanyReportsList companyId={companyId} />
      ) : null}
    </div>
  )
}

// ── Native BI Config Tab ───────────────────────────────────────────────────────

function NativeBiConfigTab({ companyId }: { companyId: number }) {
  const qc = useQueryClient()

  const { data: filters = [] }    = useQuery({ queryKey: ['nb-filters', companyId], queryFn: () => getNativeBiFilters(companyId) })
  const { data: udfs = [] }       = useQuery({ queryKey: ['nb-udfs', companyId], queryFn: () => getNativeBiItemUdfFilters(companyId) })
  const { data: dimensions = [] } = useQuery({ queryKey: ['nb-dims', companyId], queryFn: () => getNativeBiDimensions(companyId) })

  // ── New filter form ──────────────────────────────────────────────────────────
  const [newFilterKey, setNewFilterKey]       = React.useState('')
  const [newFilterLabel, setNewFilterLabel]   = React.useState('')
  const [filterError, setFilterError]         = React.useState<string | null>(null)

  const addFilterMutation = useMutation({
    mutationFn: () => upsertNativeBiFilter(companyId, newFilterKey.trim(), {
      label: newFilterLabel.trim() || null, isEnabled: true, isAdvanced: false, displayOrder: filters.length,
    }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['nb-filters', companyId] })
      setNewFilterKey(''); setNewFilterLabel(''); setFilterError(null)
    },
    onError: () => setFilterError('Error al guardar el filtro.'),
  })

  const toggleFilter = (f: NativeBiFilterConfig) =>
    upsertNativeBiFilter(companyId, f.filterKey, { label: f.label, isEnabled: !f.isEnabled, isAdvanced: f.isAdvanced, displayOrder: f.displayOrder, defaultValue: f.defaultValue })
      .then(() => qc.invalidateQueries({ queryKey: ['nb-filters', companyId] }))

  // ── New UDF form ─────────────────────────────────────────────────────────────
  const [newUdfName, setNewUdfName]   = React.useState('')
  const [newUdfLabel, setNewUdfLabel] = React.useState('')
  const [udfError, setUdfError]       = React.useState<string | null>(null)

  const addUdfMutation = useMutation({
    mutationFn: () => upsertNativeBiItemUdfFilter(companyId, newUdfName.trim(), {
      label: newUdfLabel.trim() || null, isEnabled: true, isMultiSelect: false, displayOrder: udfs.length,
    }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['nb-udfs', companyId] })
      setNewUdfName(''); setNewUdfLabel(''); setUdfError(null)
    },
    onError: () => setUdfError('Error al guardar el campo UDF.'),
  })

  const toggleUdf = (u: NativeBiItemUdfFilterConfig) =>
    upsertNativeBiItemUdfFilter(companyId, u.udfFieldName, { label: u.label, isEnabled: !u.isEnabled, isMultiSelect: u.isMultiSelect, displayOrder: u.displayOrder })
      .then(() => qc.invalidateQueries({ queryKey: ['nb-udfs', companyId] }))

  // ── Dimension toggles ────────────────────────────────────────────────────────
  const toggleDimension = (d: NativeBiDimensionConfig) =>
    upsertNativeBiDimension(companyId, d.dimensionNumber, { label: d.label, isEnabled: !d.isEnabled })
      .then(() => qc.invalidateQueries({ queryKey: ['nb-dims', companyId] }))

  const ALL_DIMENSIONS = [1, 2, 3, 4, 5]

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>

      {/* ── Filters ─────────────────────────────────────────────────────────── */}
      <div className="db-card">
        <div className="db-card-header">
          <h2 className="db-card-title">Filtros activos (Native BI)</h2>
        </div>
        <p style={{ fontSize: '13px', color: 'var(--color-text-muted)', padding: '0 16px 12px' }}>
          Controla qué filtros aparecen en los dashboards de esta empresa. La clave de filtro debe coincidir exactamente con el campo en mart.
        </p>

        {filters.length > 0 && (
          <table className="db-table" style={{ marginBottom: '16px' }}>
            <thead>
              <tr>
                <th>Clave de filtro</th>
                <th>Etiqueta</th>
                <th>Avanzado</th>
                <th>Orden</th>
                <th>Habilitado</th>
              </tr>
            </thead>
            <tbody>
              {filters.map(f => (
                <tr key={f.filterKey}>
                  <td><code className="db-code">{f.filterKey}</code></td>
                  <td>{f.label ?? <span style={{ color: 'var(--color-text-muted)' }}>—</span>}</td>
                  <td>{f.isAdvanced ? 'Sí' : 'No'}</td>
                  <td>{f.displayOrder}</td>
                  <td>
                    <button
                      className={`db-badge ${f.isEnabled ? 'db-badge--success' : 'db-badge--muted'}`}
                      onClick={() => toggleFilter(f)}
                      style={{ cursor: 'pointer', border: 'none', background: 'none' }}
                    >
                      {f.isEnabled ? 'Activo' : 'Inactivo'}
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}

        <form
          className="db-form"
          style={{ padding: '0 16px 16px', borderTop: filters.length > 0 ? '1px solid var(--color-border)' : 'none', paddingTop: filters.length > 0 ? '16px' : 0 }}
          onSubmit={e => { e.preventDefault(); addFilterMutation.mutate() }}
        >
          <div className="db-form-grid" style={{ gridTemplateColumns: '1fr 1fr auto' }}>
            <div className="db-field">
              <label className="db-label">Clave de filtro *</label>
              <input className="db-input" value={newFilterKey} onChange={e => setNewFilterKey(e.target.value)} placeholder="ej. warehouseCode" required />
            </div>
            <div className="db-field">
              <label className="db-label">Etiqueta</label>
              <input className="db-input" value={newFilterLabel} onChange={e => setNewFilterLabel(e.target.value)} placeholder="ej. Bodega" />
            </div>
            <div className="db-field" style={{ display: 'flex', alignItems: 'flex-end' }}>
              <button type="submit" className="db-btn db-btn--primary db-btn--sm" disabled={addFilterMutation.isPending || !newFilterKey.trim()}>
                Agregar
              </button>
            </div>
          </div>
          {filterError && <div className="db-alert db-alert--error">{filterError}</div>}
        </form>
      </div>

      {/* ── Item UDF Filters ─────────────────────────────────────────────────── */}
      <div className="db-card">
        <div className="db-card-header">
          <h2 className="db-card-title">Campos UDF de artículos</h2>
        </div>
        <p style={{ fontSize: '13px', color: 'var(--color-text-muted)', padding: '0 16px 12px' }}>
          Campos definidos por el usuario (UDF) en OITM que se mostrarán como filtros en los dashboards de inventario/ventas.
        </p>

        {udfs.length > 0 && (
          <table className="db-table" style={{ marginBottom: '16px' }}>
            <thead>
              <tr>
                <th>Campo UDF</th>
                <th>Etiqueta</th>
                <th>Multi-selección</th>
                <th>Orden</th>
                <th>Habilitado</th>
              </tr>
            </thead>
            <tbody>
              {udfs.map(u => (
                <tr key={u.udfFieldName}>
                  <td><code className="db-code">{u.udfFieldName}</code></td>
                  <td>{u.label ?? <span style={{ color: 'var(--color-text-muted)' }}>—</span>}</td>
                  <td>{u.isMultiSelect ? 'Sí' : 'No'}</td>
                  <td>{u.displayOrder}</td>
                  <td>
                    <button
                      className={`db-badge ${u.isEnabled ? 'db-badge--success' : 'db-badge--muted'}`}
                      onClick={() => toggleUdf(u)}
                      style={{ cursor: 'pointer', border: 'none', background: 'none' }}
                    >
                      {u.isEnabled ? 'Activo' : 'Inactivo'}
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}

        <form
          className="db-form"
          style={{ padding: '0 16px 16px', borderTop: udfs.length > 0 ? '1px solid var(--color-border)' : 'none', paddingTop: udfs.length > 0 ? '16px' : 0 }}
          onSubmit={e => { e.preventDefault(); addUdfMutation.mutate() }}
        >
          <div className="db-form-grid" style={{ gridTemplateColumns: '1fr 1fr auto' }}>
            <div className="db-field">
              <label className="db-label">Campo UDF *</label>
              <input className="db-input" value={newUdfName} onChange={e => setNewUdfName(e.target.value)} placeholder="ej. U_Category" required />
            </div>
            <div className="db-field">
              <label className="db-label">Etiqueta</label>
              <input className="db-input" value={newUdfLabel} onChange={e => setNewUdfLabel(e.target.value)} placeholder="ej. Categoría" />
            </div>
            <div className="db-field" style={{ display: 'flex', alignItems: 'flex-end' }}>
              <button type="submit" className="db-btn db-btn--primary db-btn--sm" disabled={addUdfMutation.isPending || !newUdfName.trim()}>
                Agregar
              </button>
            </div>
          </div>
          {udfError && <div className="db-alert db-alert--error">{udfError}</div>}
        </form>
      </div>

      {/* ── Dimensions ───────────────────────────────────────────────────────── */}
      <div className="db-card">
        <div className="db-card-header">
          <h2 className="db-card-title">Dimensiones de centros de costo (SAP B1)</h2>
        </div>
        <p style={{ fontSize: '13px', color: 'var(--color-text-muted)', padding: '0 16px 12px' }}>
          SAP Business One soporta hasta 5 dimensiones de centros de costo. Habilita las que usa esta empresa y asigna una etiqueta descriptiva.
        </p>
        <table className="db-table">
          <thead>
            <tr>
              <th>Dimensión</th>
              <th>Etiqueta</th>
              <th>Habilitada</th>
            </tr>
          </thead>
          <tbody>
            {ALL_DIMENSIONS.map(n => {
              const dim = dimensions.find(d => d.dimensionNumber === n)
              return (
                <DimensionRow
                  key={n}
                  companyId={companyId}
                  dimensionNumber={n}
                  dim={dim}
                  onToggle={() => dim
                    ? toggleDimension(dim)
                    : upsertNativeBiDimension(companyId, n, { label: null, isEnabled: true }).then(() => qc.invalidateQueries({ queryKey: ['nb-dims', companyId] }))
                  }
                  onSaved={() => qc.invalidateQueries({ queryKey: ['nb-dims', companyId] })}
                />
              )
            })}
          </tbody>
        </table>
      </div>
    </div>
  )
}

function DimensionRow({
  companyId, dimensionNumber, dim, onToggle, onSaved
}: {
  companyId: number
  dimensionNumber: number
  dim: NativeBiDimensionConfig | undefined
  onToggle: () => void
  onSaved: () => void
}) {
  const [editing, setEditing] = React.useState(false)
  const [label, setLabel]     = React.useState(dim?.label ?? '')

  React.useEffect(() => {
    setLabel(dim?.label ?? '')
  }, [dim?.label])

  const save = () =>
    upsertNativeBiDimension(companyId, dimensionNumber, { label: label.trim() || null, isEnabled: dim?.isEnabled ?? false })
      .then(() => { onSaved(); setEditing(false) })

  return (
    <tr>
      <td>Dimensión {dimensionNumber}</td>
      <td>
        {editing ? (
          <div style={{ display: 'flex', gap: '8px', alignItems: 'center' }}>
            <input className="db-input" style={{ maxWidth: '200px' }} value={label} onChange={e => setLabel(e.target.value)} placeholder="ej. Centro de costos" autoFocus />
            <button className="db-btn db-btn--primary db-btn--sm" onClick={save}>Guardar</button>
            <button className="db-btn db-btn--ghost db-btn--sm" onClick={() => setEditing(false)}>Cancelar</button>
          </div>
        ) : (
          <span style={{ cursor: 'pointer', color: dim?.label ? undefined : 'var(--color-text-muted)' }} onClick={() => setEditing(true)}>
            {dim?.label ?? '— (click para editar)'}
          </span>
        )}
      </td>
      <td>
        <button
          className={`db-badge ${dim?.isEnabled ? 'db-badge--success' : 'db-badge--muted'}`}
          onClick={onToggle}
          style={{ cursor: 'pointer', border: 'none', background: 'none' }}
        >
          {dim?.isEnabled ? 'Habilitada' : 'Deshabilitada'}
        </button>
      </td>
    </tr>
  )
}
