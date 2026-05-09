import { Link, useParams, useNavigate } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { getCompanies, getCompanyUsers, updateCompanyUserStatus } from '../api/adminApi'
import Table from '../components/Table'
import Badge, { statusBadge } from '../components/Badge'
import type { User } from '../../types'
import { format } from 'date-fns'

export default function CompanyUsersPage() {
  const { id } = useParams<{ id: string }>()
  const companyId = Number(id)
  const navigate = useNavigate()
  const queryClient = useQueryClient()

  const { data: companies = [] } = useQuery({
    queryKey: ['admin', 'companies'],
    queryFn: getCompanies,
  })
  const company = companies.find((c) => c.id === companyId)

  const { data: users = [], isLoading, isError } = useQuery({
    queryKey: ['admin', 'companies', companyId, 'users'],
    queryFn: () => getCompanyUsers(companyId),
    enabled: !!companyId,
  })

  const toggleStatusMutation = useMutation({
    mutationFn: ({ userId, isActive }: { userId: number; isActive: boolean }) =>
      updateCompanyUserStatus(companyId, userId, isActive),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'companies', companyId, 'users'] })
      queryClient.invalidateQueries({ queryKey: ['admin', 'companies'] }) // Updates currentUsers
    },
    onError: (err: unknown) => {
      const axiosErr = err as { response?: { data?: { message?: string } } }
      alert(axiosErr?.response?.data?.message ?? 'Error al cambiar estado.')
    },
  })

  return (
    <div className="db-page">
      <div className="db-page-header">
        <div>
          <button className="db-back-btn" onClick={() => navigate(`/admin/companies/${companyId}`)}>
            ← {company?.name ?? 'Empresa'}
          </button>
          <h1 className="db-page-title">Usuarios</h1>
          <p className="db-page-subtitle">
            {users.length} usuario{users.length !== 1 ? 's' : ''} registrado{users.length !== 1 ? 's' : ''}
          </p>
        </div>
        <Link
          to={`/admin/companies/${companyId}/users/new`}
          className="db-btn db-btn--primary"
          id="btn-new-user"
        >
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
            <line x1="12" y1="5" x2="12" y2="19" /><line x1="5" y1="12" x2="19" y2="12" />
          </svg>
          Nuevo usuario
        </Link>
      </div>

      {isError && (
        <div className="db-alert db-alert--error" role="alert">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
            <circle cx="12" cy="12" r="10" /><line x1="12" y1="8" x2="12" y2="12" /><line x1="12" y1="16" x2="12.01" y2="16" />
          </svg>
          Error al cargar los usuarios.
        </div>
      )}

      <div className="db-card">
        <Table<User>
          loading={isLoading}
          data={users}
          keyExtractor={(u) => u.id}
          emptyMessage="Esta empresa no tiene usuarios aún."
          columns={[
            {
              key: 'name',
              header: 'Nombre',
              render: (u) => `${u.firstName} ${u.lastName}`,
            },
            { key: 'email', header: 'Correo electrónico' },
            {
              key: 'role',
              header: 'Rol',
              width: '130px',
              render: (u) => <Badge label={u.role} variant="neutral" />,
            },
            {
              key: 'isActive',
              header: 'Estado',
              width: '100px',
              render: (u) => statusBadge(u.isActive ? 'Active' : 'Inactive'),
            },
            {
              key: 'lastLoginAt',
              header: 'Último acceso',
              width: '150px',
              render: (u) =>
                u.lastLoginAt
                  ? format(new Date(u.lastLoginAt), 'dd/MM/yyyy HH:mm')
                  : <span className="db-muted">—</span>,
            },
            {
              key: 'createdAt',
              header: 'Creado',
              width: '120px',
              render: (u) => format(new Date(u.createdAt), 'dd/MM/yyyy'),
            },
            {
              key: 'actions',
              header: '',
              width: '100px',
              render: (u) => (
                <div style={{ display: 'flex', gap: '8px', justifyContent: 'flex-end' }}>
                  <button
                    className="db-btn db-btn--ghost db-btn--sm"
                    onClick={() => navigate(`/admin/companies/${companyId}/users/${u.id}/edit`)}
                  >
                    Editar
                  </button>
                  <button
                    className="db-btn db-btn--ghost db-btn--sm"
                    onClick={() => toggleStatusMutation.mutate({ userId: u.id, isActive: !u.isActive })}
                    disabled={toggleStatusMutation.isPending}
                    style={{ color: u.isActive ? 'var(--color-error)' : 'var(--color-primary)' }}
                  >
                    {u.isActive ? 'Desactivar' : 'Activar'}
                  </button>
                </div>
              ),
            },
          ]}
        />
      </div>
    </div>
  )
}
