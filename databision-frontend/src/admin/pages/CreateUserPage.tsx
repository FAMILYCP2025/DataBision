import React from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { createCompanyUser } from '../api/adminApi'

export default function CreateUserPage() {
  const { id } = useParams<{ id: string }>()
  const companyId = Number(id)
  const navigate = useNavigate()
  const queryClient = useQueryClient()

  const [form, setForm] = React.useState({
    firstName: '',
    lastName: '',
    email: '',
    password: '',
    role: 'Viewer',
  })
  const [error, setError] = React.useState<string | null>(null)

  const handleChange = (key: string, value: string) => {
    setForm((prev) => ({ ...prev, [key]: value }))
  }

  const mutation = useMutation({
    mutationFn: () =>
      createCompanyUser(companyId, {
        firstName: form.firstName.trim(),
        lastName: form.lastName.trim(),
        email: form.email.trim(),
        password: form.password,
        role: form.role,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'companies', companyId, 'users'] })
      navigate(`/admin/companies/${companyId}/users`)
    },
    onError: (err: unknown) => {
      const axiosErr = err as { response?: { data?: { message?: string } } }
      setError(axiosErr?.response?.data?.message ?? 'Error al crear el usuario.')
    },
  })

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)
    mutation.mutate()
  }

  return (
    <div className="db-page">
      <div className="db-page-header">
        <div>
          <button
            className="db-back-btn"
            onClick={() => navigate(`/admin/companies/${companyId}/users`)}
          >
            ← Usuarios
          </button>
          <h1 className="db-page-title">Nuevo usuario</h1>
          <p className="db-page-subtitle">Crear un usuario vinculado a esta empresa</p>
        </div>
      </div>

      <div className="db-card db-card--form">
        <form onSubmit={handleSubmit} className="db-form" noValidate>
          <div className="db-form-grid db-form-grid--2col">
            <div className="db-field">
              <label htmlFor="user-firstname" className="db-label">
                Nombre <span className="db-required">*</span>
              </label>
              <input
                id="user-firstname"
                type="text"
                className="db-input"
                placeholder="Juan"
                value={form.firstName}
                onChange={(e) => handleChange('firstName', e.target.value)}
                required
                autoFocus
              />
            </div>
            <div className="db-field">
              <label htmlFor="user-lastname" className="db-label">
                Apellido <span className="db-required">*</span>
              </label>
              <input
                id="user-lastname"
                type="text"
                className="db-input"
                placeholder="Pérez"
                value={form.lastName}
                onChange={(e) => handleChange('lastName', e.target.value)}
                required
              />
            </div>
          </div>

          <div className="db-field">
            <label htmlFor="user-email" className="db-label">
              Correo electrónico <span className="db-required">*</span>
            </label>
            <input
              id="user-email"
              type="email"
              className="db-input"
              placeholder="juan@empresa.com"
              value={form.email}
              onChange={(e) => handleChange('email', e.target.value)}
              required
              autoComplete="off"
            />
          </div>

          <div className="db-field">
            <label htmlFor="user-password" className="db-label">
              Contraseña <span className="db-required">*</span>
            </label>
            <input
              id="user-password"
              type="password"
              className="db-input"
              placeholder="Mínimo 8 caracteres"
              value={form.password}
              onChange={(e) => handleChange('password', e.target.value)}
              required
              minLength={8}
              autoComplete="new-password"
            />
          </div>

          <div className="db-field">
            <label htmlFor="user-role" className="db-label">Rol</label>
            <select
              id="user-role"
              className="db-select"
              value={form.role}
              onChange={(e) => handleChange('role', e.target.value)}
            >
              <option value="CompanyAdmin">Company Admin</option>
              <option value="Viewer">Viewer</option>
            </select>
            <span className="db-field-hint">
              CompanyAdmin puede gestionar la empresa. Viewer solo puede visualizar reportes.
            </span>
          </div>

          {error && (
            <div className="db-alert db-alert--error" role="alert">
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                <circle cx="12" cy="12" r="10" /><line x1="12" y1="8" x2="12" y2="12" /><line x1="12" y1="16" x2="12.01" y2="16" />
              </svg>
              {error}
            </div>
          )}

          <div className="db-form-actions">
            <button
              type="button"
              className="db-btn db-btn--ghost"
              onClick={() => navigate(-1)}
            >
              Cancelar
            </button>
            <button
              id="btn-create-user"
              type="submit"
              className="db-btn db-btn--primary"
              disabled={mutation.isPending}
            >
              {mutation.isPending ? (
                <><span className="db-spinner db-spinner--sm" /> Creando…</>
              ) : (
                'Crear usuario'
              )}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
