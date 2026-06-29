import { NavLink, useParams, useLocation } from 'react-router-dom'
import { KeyRound } from 'lucide-react'
import { useClientModules } from '../hooks/useClientData'
import { useClientAuthStore } from '../store/useClientAuthStore'
import { clientLogout } from '../api/clientApi'

// ── Icon map ──────────────────────────────────────────────────────────────────
const ICONS: Record<string, React.ReactNode> = {
  'trending-up': (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <polyline points="23 6 13.5 15.5 8.5 10.5 1 18" /><polyline points="17 6 23 6 23 12" />
    </svg>
  ),
  'file-text': (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" /><polyline points="14 2 14 8 20 8" /><line x1="16" y1="13" x2="8" y2="13" /><line x1="16" y1="17" x2="8" y2="17" /><polyline points="10 9 9 9 8 9" />
    </svg>
  ),
  'git-branch': (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <line x1="6" y1="3" x2="6" y2="15" /><circle cx="18" cy="6" r="3" /><circle cx="6" cy="18" r="3" /><path d="M18 9a9 9 0 0 1-9 9" />
    </svg>
  ),
  'package': (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <line x1="16.5" y1="9.4" x2="7.5" y2="4.21" /><path d="M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z" /><polyline points="3.27 6.96 12 12.01 20.73 6.96" /><line x1="12" y1="22.08" x2="12" y2="12" />
    </svg>
  ),
  'dollar-sign': (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <line x1="12" y1="1" x2="12" y2="23" /><path d="M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6" />
    </svg>
  ),
  'bar-chart-2': (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <line x1="18" y1="20" x2="18" y2="10" /><line x1="12" y1="20" x2="12" y2="4" /><line x1="6" y1="20" x2="6" y2="14" />
    </svg>
  ),
}

function ModuleIcon({ name }: { name: string | null }) {
  const icon = name && ICONS[name] ? ICONS[name] : (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <rect x="3" y="3" width="7" height="7" /><rect x="14" y="3" width="7" height="7" /><rect x="14" y="14" width="7" height="7" /><rect x="3" y="14" width="7" height="7" />
    </svg>
  )
  return <span className="cp-nav-icon">{icon}</span>
}

