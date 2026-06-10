import { useParams } from 'react-router-dom'
import { useClientModules } from '../hooks/useClientData'
import { useClientAuthStore } from '../store/useClientAuthStore'

export default function ClientHeader() {
  const { moduleSlug } = useParams()
  const { data: modules } = useClientModules()
  const user = useClientAuthStore((s) => s.user)

  const currentModule = modules?.find((m) => m.slug === moduleSlug)
  const companyName = (user as { companyName?: string } | null)?.companyName ?? 'Mi Empresa'

  return (
    <header className="cp-header">
      {/* Left: breadcrumb */}
      <div className="cp-header-left">
        <span className="cp-header-company">{companyName}</span>
        {currentModule && (
          <>
            <span className="cp-header-sep">
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                <polyline points="9 18 15 12 9 6" />
              </svg>
            </span>
            <span className="cp-header-module">{currentModule.name}</span>
          </>
        )}
      </div>

      {/* Right: user chip */}
      <div className="cp-header-right">
        <div className="cp-header-user">
          <div className="cp-header-avatar">
            {user?.name?.charAt(0).toUpperCase() ?? 'U'}
          </div>
          <div className="cp-header-user-meta">
            <span className="cp-header-user-name">{user?.name}</span>
            <span className="cp-header-user-role">{user?.email}</span>
          </div>
        </div>
      </div>
    </header>
  )
}
