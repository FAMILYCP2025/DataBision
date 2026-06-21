import React from 'react'
import { useParams, Link, useNavigate } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  getCompanies, updateCompany,
  getNativeBiFilters, upsertNativeBiFilter,
  getNativeBiItemUdfFilters, upsertNativeBiItemUdfFilter,
  getNativeBiDimensions, upsertNativeBiDimension,
  getAccountClassificationRules, createAccountClassificationRule,
  updateAccountClassificationRule, deleteAccountClassificationRule,
  getAccountClassificationTemplate,
  getNativeBiConnectionProfiles, createNativeBiConnectionProfile,
  updateNativeBiConnectionProfile, deleteNativeBiConnectionProfile,
  testNativeBiConnectionProfile,
  STATEMENT_LINES,
  type NativeBiFilterConfig, type NativeBiItemUdfFilterConfig, type NativeBiDimensionConfig,
  type AccountClassificationRule, type AccountClassificationTemplateSuggestion,
  type NativeBiConnectionProfile, type CreateNativeBiConnectionProfilePayload,
  type UpdateNativeBiConnectionProfilePayload, type TestNativeBiConnectionResult,
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

      {/* ── Connection Profiles ──────────────────────────────────────────────── */}
      <NativeBiConnectionProfilesSection companyId={companyId} />

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

      {/* ── Account Classification ───────────────────────────────────────────── */}
      <AccountClassificationSection companyId={companyId} />

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

// ── Account Classification Section ────────────────────────────────────────────

