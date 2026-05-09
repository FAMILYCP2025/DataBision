import React from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuthStore } from '../store/useAuthStore'
import { login } from '../api/adminApi'

export default function LoginPage() {
  const navigate = useNavigate()
  const { setAuth } = useAuthStore()

  const [email, setEmail] = React.useState('')
  const [password, setPassword] = React.useState('')
  const [error, setError] = React.useState<string | null>(null)
  const [loading, setLoading] = React.useState(false)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)
    setLoading(true)
    try {
      const result = await login({ email, password })

      if (result.user.role !== 'SuperAdmin') {
        setError('Acceso denegado. Solo SuperAdmin puede ingresar aquí.')
        return
      }

      setAuth(result.user, result.accessToken)
      navigate('/admin')
    } catch (err: unknown) {
      // Log full error in dev so we can inspect network/CORS issues
      console.error('[Login error]', err)

      const axiosErr = err as {
        response?: { status?: number; data?: { message?: string; error?: string } }
        message?: string
        code?: string
      }

      if (!axiosErr.response) {
        // Network error or CORS — no response received at all
        setError(
          `No se pudo conectar con el servidor (${axiosErr.code ?? axiosErr.message ?? 'network error'}). ` +
          'Verifique que el backend esté corriendo en http://localhost:5000.'
        )
      } else {
        // Got a response — show server message or status
        const serverMessage = axiosErr.response.data?.message
        const serverError = axiosErr.response.data?.error
        const status = axiosErr.response.status

        setError(
          serverMessage ??
          (serverError ? `Error del servidor: ${serverError}` : null) ??
          (status === 401 ? 'Credenciales incorrectas.' : null) ??
          (status === 429 ? 'Demasiados intentos. Espere 15 minutos.' : null) ??
          `Error inesperado (HTTP ${status}).`
        )
      }
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="db-login-page">
      {/* Brand panel */}
      <div className="db-login-brand">
        <div className="db-login-brand-inner">
          <div className="db-login-brand-logo">
            <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
              <polyline points="22 12 18 12 15 21 9 3 6 12 2 12" />
            </svg>
          </div>
          <h1 className="db-login-brand-title">DataBision</h1>
          <p className="db-login-brand-desc">
            Panel de administración centralizado para gestión de empresas y usuarios.
          </p>
          <div className="db-login-features">
            {[
              'Gestión multi-empresa',
              'Control de usuarios y roles',
              'Auditoría y trazabilidad',
            ].map((f) => (
              <div key={f} className="db-login-feature-item">
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
                  <polyline points="20 6 9 17 4 12" />
                </svg>
                <span>{f}</span>
              </div>
            ))}
          </div>
        </div>
      </div>

      {/* Form panel */}
      <div className="db-login-form-panel">
        <div className="db-login-form-card">
          <div className="db-login-form-header">
            <h2>Iniciar sesión</h2>
            <p>Acceso exclusivo para SuperAdmin</p>
          </div>

          <form onSubmit={handleSubmit} className="db-form" noValidate>
            <div className="db-field">
              <label htmlFor="login-email" className="db-label">
                Correo electrónico
              </label>
              <input
                id="login-email"
                type="email"
                className="db-input"
                placeholder="admin@empresa.com"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                required
                autoComplete="email"
                autoFocus
              />
            </div>

            <div className="db-field">
              <label htmlFor="login-password" className="db-label">
                Contraseña
              </label>
              <input
                id="login-password"
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
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                  <circle cx="12" cy="12" r="10" />
                  <line x1="12" y1="8" x2="12" y2="12" />
                  <line x1="12" y1="16" x2="12.01" y2="16" />
                </svg>
                {error}
              </div>
            )}

            <button
              id="login-submit"
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
                'Ingresar al panel'
              )}
            </button>
          </form>
        </div>
      </div>
    </div>
  )
}
