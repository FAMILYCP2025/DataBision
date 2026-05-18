import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'

import AuthBootstrap from '../components/AuthBootstrap'
import AdminLayout from './components/AdminLayout'
import RequireAuth from './components/RequireAuth'
import LoginPage from './pages/LoginPage'
import DashboardPage from './pages/DashboardPage'
import CompaniesPage from './pages/CompaniesPage'
import CreateCompanyPage from './pages/CreateCompanyPage'
import CompanyDetailPage from './pages/CompanyDetailPage'
import CompanyUsersPage from './pages/CompanyUsersPage'
import CreateUserPage from './pages/CreateUserPage'
import EditUserPage from './pages/EditUserPage'

const queryClient = new QueryClient({
  defaultOptions: {
    queries: { retry: 1, staleTime: 30_000 },
  },
})

export default function AdminApp() {
  return (
    <QueryClientProvider client={queryClient}>
      <AuthBootstrap context="admin">
        <BrowserRouter>
          <Routes>
            {/* Public */}
            <Route path="/admin/login" element={<LoginPage />} />

            {/* Protected admin routes */}
            <Route element={<RequireAuth />}>
              <Route element={<AdminLayout />}>
                <Route path="/admin" element={<DashboardPage />} />
                <Route path="/admin/companies" element={<CompaniesPage />} />
                <Route path="/admin/companies/new" element={<CreateCompanyPage />} />
                <Route path="/admin/companies/:id" element={<CompanyDetailPage />} />
                <Route path="/admin/companies/:id/users" element={<CompanyUsersPage />} />
                <Route path="/admin/companies/:id/users/new" element={<CreateUserPage />} />
                <Route path="/admin/companies/:id/users/:userId/edit" element={<EditUserPage />} />
              </Route>
            </Route>

            {/* Fallback */}
            <Route path="*" element={<Navigate to="/admin" replace />} />
          </Routes>
        </BrowserRouter>
      </AuthBootstrap>
    </QueryClientProvider>
  )
}
