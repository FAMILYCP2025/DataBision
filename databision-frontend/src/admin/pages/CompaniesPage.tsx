import { Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { getCompanies } from '../api/adminApi'
import Table from '../components/Table'
import { statusBadge } from '../components/Badge'
import type { Company } from '../../types'
import { format } from 'date-fns'

export default function CompaniesPage() {
  const { data: companies = [], isLoading, isError } = useQuery({
    queryKey: ['admin', 'companies'],
    queryFn: getCompanies,
  })

  return (
    <div className="db-page">
      <div className="db-page-header">
        <div>
          <h1 className="db-page-title">Empresas</h1>
          <p className="db-page-subtitle">
            {companies.length} empresa{companies.length !== 1 ? 's' : ''} registrada{companies.length !== 1 ? 's' : ''}
          </p>
        </div>
        <Link to="/admin/companies/new" className="db-btn db-btn--primary" id="btn-new-company">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
            <line x1="12" y1="5" x2="12" y2="19" /><line x1="5" y1="12" x2="19" y2="12" />
          </svg>
          Nueva empresa
        </Link>
      </div>

      {isError && (
        <div className="db-alert db-alert--error" role="alert">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
            <circle cx="12" cy="12" r="10" /><line x1="12" y1="8" x2="12" y2="12" /><line x1="12" y1="16" x2="12.01" y2="16" />
          </svg>
          No se pudo cargar la lista. Verifica tu conexión.
        </div>
      )}

      <div className="db-card">
        <Table<Company>
          loading={isLoading}
          data={companies}
          keyExtractor={(c) => c.id}
          emptyMessage="No hay empresas registradas."
          columns={[
            { key: 'id', header: 'ID', width: '60px' },
            { key: 'name', header: 'Nombre' },
            { key: 'slug', header: 'Slug', width: '160px' },
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
              width: '160px',
              render: (c) => (
                <div className="db-row-actions">
                  <Link to={`/admin/companies/${c.id}`} className="db-table-action">
                    Detalle
                  </Link>
                  <Link to={`/admin/companies/${c.id}/users`} className="db-table-action">
                    Usuarios
                  </Link>
                </div>
              ),
            },
          ]}
        />
      </div>
    </div>
  )
}
