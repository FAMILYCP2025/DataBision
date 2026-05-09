import React from 'react'
import { Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { getCompanies } from '../api/adminApi'
import Table from '../components/Table'
import Badge, { statusBadge } from '../components/Badge'
import type { Company } from '../../types'
import { format } from 'date-fns'

export default function DashboardPage() {
  const { data: companies = [], isLoading, isError } = useQuery({
    queryKey: ['admin', 'companies'],
    queryFn: getCompanies,
  })

  const active = companies.filter((c) => c.status === 'Active').length
  const suspended = companies.filter((c) => c.status === 'Suspended').length
  const inactive = companies.filter((c) => c.status === 'Inactive').length

  const stats = [
    { label: 'Total empresas', value: companies.length, accent: '#2563EB' },
    { label: 'Activas', value: active, accent: '#16A34A' },
    { label: 'Suspendidas', value: suspended, accent: '#D97706' },
    { label: 'Inactivas', value: inactive, accent: '#94A3B8' },
  ]

  return (
    <div className="db-page">
      {/* Page header */}
      <div className="db-page-header">
        <div>
          <h1 className="db-page-title">Dashboard</h1>
          <p className="db-page-subtitle">Visión general de empresas registradas</p>
        </div>
        <Link to="/admin/companies/new" className="db-btn db-btn--primary" id="btn-new-company-dash">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
            <line x1="12" y1="5" x2="12" y2="19" />
            <line x1="5" y1="12" x2="19" y2="12" />
          </svg>
          Nueva empresa
        </Link>
      </div>

      {/* Stats */}
      <div className="db-stats-grid">
        {stats.map((s) => (
          <div key={s.label} className="db-stat-card">
            <span className="db-stat-value" style={{ color: s.accent }}>{s.value}</span>
            <span className="db-stat-label">{s.label}</span>
          </div>
        ))}
      </div>

      {/* Error */}
      {isError && (
        <div className="db-alert db-alert--error" role="alert">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
            <circle cx="12" cy="12" r="10" /><line x1="12" y1="8" x2="12" y2="12" /><line x1="12" y1="16" x2="12.01" y2="16" />
          </svg>
          No se pudo cargar la lista de empresas. Verifique su conexión.
        </div>
      )}

      {/* Table */}
      <div className="db-card">
        <div className="db-card-header">
          <h2 className="db-card-title">Empresas</h2>
          <Link to="/admin/companies" className="db-link">Ver todas →</Link>
        </div>
        <Table<Company>
          loading={isLoading}
          data={companies.slice(0, 10)}
          keyExtractor={(c) => c.id}
          emptyMessage="No hay empresas registradas aún."
          columns={[
            { key: 'name', header: 'Nombre' },
            { key: 'slug', header: 'Slug', width: '140px' },
            {
              key: 'status',
              header: 'Estado',
              width: '110px',
              render: (c) => statusBadge(c.status),
            },
            {
              key: 'createdAt',
              header: 'Creada',
              width: '130px',
              render: (c) => format(new Date(c.createdAt), 'dd/MM/yyyy'),
            },
            {
              key: 'actions',
              header: '',
              width: '100px',
              render: (c) => (
                <Link to={`/admin/companies/${c.id}`} className="db-table-action">
                  Ver detalle
                </Link>
              ),
            },
          ]}
        />
      </div>
    </div>
  )
}
