import React from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { getCompanyUsers, updateCompanyUser, getCompanies } from '../api/adminApi'

export default function EditUserPage() {
  const { id: companyIdStr, userId: userIdStr } = useParams<{ id: string; userId: string }>()
  const companyId = Number(companyIdStr)
  const userId = Number(userIdStr)
  const navigate = useNavigate()
  const queryClient = useQueryClient()

  const { data: companies = [] } = useQuery({
    queryKey: ['admin', 'companies'],
    queryFn: getCompanies,
  })
  const company = companies.find((c) => c.id === companyId)

  const { data: users = [], isLoading } = useQuery({
    queryKey: ['admin', 'companies', companyId, 'users'],
    queryFn: () => getCompanyUsers(companyId),
    enabled: !!companyId,
  })

  const user = users.find((u) => u.id === userId)

  const [firstName, setFirstName] = React.useState('')
  const [lastName, setLastName] = React.useState('')
  const [role, setRole] = React.useState('Viewer')
  const [error, setError] = React.useState<string | null>(null)

  React.useEffect(() => {
    if (user) {
      // eslint-disable-next-line react-hooks/set-state-in-effect
      setFirstName(user.firstName)
      setLastName(user.lastName)
      setRole(user.role)
    }
  }, [user])

  const mutation = useMutation({
    mutationFn: () => updateCompanyUser(companyId, userId, { firstName, lastName, role }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'companies', companyId, 'users'] })
      navigate(`/admin/companies/${companyId}/users`)
    },
    onError: (err: unknown) => {
      const axiosErr = err as { response?: { data?: { message?: string } } }
      setError(axiosErr?.response?.data?.message ?? 'Error al actualizar usuario.')
    },
  })

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)
    mutation.mutate()
  }

  if (isLoading) {
    return <div className="db-page db-page--center"><div className="db-spinner db-spinner--lg" /></div>
  }

  if (!user) {
    return (
      <div className="db-page db-page--center">
        <div className="db-empty-state">
          <h3>Usuario no encontrado</h3>
          <button className="db-btn db-btn--ghost" onClick={() => navigate(`/admin/companies/${companyId}/users`)}>Volver</button>
        </div>
      </div>
    )
  }

  return (
    <div className="db-page">
      <div className="db-page-header">
        <div>
          <button className="db-back-btn" onClick={() => navigate(`/admin/companies/${companyId}/users`)}>
            ← Volver a usuarios
          </button>
          <h1 className="db-page-title">Editar usuario</h1>
          <p className="db-page-subtitle">Empresa: {company?.name ?? '...'}</p>
        </div>
      </div>

      <div className="db-card db-card--form">
        <form onSubmit={handleSubmit} className="db-form" noValidate>
          <div className="db-form-grid">
            <div className="db-field">
              <label htmlFor="user-email" className="db-label">Correo electrónico</label>
              <input id="user-email" type="email" className="db-input db-input--readonly" value={user.email} readOnly />
            </div>
            
            <div className="db-field">
              <label htmlFor="user-role" className="db-label">Rol</label>
              <select id="user-role" className="db-select" value={role} onChange={(e) => setRole(e.target.value)}>
                <option value="Viewer">Viewer (Solo lectura)</option>
                <option value="CompanyAdmin">CompanyAdmin (Administrador)</option>
              </select>
            </div>

            <div className="db-field">
              <label htmlFor="user-first-name" className="db-label">Nombre <span className="db-required">*</span></label>
              <input id="user-first-name" type="text" className="db-input" value={firstName} onChange={(e) => setFirstName(e.target.value)} required />
            </div>

            <div className="db-field">
              <label htmlFor="user-last-name" className="db-label">Apellido <span className="db-required">*</span></label>
              <input id="user-last-name" type="text" className="db-input" value={lastName} onChange={(e) => setLastName(e.target.value)} required />
            </div>
          </div>

          {error && (
            <div className="db-alert db-alert--error" role="alert">
              {error}
            </div>
          )}

          <div className="db-form-actions">
            <button type="button" className="db-btn db-btn--ghost" onClick={() => navigate(-1)}>Cancelar</button>
            <button type="submit" className="db-btn db-btn--primary" disabled={mutation.isPending}>
              {mutation.isPending ? <><span className="db-spinner db-spinner--sm" /> Guardando…</> : 'Guardar cambios'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
