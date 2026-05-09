import { Navigate, Outlet } from 'react-router-dom'
import { useClientAuthStore } from '../store/useClientAuthStore'

export default function ClientRequireAuth() {
  const { isAuthenticated, user } = useClientAuthStore()

  if (!isAuthenticated) {
    return <Navigate to="/client/login" replace />
  }

  // SuperAdmin is not allowed in client portal
  if (user?.role === 'SuperAdmin') {
    return <Navigate to="/admin" replace />
  }

  return <Outlet />
}
