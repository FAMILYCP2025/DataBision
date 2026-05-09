import { Outlet } from 'react-router-dom'
import Sidebar from './Sidebar'

export default function AdminLayout() {
  return (
    <div className="db-shell">
      <Sidebar />
      <main className="db-main">
        <div className="db-content">
          <Outlet />
        </div>
      </main>
    </div>
  )
}
