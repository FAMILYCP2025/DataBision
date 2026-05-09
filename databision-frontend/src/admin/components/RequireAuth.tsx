import { Navigate, Outlet } from 'react-router-dom'
import { useAuthStore } from '../store/useAuthStore'

export default function RequireAuth() {
  const { isAuthenticated, user } = useAuthStore()

  if (!isAuthenticated || user?.role !== 'SuperAdmin') {
    return <Navigate to="/admin/login" replace />
  }

  return <Outlet />
}