function AccountClassificationSection({ companyId }: { companyId: number }) {
  const qc = useQueryClient()
  const { data: rules = [], isLoading, isError } = useQuery({
    queryKey: ['nb-acr', companyId],
    queryFn: () => getAccountClassificationRules(companyId),
    retry: false,
  })

  const [newAccountCode, setNewAccountCode]   = React.useState('')
  const [newFormatCode, setNewFormatCode]     = React.useState('')
  const [newStatementLine, setNewStatementLine] = React.useState<string>(STATEMENT_LINES[0])
  const [addError, setAddError]               = React.useState<string | null>(null)
  const [editingId, setEditingId]             = React.useState<number | null>(null)
  const [editLine, setEditLine]               = React.useState('')

  const [templateRules, setTemplateRules]     = React.useState<AccountClassificationTemplateSuggestion[]>([])
  const [showTemplate, setShowTemplate]       = React.useState(false)
  const [templateLoading, setTemplateLoading] = React.useState(false)

  const addMutation = useMutation({
    mutationFn: () => createAccountClassificationRule(companyId, {
      accountCode: newAccountCode.trim() || null,
      formatCode: newFormatCode.trim() || null,
      statementLine: newStatementLine,
    }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['nb-acr', companyId] })
      setNewAccountCode(''); setNewFormatCode(''); setAddError(null)
    },
    onError: (err: unknown) => {
      const axiosErr = err as { response?: { data?: { message?: string } } }
      setAddError(axiosErr?.response?.data?.message ?? 'Error al agregar regla.')
    },
  })

  const deleteMutation = useMutation({
    mutationFn: (ruleId: number) => deleteAccountClassificationRule(companyId, ruleId),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['nb-acr', companyId] }),
  })

  const startEdit = (rule: AccountClassificationRule) => {
    setEditingId(rule.id)
    setEditLine(rule.statementLine)
  }

  const saveEdit = async (rule: AccountClassificationRule) => {
    await updateAccountClassificationRule(companyId, rule.id, {
      accountCode: rule.accountCode,
      formatCode: rule.formatCode,
      statementLine: editLine,
    })
    qc.invalidateQueries({ queryKey: ['nb-acr', companyId] })
    setEditingId(null)
  }

  const loadTemplate = async () => {
    setTemplateLoading(true)
    try {
      const suggestions = await getAccountClassificationTemplate(companyId)
      setTemplateRules(suggestions)
      setShowTemplate(true)
    } finally {
      setTemplateLoading(false)
    }
  }

  const applyTemplateSuggestion = (s: AccountClassificationTemplateSuggestion) =>
    createAccountClassificationRule(companyId, {
      accountCode: s.accountCode,
      formatCode: s.formatCode,
      statementLine: s.suggestedStatementLine,
    }).then(() => qc.invalidateQueries({ queryKey: ['nb-acr', companyId] }))

  if (isError) {
    return (
      <div className="db-alert db-alert--warning" style={{ margin: '0 0 16px' }}>
        No se puede cargar clasificación contable. Verifique que Analytics Company ID esté configurado y que la conexión a Supabase esté activa.
      </div>
    )
  }

  return (
    <div className="db-card">
      <div className="db-card-header">
        <h2 className="db-card-title">Clasificación contable</h2>
        <div style={{ display: 'flex', gap: '8px' }}>
          <button
            className="db-btn db-btn--ghost db-btn--sm"
            onClick={loadTemplate}
            disabled={templateLoading}
          >
            {templateLoading ? 'Cargando...' : 'Sugerencias desde OACT'}
          </button>
        </div>
      </div>

      <div className="db-alert db-alert--warning" style={{ margin: '0 16px 12px', fontSize: '12px' }}>
        Validar todas las reglas con el contador o finanzas del cliente. Una clasificación incorrecta
        afecta Estado de Resultados, Balance General y EBITDA.
      </div>

      <p style={{ fontSize: '13px', color: 'var(--color-text-muted)', padding: '0 16px 12px' }}>
        Mapea cuentas SAP B1 (por código exacto o prefijo de formato) a líneas del estado financiero.
        Las reglas se usan por el ETL al ejecutar <code className="db-code">mart.refresh_accounting_all()</code>.
      </p>

      {isLoading ? (
        <div style={{ padding: '16px' }}><span className="db-spinner" /></div>
      ) : (
        <>
          {rules.length > 0 && (
            <table className="db-table" style={{ marginBottom: '16px' }}>
              <thead>
                <tr>
                  <th>Código cuenta</th>
                  <th>Prefijo formato</th>
                  <th>Línea estado financiero</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {rules.map(rule => (
                  <tr key={rule.id}>
                    <td><code className="db-code">{rule.accountCode ?? '—'}</code></td>
                    <td><code className="db-code">{rule.formatCode ?? '—'}</code></td>
                    <td>
                      {editingId === rule.id ? (
                        <div style={{ display: 'flex', gap: '8px', alignItems: 'center' }}>
                          <select
                            className="db-select"
                            style={{ minWidth: '200px' }}
                            value={editLine}
                            onChange={e => setEditLine(e.target.value)}
                          >
                            {STATEMENT_LINES.map(sl => (
                              <option key={sl} value={sl}>{sl}</option>
                            ))}
                          </select>
                          <button className="db-btn db-btn--primary db-btn--sm" onClick={() => saveEdit(rule)}>Guardar</button>
                          <button className="db-btn db-btn--ghost db-btn--sm" onClick={() => setEditingId(null)}>Cancelar</button>
                        </div>
                      ) : (
                        <span
                          className="db-badge db-badge--muted"
                          style={{ cursor: 'pointer' }}
                          onClick={() => startEdit(rule)}
                          title="Click para editar"
                        >
                          {rule.statementLine}
                        </span>
                      )}
                    </td>
                    <td>
                      <button
                        className="db-btn db-btn--ghost db-btn--sm"
                        style={{ color: 'var(--color-error)' }}
                        onClick={() => deleteMutation.mutate(rule.id)}
                      >
                        Eliminar
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}

          {rules.length === 0 && !isLoading && (
            <p style={{ padding: '0 16px 16px', color: 'var(--color-text-muted)', fontSize: '13px' }}>
              No hay reglas configuradas. Los informes financieros mostrarán todas las cuentas como "unclassified".
            </p>
          )}

          <form
            className="db-form"
            style={{ padding: '0 16px 16px', borderTop: rules.length > 0 ? '1px solid var(--color-border)' : 'none', paddingTop: rules.length > 0 ? '16px' : 0 }}
            onSubmit={e => { e.preventDefault(); addMutation.mutate() }}
          >
            <div className="db-form-grid" style={{ gridTemplateColumns: '1fr 1fr 1fr auto' }}>
              <div className="db-field">
                <label className="db-label">Código cuenta</label>
                <input className="db-input" value={newAccountCode} onChange={e => setNewAccountCode(e.target.value)} placeholder="ej. 41000001" />
              </div>
              <div className="db-field">
                <label className="db-label">Prefijo formato</label>
                <input className="db-input" value={newFormatCode} onChange={e => setNewFormatCode(e.target.value)} placeholder="ej. 41 (todas las 41*)" />
              </div>
              <div className="db-field">
                <label className="db-label">Línea estado financiero *</label>
                <select className="db-select" value={newStatementLine} onChange={e => setNewStatementLine(e.target.value)}>
                  {STATEMENT_LINES.map(sl => <option key={sl} value={sl}>{sl}</option>)}
                </select>
              </div>
              <div className="db-field" style={{ display: 'flex', alignItems: 'flex-end' }}>
                <button
                  type="submit"
                  className="db-btn db-btn--primary db-btn--sm"
                  disabled={addMutation.isPending || (!newAccountCode.trim() && !newFormatCode.trim())}
                >
                  Agregar
                </button>
              </div>
            </div>
            {addError && <div className="db-alert db-alert--error">{addError}</div>}
          </form>
        </>
      )}

      {showTemplate && templateRules.length > 0 && (
        <div style={{ borderTop: '1px solid var(--color-border)', padding: '16px' }}>
          <h3 style={{ fontSize: '14px', fontWeight: 600, marginBottom: '12px' }}>
            Sugerencias automáticas (no aplicadas)
          </h3>
          <p style={{ fontSize: '12px', color: 'var(--color-text-muted)', marginBottom: '12px' }}>
            Basadas en el tipo de cuenta SAP B1. Revisar con contador antes de aplicar.
          </p>
          <table className="db-table">
            <thead>
              <tr>
                <th>Código</th>
                <th>Nombre</th>
                <th>Tipo SAP</th>
                <th>Sugerencia</th>
                <th>Razón</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {templateRules.map(s => (
                <tr key={s.accountCode}>
                  <td><code className="db-code">{s.accountCode}</code></td>
                  <td>{s.accountName}</td>
                  <td><code className="db-code">{s.accountType}</code></td>
                  <td><span className="db-badge db-badge--muted">{s.suggestedStatementLine}</span></td>
                  <td style={{ fontSize: '12px', color: 'var(--color-text-muted)' }}>{s.reason}</td>
                  <td>
                    <button
                      className="db-btn db-btn--ghost db-btn--sm"
                      onClick={() => applyTemplateSuggestion(s)}
                    >
                      Aplicar
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          <button className="db-btn db-btn--ghost db-btn--sm" style={{ marginTop: '12px' }} onClick={() => setShowTemplate(false)}>
            Cerrar sugerencias
          </button>
        </div>
      )}
    </div>
  )
}

// ── Native BI Connection Profiles Section ────────────────────────────────────

const BLANK_CREATE: CreateNativeBiConnectionProfilePayload = {
  profileName: '', environmentName: 'Production', serviceLayerBaseUrl: '',
  companyDb: '', sapUserName: '', secretRef: '',
  isActive: true, ignoreSslErrors: false, timeoutSeconds: 60, fetchConcurrency: 3,
}

function NativeBiConnectionProfilesSection({ companyId }: { companyId: number }) {
  const qc = useQueryClient()
  const { data: profiles = [], isLoading } = useQuery({
    queryKey: ['nb-profiles', companyId],
    queryFn: () => getNativeBiConnectionProfiles(companyId),
  })

  const [showAdd, setShowAdd]         = React.useState(false)
  const [form, setForm]               = React.useState<CreateNativeBiConnectionProfilePayload>(BLANK_CREATE)
  const [addError, setAddError]       = React.useState<string | null>(null)
  const [editingId, setEditingId]     = React.useState<number | null>(null)
  const [editForm, setEditForm]       = React.useState<UpdateNativeBiConnectionProfilePayload | null>(null)
  const [editError, setEditError]     = React.useState<string | null>(null)
  const [testResults, setTestResults] = React.useState<Record<number, TestNativeBiConnectionResult>>({})
  const [testingId, setTestingId]     = React.useState<number | null>(null)

  const invalidate = () => qc.invalidateQueries({ queryKey: ['nb-profiles', companyId] })

  const addMutation = useMutation({
    mutationFn: () => createNativeBiConnectionProfile(companyId, form),
    onSuccess: () => { invalidate(); setForm(BLANK_CREATE); setShowAdd(false); setAddError(null) },
    onError: (err: unknown) => {
      const ax = err as { response?: { data?: { message?: string } } }
      setAddError(ax?.response?.data?.message ?? 'Error al crear el perfil.')
    },
  })

  const updateMutation = useMutation({
    mutationFn: ({ id, payload }: { id: number; payload: UpdateNativeBiConnectionProfilePayload }) =>
      updateNativeBiConnectionProfile(companyId, id, payload),
    onSuccess: () => { invalidate(); setEditingId(null); setEditForm(null); setEditError(null) },
    onError: (err: unknown) => {
      const ax = err as { response?: { data?: { message?: string } } }
      setEditError(ax?.response?.data?.message ?? 'Error al actualizar el perfil.')
    },
  })

  const deleteMutation = useMutation({
    mutationFn: (id: number) => deleteNativeBiConnectionProfile(companyId, id),
    onSuccess: () => invalidate(),
  })

  const handleTest = async (p: NativeBiConnectionProfile) => {
    setTestingId(p.id)
    try {
      const result = await testNativeBiConnectionProfile(companyId, p.id)
      setTestResults(prev => ({ ...prev, [p.id]: result }))
    } catch {
      setTestResults(prev => ({
        ...prev,
        [p.id]: {
          success: false, latencyMs: 0,
          checkedAt: new Date().toISOString(),
          serviceLayerBaseUrlMasked: '', companyDb: p.companyDb,
          message: 'Error al conectar con la API.',
          capabilities: { loginOk: false, chartOfAccountsOk: false, journalEntriesOk: false },
        }
      }))
    } finally {
      setTestingId(null)
    }
  }

  const startEdit = (p: NativeBiConnectionProfile) => {
    setEditingId(p.id)
    setEditError(null)
    setEditForm({
      profileName: p.profileName, environmentName: p.environmentName,
      serviceLayerBaseUrl: p.serviceLayerBaseUrl, companyDb: p.companyDb,
      sapUserName: p.sapUserName, secretRef: '',
      isActive: p.isActive, ignoreSslErrors: p.ignoreSslErrors,
      timeoutSeconds: p.timeoutSeconds, fetchConcurrency: p.fetchConcurrency,
    })
  }

  const setF = (k: keyof CreateNativeBiConnectionProfilePayload, v: string | number | boolean) =>
    setForm(prev => ({ ...prev, [k]: v }))

  const setEF = (k: keyof UpdateNativeBiConnectionProfilePayload, v: string | number | boolean | undefined) =>
    setEditForm(prev => prev ? ({ ...prev, [k]: v }) : prev)

  return (
    <div className="db-card">
      <div className="db-card-header">
        <h2 className="db-card-title">Perfiles de conexión SAP Service Layer</h2>
        <button className="db-btn db-btn--ghost db-btn--sm" onClick={() => { setShowAdd(s => !s); setAddError(null) }}>
          {showAdd ? 'Cancelar' : '+ Nuevo perfil'}
        </button>
      </div>

      <div className="db-alert db-alert--warning" style={{ margin: '0 16px 12px', fontSize: '12px' }}>
        <strong>Seguridad:</strong> El campo <code>SecretRef</code> debe ser <code>env:NOMBRE_VARIABLE</code>.
        La variable de entorno debe estar configurada en el servidor donde corre la API (no el extractor).
        El password SAP nunca se muestra en esta UI.
      </div>

      <p style={{ fontSize: '13px', color: 'var(--color-text-muted)', padding: '0 16px 12px' }}>
        Los perfiles son usados por el extractor con <code>--profile &lt;nombre&gt;</code> para cargar credenciales SAP sin appsettings.
      </p>

      {isLoading ? (
        <div style={{ padding: '16px' }}><span className="db-spinner" /></div>
      ) : profiles.length > 0 ? (
        <table className="db-table" style={{ marginBottom: editingId ? 0 : '16px' }}>
          <thead>
            <tr>
              <th>Perfil</th>
              <th>Entorno</th>
              <th>SL URL</th>
              <th>CompanyDB</th>
              <th>Usuario SAP</th>
              <th>SecretRef</th>
              <th>Concurrencia</th>
              <th>Activo</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {profiles.map(p => (
              <React.Fragment key={p.id}>
                <tr>
                  <td><code className="db-code">{p.profileName}</code></td>
                  <td>{p.environmentName}</td>
                  <td style={{ fontSize: '12px', maxWidth: '160px', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }} title={p.serviceLayerBaseUrl}>{p.serviceLayerBaseUrl}</td>
                  <td><code className="db-code">{p.companyDb}</code></td>
                  <td>{p.sapUserName}</td>
                  <td><code className="db-code" style={{ fontSize: '11px' }}>{p.secretRefHint}</code></td>
                  <td style={{ textAlign: 'center' }}>{p.fetchConcurrency}</td>
                  <td>
                    <span className={`db-badge ${p.isActive ? 'db-badge--success' : 'db-badge--muted'}`}>
                      {p.isActive ? 'Activo' : 'Inactivo'}
                    </span>
                  </td>
                  <td>
                    <div style={{ display: 'flex', gap: '6px', alignItems: 'center' }}>
                      <button
                        className="db-btn db-btn--ghost db-btn--sm"
                        onClick={() => handleTest(p)}
                        disabled={testingId === p.id}
                      >
                        {testingId === p.id ? '…' : 'Test'}
                      </button>
                      <button
                        className="db-btn db-btn--ghost db-btn--sm"
                        onClick={() => editingId === p.id ? setEditingId(null) : startEdit(p)}
                      >
                        {editingId === p.id ? 'Cerrar' : 'Editar'}
                      </button>
                      <button
                        className="db-btn db-btn--ghost db-btn--sm"
                        style={{ color: 'var(--color-error)' }}
                        onClick={() => { if (window.confirm(`¿Eliminar perfil "${p.profileName}"?`)) deleteMutation.mutate(p.id) }}
                      >
                        Eliminar
                      </button>
                    </div>
                  </td>
                </tr>

                {/* Test result row */}
                {testResults[p.id] && (
                  <tr>
                    <td colSpan={9} style={{ backgroundColor: testResults[p.id].success ? 'rgba(22,163,74,0.06)' : 'rgba(220,38,38,0.06)', padding: '10px 16px' }}>
                      <div style={{ display: 'flex', gap: '16px', alignItems: 'flex-start', flexWrap: 'wrap' }}>
                        <span className={`db-badge ${testResults[p.id].success ? 'db-badge--success' : 'db-badge--error'}`}>
                          {testResults[p.id].success ? '✓ OK' : '✗ FAIL'}
                        </span>
                        <span style={{ fontSize: '12px' }}>{testResults[p.id].latencyMs}ms</span>
                        <span style={{ fontSize: '12px', color: 'var(--color-text-muted)' }}>
                          Login: {testResults[p.id].capabilities.loginOk ? '✓' : '✗'} &nbsp;
                          CoA: {testResults[p.id].capabilities.chartOfAccountsOk ? '✓' : '✗'} &nbsp;
                          JE: {testResults[p.id].capabilities.journalEntriesOk ? '✓' : '✗'}
                        </span>
                        <span style={{ fontSize: '12px', flex: 1 }}>{testResults[p.id].message}</span>
                        <button className="db-btn db-btn--ghost db-btn--sm" style={{ fontSize: '11px' }}
                          onClick={() => setTestResults(prev => { const n = { ...prev }; delete n[p.id]; return n })}>
                          ×
                        </button>
                      </div>
                    </td>
                  </tr>
                )}

                {/* Inline edit row */}
                {editingId === p.id && editForm && (
                  <tr>
                    <td colSpan={9} style={{ padding: '16px', backgroundColor: 'var(--color-surface)', borderTop: '1px solid var(--color-border)' }}>
                      <form onSubmit={e => { e.preventDefault(); updateMutation.mutate({ id: p.id, payload: editForm }) }}>
                        <div className="db-form-grid" style={{ gridTemplateColumns: '1fr 1fr 1fr', gap: '12px' }}>
                          <div className="db-field">
                            <label className="db-label">Nombre del perfil *</label>
                            <input className="db-input" value={editForm.profileName} onChange={e => setEF('profileName', e.target.value)} required />
                          </div>
                          <div className="db-field">
                            <label className="db-label">Entorno</label>
                            <select className="db-select" value={editForm.environmentName} onChange={e => setEF('environmentName', e.target.value)}>
                              <option>Production</option><option>Staging</option><option>Development</option>
                            </select>
                          </div>
                          <div className="db-field">
                            <label className="db-label">SL Base URL *</label>
                            <input className="db-input" value={editForm.serviceLayerBaseUrl} onChange={e => setEF('serviceLayerBaseUrl', e.target.value)} required />
                          </div>
                          <div className="db-field">
                            <label className="db-label">CompanyDB *</label>
                            <input className="db-input" value={editForm.companyDb} onChange={e => setEF('companyDb', e.target.value)} required />
                          </div>
                          <div className="db-field">
                            <label className="db-label">Usuario SAP *</label>
                            <input className="db-input" value={editForm.sapUserName} onChange={e => setEF('sapUserName', e.target.value)} required />
                          </div>
                          <div className="db-field">
                            <label className="db-label">SecretRef (vacío = mantener)</label>
                            <input className="db-input" value={editForm.secretRef ?? ''} onChange={e => setEF('secretRef', e.target.value || undefined)} placeholder={`Actual: ${p.secretRefHint}`} />
                          </div>
                          <div className="db-field">
                            <label className="db-label">Timeout (seg)</label>
                            <input className="db-input" type="number" min={10} max={300} value={editForm.timeoutSeconds} onChange={e => setEF('timeoutSeconds', Number(e.target.value))} />
                          </div>
                          <div className="db-field">
                            <label className="db-label">Concurrencia OJDT</label>
                            <input className="db-input" type="number" min={1} max={10} value={editForm.fetchConcurrency} onChange={e => setEF('fetchConcurrency', Number(e.target.value))} />
                          </div>
                          <div className="db-field" style={{ display: 'flex', gap: '16px', alignItems: 'flex-end', paddingBottom: '4px' }}>
                            <label style={{ display: 'flex', gap: '6px', alignItems: 'center', cursor: 'pointer', fontSize: '13px' }}>
                              <input type="checkbox" checked={editForm.isActive} onChange={e => setEF('isActive', e.target.checked)} />
                              Activo
                            </label>
                            <label style={{ display: 'flex', gap: '6px', alignItems: 'center', cursor: 'pointer', fontSize: '13px' }}>
                              <input type="checkbox" checked={editForm.ignoreSslErrors} onChange={e => setEF('ignoreSslErrors', e.target.checked)} />
                              Ignorar SSL
                            </label>
                          </div>
                        </div>
                        {editError && <div className="db-alert db-alert--error" style={{ marginTop: '8px' }}>{editError}</div>}
                        <div className="db-form-actions" style={{ marginTop: '12px' }}>
                          <button type="button" className="db-btn db-btn--ghost" onClick={() => setEditingId(null)}>Cancelar</button>
                          <button type="submit" className="db-btn db-btn--primary" disabled={updateMutation.isPending}>
                            {updateMutation.isPending ? <><span className="db-spinner db-spinner--sm" /> Guardando…</> : 'Guardar cambios'}
                          </button>
                        </div>
                      </form>
                    </td>
                  </tr>
                )}
              </React.Fragment>
            ))}
          </tbody>
        </table>
      ) : (
        <p style={{ padding: '0 16px 16px', color: 'var(--color-text-muted)', fontSize: '13px' }}>
          No hay perfiles de conexión. Crea uno para usar <code>--profile</code> en el extractor.
        </p>
      )}

      {/* Add form */}
      {showAdd && (
        <div style={{ borderTop: profiles.length > 0 ? '1px solid var(--color-border)' : 'none', padding: '16px' }}>
          <h3 style={{ fontSize: '14px', fontWeight: 600, marginBottom: '12px' }}>Nuevo perfil de conexión</h3>
          <form onSubmit={e => { e.preventDefault(); addMutation.mutate() }}>
            <div className="db-form-grid" style={{ gridTemplateColumns: '1fr 1fr 1fr', gap: '12px' }}>
              <div className="db-field">
                <label className="db-label">Nombre del perfil *</label>
                <input className="db-input" value={form.profileName} onChange={e => setF('profileName', e.target.value)} placeholder="ej. produccion" required />
              </div>
              <div className="db-field">
                <label className="db-label">Entorno</label>
                <select className="db-select" value={form.environmentName} onChange={e => setF('environmentName', e.target.value)}>
                  <option>Production</option><option>Staging</option><option>Development</option>
                </select>
              </div>
              <div className="db-field">
                <label className="db-label">SL Base URL *</label>
                <input className="db-input" value={form.serviceLayerBaseUrl} onChange={e => setF('serviceLayerBaseUrl', e.target.value)} placeholder="https://sap-host:50000/b1s/v1" required />
              </div>
              <div className="db-field">
                <label className="db-label">CompanyDB *</label>
                <input className="db-input" value={form.companyDb} onChange={e => setF('companyDb', e.target.value)} placeholder="CLIENT_PRD" required />
              </div>
              <div className="db-field">
                <label className="db-label">Usuario SAP *</label>
                <input className="db-input" value={form.sapUserName} onChange={e => setF('sapUserName', e.target.value)} placeholder="databision_ro" required />
              </div>
              <div className="db-field">
                <label className="db-label">SecretRef * <span style={{ fontWeight: 400, color: 'var(--color-text-muted)' }}>(env:VAR)</span></label>
                <input className="db-input" value={form.secretRef} onChange={e => setF('secretRef', e.target.value)} placeholder="env:SAP_PASSWORD_CLIENT" required />
              </div>
              <div className="db-field">
                <label className="db-label">Timeout (seg)</label>
                <input className="db-input" type="number" min={10} max={300} value={form.timeoutSeconds} onChange={e => setF('timeoutSeconds', Number(e.target.value))} />
              </div>
              <div className="db-field">
                <label className="db-label">Concurrencia OJDT</label>
                <input className="db-input" type="number" min={1} max={10} value={form.fetchConcurrency} onChange={e => setF('fetchConcurrency', Number(e.target.value))} />
              </div>
              <div className="db-field" style={{ display: 'flex', gap: '16px', alignItems: 'flex-end', paddingBottom: '4px' }}>
                <label style={{ display: 'flex', gap: '6px', alignItems: 'center', cursor: 'pointer', fontSize: '13px' }}>
                  <input type="checkbox" checked={form.isActive} onChange={e => setF('isActive', e.target.checked)} />
                  Activo
                </label>
                <label style={{ display: 'flex', gap: '6px', alignItems: 'center', cursor: 'pointer', fontSize: '13px' }}>
                  <input type="checkbox" checked={form.ignoreSslErrors} onChange={e => setF('ignoreSslErrors', e.target.checked)} />
                  Ignorar SSL
                </label>
              </div>
            </div>
            {addError && <div className="db-alert db-alert--error" style={{ marginTop: '8px' }}>{addError}</div>}
            <div className="db-form-actions" style={{ marginTop: '12px' }}>
              <button type="button" className="db-btn db-btn--ghost" onClick={() => { setShowAdd(false); setAddError(null) }}>Cancelar</button>
              <button type="submit" className="db-btn db-btn--primary" disabled={addMutation.isPending || !form.profileName.trim() || !form.secretRef.trim()}>
                {addMutation.isPending ? <><span className="db-spinner db-spinner--sm" /> Creando…</> : 'Crear perfil'}
              </button>
            </div>
          </form>
        </div>
      )}
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
