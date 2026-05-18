import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'

import AuthBootstrap from '../components/AuthBootstrap'
import ClientLayout from './components/ClientLayout'
import ClientRequireAuth from './components/ClientRequireAuth'
import ClientLoginPage from './pages/ClientLoginPage'
import ClientHomePage from './pages/ClientHomePage'
import ModulePage from './pages/ModulePage'
import ReportViewPage from './pages/ReportViewPage'
import UsersSettingsPage from './pages/settings/UsersSettingsPage'
import PermissionsSettingsPage from './pages/settings/PermissionsSettingsPage'
import BrandingSettingsPage from './pages/settings/BrandingSettingsPage'

const queryClient = new QueryClient({
  defaultOptions: {
    queries: { retry: 1, staleTime: 30_000 },
  },
})

export default function ClientApp() {
  return (
    <QueryClientProvider client={queryClient}>
      <AuthBootstrap context="client">
        <BrowserRouter>
          <Routes>
            {/* Public */}
            <Route path="/client/login" element={<ClientLoginPage />} />

            {/* Protected client routes */}
            <Route element={<ClientRequireAuth />}>
              <Route element={<ClientLayout />}>
                <Route path="/client" element={<ClientHomePage />} />
                <Route path="/client/modules/:moduleSlug" element={<ModulePage />} />
                <Route path="/client/modules/:moduleSlug/reports/:reportId" element={<ReportViewPage />} />

                {/* Settings (CompanyAdmin) */}
                <Route path="/client/settings/users" element={<UsersSettingsPage />} />
                <Route path="/client/settings/permissions" element={<PermissionsSettingsPage />} />
                <Route path="/client/settings/branding" element={<BrandingSettingsPage />} />
              </Route>
            </Route>

            {/* Fallback — redirect to client home */}
            <Route path="/" element={<Navigate to="/client/login" replace />} />
            <Route path="*" element={<Navigate to="/client/login" replace />} />
          </Routes>
        </BrowserRouter>
      </AuthBootstrap>
    </QueryClientProvider>
  )
}
