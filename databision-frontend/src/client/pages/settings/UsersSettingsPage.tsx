import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { getCompanyUsersClient, createCompanyUserClient, updateCompanyUserStatusClient } from '../../api/clientApi'
import { useClientAuthStore } from '../../store/useClientAuthStore'
import Table from '../../../admin/components/Table'
import Badge, { statusBadge } from '../../../admin/components/Badge'
import type { User } from '../../../types'


export default function UsersSettingsPage() {
  const queryClient = useQueryClient()
  const { user } = useClientAuthStore()
  const [isModalOpen, setIsModalOpen] = useState(false)
  const [formData, setFormData] = useState({ firstName: '', lastName: '', email: '', password: '', role: 'Viewer' })
  
  const { data: users = [], isLoading } = useQuery({
    queryKey: ['client', 'users'],
    queryFn: getCompanyUsersClient,
    enabled: !!user,
  })

  const toggleStatusMutation = useMutation({
    mutationFn: ({ userId, isActive }: { userId: number; isActive: boolean }) =>
      updateCompanyUserStatusClient(userId, isActive),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['client', 'users'] })
    },
    onError: (err: unknown) => {
      const axiosErr = err as { response?: { data?: { message?: string } } }
      alert(axiosErr?.response?.data?.message ?? 'Error al cambiar estado.')
    },
  })

  const createUserMutation = useMutation({
    mutationFn: () => createCompanyUserClient(formData),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['client', 'users'] })
      setIsModalOpen(false)
      setFormData({ firstName: '', lastName: '', email: '', password: '', role: 'Viewer' })
    },
    onError: (err: unknown) => {
      const axiosErr = err as { response?: { data?: { message?: string } } }
      alert(axiosErr?.response?.data?.message ?? 'Error al crear usuario.')
    },
  })

  return (
    <div className="cp-report-view">
      <div className="cp-report-header" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <div>
          <h1 className="cp-report-title">Usuarios</h1>
          <p className="cp-report-desc">Administra los accesos de tu empresa</p>
        </div>
        <button className="db-btn db-btn--primary" onClick={() => setIsModalOpen(true)}>
          Nuevo usuario
        </button>
      </div>
      
      <div className="cp-report-content" style={{ padding: '24px' }}>
        <div className="db-card">
          <Table<User>
            loading={isLoading}
            data={users}
            keyExtractor={(u) => u.id}
            emptyMessage="Tu empresa no tiene usuarios adicionales."
            columns={[
              {
                key: 'name',
                header: 'Nombre',
                render: (u) => `${u.firstName} ${u.lastName}`,
              },
              { key: 'email', header: 'Correo' },
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
                key: 'actions',
                header: '',
                width: '100px',
                render: (u) => (
                  <div style={{ display: 'flex', gap: '8px', justifyContent: 'flex-end' }}>
                    <button
                      className="db-btn db-btn--ghost db-btn--sm"
                      onClick={() => toggleStatusMutation.mutate({ userId: u.id, isActive: !u.isActive })}
                      disabled={toggleStatusMutation.isPending || u.id === user?.id}
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

      {isModalOpen && (
        <div className="db-modal-overlay">
          <div className="db-modal">
            <div className="db-modal-header">
              <h2>Nuevo Usuario</h2>
              <button className="db-modal-close" onClick={() => setIsModalOpen(false)}>×</button>
            </div>
            <div className="db-modal-body">
              <div className="db-form-group">
                <label>Nombre</label>
                <input 
                  type="text" 
                  className="db-input" 
                  value={formData.firstName}
                  onChange={e => setFormData({ ...formData, firstName: e.target.value })}
                />
              </div>
              <div className="db-form-group">
                <label>Apellido</label>
                <input 
                  type="text" 
                  className="db-input" 
                  value={formData.lastName}
                  onChange={e => setFormData({ ...formData, lastName: e.target.value })}
                />
              </div>
              <div className="db-form-group">
                <label>Correo Electrónico</label>
                <input 
                  type="email" 
                  className="db-input" 
                  value={formData.email}
                  onChange={e => setFormData({ ...formData, email: e.target.value })}
                />
              </div>
              <div className="db-form-group">
                <label>Contraseña</label>
                <input 
                  type="password" 
                  className="db-input" 
                  value={formData.password}
                  onChange={e => setFormData({ ...formData, password: e.target.value })}
                />
              </div>
              <div className="db-form-group">
                <label>Rol</label>
                <select 
                  className="db-input" 
                  value={formData.role}
                  onChange={e => setFormData({ ...formData, role: e.target.value })}
                >
                  <option value="Viewer">Viewer</option>
                  <option value="CompanyAdmin">CompanyAdmin</option>
                </select>
              </div>
            </div>
            <div className="db-modal-footer">
              <button className="db-btn db-btn--ghost" onClick={() => setIsModalOpen(false)}>Cancelar</button>
              <button 
                className="db-btn db-btn--primary" 
                onClick={() => createUserMutation.mutate()}
                disabled={createUserMutation.isPending || !formData.firstName || !formData.lastName || !formData.email || !formData.password}
              >
                {createUserMutation.isPending ? 'Creando...' : 'Crear Usuario'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
