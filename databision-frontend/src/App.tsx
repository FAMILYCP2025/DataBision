import { isAdminHost } from './lib/theme'
import AdminApp from './admin/AdminApp'
import ClientApp from './client/ClientApp'

export default function App() {
  const isAdmin = isAdminHost()
  return isAdmin ? <AdminApp /> : <ClientApp />
}
