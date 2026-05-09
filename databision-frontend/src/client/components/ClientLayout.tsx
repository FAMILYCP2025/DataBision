import { Outlet } from 'react-router-dom'
import ClientSidebar from './ClientSidebar'
import ClientHeader from './ClientHeader'
import BrandingLoader from './BrandingLoader'

export default function ClientLayout() {
  return (
    <>
      <BrandingLoader />
      <div className="cp-shell">
        <ClientSidebar />
        <div className="cp-body">
          <ClientHeader />
          <main className="cp-main">
            <Outlet />
          </main>
        </div>
      </div>
    </>
  )
}
