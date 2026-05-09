import { useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { useClientModules } from '../hooks/useClientData'

export default function ClientHomePage() {
  const navigate = useNavigate()
  const { data: modules, isLoading } = useClientModules()

  useEffect(() => {
    if (!isLoading && modules && modules.length > 0) {
      navigate(`/client/modules/${modules[0].slug}`, { replace: true })
    }
  }, [modules, isLoading, navigate])

  return (
    <div className="cp-page cp-page--center">
      {isLoading ? (
        <span className="db-spinner db-spinner--lg" />
      ) : (
        <div className="cp-empty-state">
          <div className="cp-empty-icon">
            <svg width="36" height="36" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
              <rect x="3" y="3" width="7" height="7" /><rect x="14" y="3" width="7" height="7" /><rect x="14" y="14" width="7" height="7" /><rect x="3" y="14" width="7" height="7" />
            </svg>
          </div>
          <h3>Sin módulos disponibles</h3>
          <p>No tienes módulos asignados. Contacta al administrador de tu empresa.</p>
        </div>
      )}
    </div>
  )
}
