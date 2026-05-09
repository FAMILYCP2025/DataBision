import React from 'react'
import { useNavigate } from 'react-router-dom'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { createCompany } from '../api/adminApi'

export default function CreateCompanyPage() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()

  const [name, setName] = React.useState('')
  const [slug, setSlug] = React.useState('')
  const [status, setStatus] = React.useState('Active')
  const [planName, setPlanName] = React.useState('Basic')
  const [userLimit, setUserLimit] = React.useState(10)
  const [error, setError] = React.useState<string | null>(null)

  // Auto-generate slug from name
  const handleNameChange = (value: string) => {
    setName(value)
    setSlug(
      value
        .toLowerCase()
        .trim()
        .replace(/[^a-z0-9\s-]/g, '')
        .replace(/\s+/g, '-')
    )
  }

  const mutation = useMutation({
    mutationFn: createCompany,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'companies'] })
      navigate('/admin/companies')
    },
    onError: (err: unknown) => {
      const axiosErr = err as { response?: { data?: { message?: string } } }
      setError(axiosErr?.response?.data?.message ?? 'Error al crear la empresa.')
    },
  })

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)
    mutation.mutate({ name: name.trim(), slug: slug.trim(), status, planName, userLimit })
  }

  return (
    <div className="db-page">
      <div className="db-page-header">
        <div>
          <button
            className="db-back-btn"
            onClick={() => navigate(-1)}
          >
            ← Volver
          </button>
          <h1 className="db-page-title">Nueva empresa</h1>
          <p className="db-page-subtitle">Registrar una nueva empresa en la plataforma</p>
        </div>
      </div>

      <div className="db-card db-card--form">
        <form onSubmit={handleSubmit} className="db-form" noValidate>
          <div className="db-form-grid">
            <div className="db-field">
              <label htmlFor="company-name" className="db-label">
                Nombre de la empresa <span className="db-required">*</span>
              </label>
              <input
                id="company-name"
                type="text"
                className="db-input"
                placeholder="Ej: Acme Corporation"
                value={name}
                onChange={(e) => handleNameChange(e.target.value)}
                required
                autoFocus
              />
            </div>

            <div className="db-field">
              <label htmlFor="company-slug" className="db-label">
                Slug <span className="db-required">*</span>
              </label>
              <input
                id="company-slug"
                type="text"
                className="db-input db-input--mono"
                placeholder="acme-corporation"
                value={slug}
                onChange={(e) => setSlug(e.target.value)}
                required
                pattern="[a-z0-9-]+"
              />
              <span className="db-field-hint">Identificador único: solo minúsculas, números y guiones.</span>
            </div>

            <div className="db-field">
              <label htmlFor="company-status" className="db-label">Estado</label>
              <select
                id="company-status"
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
              <label htmlFor="company-plan" className="db-label">Plan</label>
              <select
                id="company-plan"
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
              <label htmlFor="company-limit" className="db-label">Límite de Usuarios</label>
              <input
                id="company-limit"
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
              id="btn-create-company"
              type="submit"
              className="db-btn db-btn--primary"
              disabled={mutation.isPending}
            >
              {mutation.isPending ? (
                <><span className="db-spinner db-spinner--sm" /> Creando…</>
              ) : (
                'Crear empresa'
              )}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
