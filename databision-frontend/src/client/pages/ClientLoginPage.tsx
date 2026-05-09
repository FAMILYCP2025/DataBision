import React from 'react'
import { useNavigate } from 'react-router-dom'
import { useClientAuthStore } from '../store/useClientAuthStore'
import { clientLogin } from '../api/clientApi'
import { resolveSlug } from '../../lib/theme'

export default function ClientLoginPage() {
  const navigate = useNavigate()
  const { setAuth } = useClientAuthStore()

  const [email, setEmail] = React.useState('')
  const [password, setPassword] = React.useState('')
  const [error, setError] = React.useState<string | null>(null)
  const [loading, setLoading] = React.useState(false)

  // Resolve tenant slug from URL param or subdomain, or fallback to localStorage
  const tenant = resolveSlug() || localStorage.getItem('databision-tenant')

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    
    if (!tenant) {
      setError('No se pudo identificar la empresa. Ingresa con un enlace válido.')
      return
    }

    setLoading(true)

    try {
      const result = await clientLogin({ email, password, tenant })

      // SuperAdmin cannot access client portal
      if (result.user.role === 'SuperAdmin') {
        setError('Los administradores globales deben acceder al panel de administración.')
        return
      }

      // Must belong to a company
      if (!result.user.companyId) {
        setError('Tu cuenta no tiene una empresa asignada. Contacta al administrador.')
        return
      }

      setAuth(result.user, result.accessToken, tenant)
      navigate('/client')
    } catch (err: unknown) {
      console.error('[Client login error]', err)
      const axiosErr = err as {
        response?: { status?: number; data?: { message?: string } }
        code?: string
        message?: string
      }

      if (!axiosErr.response) {
        setError(
          `No se pudo conectar con el servidor (${axiosErr.code ?? 'network error'}). ` +
          'Verifique que el backend esté activo.'
        )
      } else {
        const status = axiosErr.response.status
        const msg = axiosErr.response.data?.message
        setError(
          msg ??
          (status === 401 ? 'Credenciales incorrectas.' : null) ??
          (status === 403 ? 'Acceso denegado a este portal.' : null) ??
          `Error inesperado (HTTP ${status}).`
        )
      }
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="cp-login-page">
      {/* Left brand panel */}
      <div className="cp-login-brand">
        <div className="cp-login-brand-inner">
          <div className="cp-login-brand-logo">
            <svg width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
              <polyline points="22 12 18 12 15 21 9 3 6 12 2 12" />
            </svg>
          </div>
          <h1 className="cp-login-brand-title">DataBision</h1>
          <p className="cp-login-brand-subtitle">Portal de Inteligencia de Negocios</p>
          <p className="cp-login-brand-desc">
            Accede a los informes y análisis de tu empresa en un solo lugar.
          </p>

          <div className="cp-login-brand-features">
            {[
              'Informes por módulo y proceso',
              'Datos actualizados en tiempo real',
              'Acceso seguro con JWT',
            ].map((f) => (
              <div key={f} className="cp-login-feature">
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
                  <polyline points="20 6 9 17 4 12" />
                </svg>
                <span>{f}</span>
              </div>
            ))}
          </div>

          {tenant && (
            <div className="cp-login-tenant-chip">
              <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                <path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z" />
                <polyline points="9 22 9 12 15 12 15 22" />
              </svg>
              Empresa: <strong>{tenant}</strong>
            </div>
          )}
        </div>
      </div>

      {/* Right form panel */}
      <div className="cp-login-form-panel">
        <div className="cp-login-form-card">
          <div className="cp-login-form-header">
            <h2>Iniciar sesión</h2>
            <p>Accede con las credenciales de tu empresa</p>
          </div>

          <form onSubmit={handleSubmit} className="db-form" noValidate>
            <div className="db-field">
              <label htmlFor="client-email" className="db-label">
                Correo electrónico
              </label>
              <input
                id="client-email"
                type="email"
                className="db-input"
                placeholder="usuario@empresa.com"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                required
                autoComplete="email"
                autoFocus
              />
            </div>

            <div className="db-field">
              <label htmlFor="client-password" className="db-label">
                Contraseña
              </label>
              <input
                id="client-password"
                type="password"
                className="db-input"
                placeholder="••••••••"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                required
                autoComplete="current-password"
              />
            </div>

            {error && (
              <div className="db-alert db-alert--error" role="alert">
                <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                  <circle cx="12" cy="12" r="10" /><line x1="12" y1="8" x2="12" y2="12" /><line x1="12" y1="16" x2="12.01" y2="16" />
                </svg>
                {error}
              </div>
            )}

            <button
              id="client-login-submit"
              type="submit"
              className="db-btn db-btn--primary db-btn--full"
              disabled={loading}
            >
              {loading ? (
                <>
                  <span className="db-spinner db-spinner--sm" />
                  Ingresando…
                </>
              ) : (
                'Ingresar al portal'
              )}
            </button>
          </form>

          {!tenant && (
            <p className="cp-login-tenant-hint">
              ¿Eres SuperAdmin?{' '}
              <a href="/?app=admin" className="db-link">
                Ir al panel de administración
              </a>
            </p>
          )}
        </div>
      </div>
    </div>
  )
}