export default function ClientSidebar() {
  const { moduleSlug } = useParams()
  const location = useLocation()
  const { data: modules, isLoading } = useClientModules()
  const { user, clearAuth } = useClientAuthStore()

  const initials = user?.name
    ? user.name.split(' ').map((n) => n[0]).join('').toUpperCase().slice(0, 2)
    : 'U'

  // Show Módulos section only while loading or when at least one module has reports.
  // Hides it entirely when Power BI has no reports assigned (native BI demo scenario).
  const hasVisibleModules = isLoading || Boolean(modules?.some((m) => m.reportCount > 0))

  async function handleLogout() {
    const currentTenant = useClientAuthStore.getState().tenant || localStorage.getItem('databision-tenant')
    try { await clientLogout() } catch { /* ignore */ }
    clearAuth()
    const tenantParam = currentTenant ? `?tenant=${currentTenant}` : ''
    window.location.href = `/client/login${tenantParam}`
  }

  return (
    <aside className="cp-sidebar">
      {/* Logo / Brand */}
      <div className="cp-sidebar-brand">
        <div className="cp-brand-mark">
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
            <polyline points="22 12 18 12 15 21 9 3 6 12 2 12" />
          </svg>
        </div>
        <div className="cp-brand-text">
          <span className="cp-brand-name">DataBision</span>
          <span className="cp-brand-sub">Portal BI</span>
        </div>
      </div>

      {/* Nav — Modules (hidden when Power BI has no reports assigned) */}
      {hasVisibleModules && (
        <nav className="cp-sidebar-nav">
          <p className="cp-nav-section-label">Módulos</p>

          {isLoading && (
            <div className="cp-nav-loading">
              {[1, 2, 3, 4].map((i) => (
                <div key={i} className="cp-nav-skeleton" />
              ))}
            </div>
          )}

          {modules?.map((mod) => (
            <NavLink
              key={mod.slug}
              to={`/client/modules/${mod.slug}`}
              className={({ isActive }) =>
                `cp-nav-item${isActive || moduleSlug === mod.slug ? ' cp-nav-item--active' : ''}`
              }
            >
              <ModuleIcon name={mod.icon} />
              <span className="cp-nav-label">{mod.name}</span>
              <span className="cp-nav-count">{mod.reportCount}</span>
            </NavLink>
          ))}
        </nav>
      )}

      {/* Analytics — MART */}
      <nav className="cp-sidebar-nav" style={{ marginTop: 4 }}>
        <p className="cp-nav-section-label">Analytics</p>
        <NavLink
          to="/client/bi/mart-overview"
          className={({ isActive }) =>
            `cp-nav-item${isActive ? ' cp-nav-item--active' : ''}`
          }
        >
          <span className="cp-nav-icon">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
              <line x1="18" y1="20" x2="18" y2="10" /><line x1="12" y1="20" x2="12" y2="4" /><line x1="6" y1="20" x2="6" y2="14" />
            </svg>
          </span>
          <span className="cp-nav-label">Resumen</span>
        </NavLink>
        <NavLink
          to="/client/bi/sales"
          className={({ isActive }) =>
            `cp-nav-item${isActive ? ' cp-nav-item--active' : ''}`
          }
        >
          <span className="cp-nav-icon">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
              <polyline points="23 6 13.5 15.5 8.5 10.5 1 18" /><polyline points="17 6 23 6 23 12" />
            </svg>
          </span>
          <span className="cp-nav-label">Ventas</span>
        </NavLink>
        <NavLink
          to="/client/bi/purchase"
          className={({ isActive }) =>
            `cp-nav-item${isActive ? ' cp-nav-item--active' : ''}`
          }
        >
          <span className="cp-nav-icon">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
              <circle cx="9" cy="21" r="1" /><circle cx="20" cy="21" r="1" /><path d="M1 1h4l2.68 13.39a2 2 0 0 0 2 1.61h9.72a2 2 0 0 0 2-1.61L23 6H6" />
            </svg>
          </span>
          <span className="cp-nav-label">Compras</span>
        </NavLink>
        <NavLink
          to="/client/bi/inventory-mart"
          className={({ isActive }) =>
            `cp-nav-item${isActive ? ' cp-nav-item--active' : ''}`
          }
        >
          <span className="cp-nav-icon">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
              <line x1="16.5" y1="9.4" x2="7.5" y2="4.21" /><path d="M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z" /><polyline points="3.27 6.96 12 12.01 20.73 6.96" /><line x1="12" y1="22.08" x2="12" y2="12" />
            </svg>
          </span>
          <span className="cp-nav-label">Inventario</span>
        </NavLink>
        <NavLink
          to="/client/bi/finance-mart"
          className={({ isActive }) =>
            `cp-nav-item${isActive ? ' cp-nav-item--active' : ''}`
          }
        >
          <span className="cp-nav-icon">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
              <line x1="12" y1="1" x2="12" y2="23" /><path d="M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6" />
            </svg>
          </span>
          <span className="cp-nav-label">Finanzas</span>
        </NavLink>
      </nav>

      {/* Native BI */}
      <nav className="cp-sidebar-nav" style={{ marginTop: 4 }}>
        <p className="cp-nav-section-label">Análisis</p>
        <NavLink
          to="/client/bi/dashboard"
          className={({ isActive }) =>
            `cp-nav-item${isActive || location.pathname === '/client/bi/dashboard' ? ' cp-nav-item--active' : ''}`
          }
        >
          <span className="cp-nav-icon">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
              <rect x="3" y="3" width="7" height="7" /><rect x="14" y="3" width="7" height="7" /><rect x="14" y="14" width="7" height="7" /><rect x="3" y="14" width="7" height="7" />
            </svg>
          </span>
          <span className="cp-nav-label">Dashboard</span>
        </NavLink>
        <NavLink
          to="/client/bi/sales"
          className={({ isActive }) =>
            `cp-nav-item${isActive ? ' cp-nav-item--active' : ''}`
          }
        >
          <span className="cp-nav-icon">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
              <polyline points="23 6 13.5 15.5 8.5 10.5 1 18" /><polyline points="17 6 23 6 23 12" />
            </svg>
          </span>
          <span className="cp-nav-label">Ventas</span>
        </NavLink>
        <NavLink
          to="/client/bi/purchasing"
          className={({ isActive }) =>
            `cp-nav-item${isActive ? ' cp-nav-item--active' : ''}`
          }
        >
          <span className="cp-nav-icon">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
              <path d="M6 2L3 6v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2V6l-3-4z" /><line x1="3" y1="6" x2="21" y2="6" /><path d="M16 10a4 4 0 0 1-8 0" />
            </svg>
          </span>
          <span className="cp-nav-label">Compras</span>
        </NavLink>
        <NavLink
          to="/client/bi/inventory"
          className={({ isActive }) =>
            `cp-nav-item${isActive ? ' cp-nav-item--active' : ''}`
          }
        >
          <span className="cp-nav-icon">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
              <line x1="16.5" y1="9.4" x2="7.5" y2="4.21" /><path d="M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z" /><polyline points="3.27 6.96 12 12.01 20.73 6.96" /><line x1="12" y1="22.08" x2="12" y2="12" />
            </svg>
          </span>
          <span className="cp-nav-label">Inventario</span>
        </NavLink>
        <NavLink
          to="/client/bi/finance"
          className={({ isActive }) =>
            `cp-nav-item${isActive ? ' cp-nav-item--active' : ''}`
          }
        >
          <span className="cp-nav-icon">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
              <line x1="12" y1="1" x2="12" y2="23" /><path d="M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6" />
            </svg>
          </span>
          <span className="cp-nav-label">Finanzas</span>
        </NavLink>
        <NavLink
          to="/client/bi/operations"
          className={({ isActive }) =>
            `cp-nav-item${isActive ? ' cp-nav-item--active' : ''}`
          }
        >
          <span className="cp-nav-icon">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
              <polyline points="22 12 18 12 15 21 9 3 6 12 2 12" />
            </svg>
          </span>
          <span className="cp-nav-label">Operaciones</span>
        </NavLink>
        {user?.role === 'CompanyAdmin' && (
          <NavLink
            to="/client/bi/diagnostics"
            className={({ isActive }) =>
              `cp-nav-item${isActive ? ' cp-nav-item--active' : ''}`
            }
          >
            <span className="cp-nav-icon">
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                <circle cx="12" cy="12" r="3" /><path d="M19.07 4.93a10 10 0 0 0-14.14 0" /><path d="M4.93 19.07a10 10 0 0 0 14.14 0" />
              </svg>
            </span>
            <span className="cp-nav-label">Diagnósticos</span>
          </NavLink>
        )}
      </nav>

      {/* Admin Menu (Only for CompanyAdmin) */}
      {user?.role === 'CompanyAdmin' && (
        <nav className="cp-sidebar-nav" style={{ marginTop: 'auto', marginBottom: '16px' }}>
          <p className="cp-nav-section-label">Administración</p>
          <NavLink
            to="/client/settings/users"
            className={({ isActive }) => `cp-nav-item${isActive ? ' cp-nav-item--active' : ''}`}
          >
            <span className="cp-nav-icon">
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2" /><circle cx="9" cy="7" r="4" /><path d="M23 21v-2a4 4 0 0 0-3-3.87" /><path d="M16 3.13a4 4 0 0 1 0 7.75" />
              </svg>
            </span>
            <span className="cp-nav-label">Usuarios</span>
          </NavLink>
          <NavLink
            to="/client/settings/permissions"
            className={({ isActive }) => `cp-nav-item${isActive ? ' cp-nav-item--active' : ''}`}
          >
            <span className="cp-nav-icon">
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                <rect x="3" y="11" width="18" height="11" rx="2" ry="2" /><path d="M7 11V7a5 5 0 0 1 10 0v4" />
              </svg>
            </span>
            <span className="cp-nav-label">Permisos</span>
          </NavLink>
          <NavLink
            to="/client/settings/branding"
            className={({ isActive }) => `cp-nav-item${isActive ? ' cp-nav-item--active' : ''}`}
          >
            <span className="cp-nav-icon">
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                <path d="M12 2.69l5.66 5.66a8 8 0 1 1-11.31 0z" />
              </svg>
            </span>
            <span className="cp-nav-label">Apariencia</span>
          </NavLink>
          <NavLink
            to="/client/settings/password"
            className={({ isActive }) => `cp-nav-item${isActive ? ' cp-nav-item--active' : ''}`}
          >
            <span className="cp-nav-icon"><KeyRound size={16} /></span>
            <span className="cp-nav-label">Contraseña</span>
          </NavLink>
        </nav>
      )}

      {/* Contraseña — usuarios sin rol admin */}
      {user?.role !== 'CompanyAdmin' && (
        <nav className="cp-sidebar-nav" style={{ marginTop: 'auto', marginBottom: 16 }}>
          <NavLink
            to="/client/settings/password"
            className={({ isActive }) => `cp-nav-item${isActive ? ' cp-nav-item--active' : ''}`}
          >
            <span className="cp-nav-icon"><KeyRound size={16} /></span>
            <span className="cp-nav-label">Contraseña</span>
          </NavLink>
        </nav>
      )}

      {/* Footer — user */}
      <div className="cp-sidebar-footer">
        <div className="cp-user-avatar">{initials}</div>
        <div className="cp-user-meta">
          <span className="cp-user-name">{user?.name ?? 'Usuario'}</span>
          <span className="cp-user-role">{user?.role ?? ''}</span>
        </div>
        <button
          className="cp-logout-btn"
          onClick={handleLogout}
          title="Cerrar sesión"
          aria-label="Cerrar sesión"
        >
          <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
            <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4" /><polyline points="16 17 21 12 16 7" /><line x1="21" y1="12" x2="9" y2="12" />
          </svg>
        </button>
      </div>
    </aside>
  )
}
