import React from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { getCompanyUsersClient, getCompanyPermissionsClient, updateCompanyPermissionsClient, getModules, getReportsByModule } from '../../api/clientApi'
import { useClientAuthStore } from '../../store/useClientAuthStore'
import type { ClientReport, ClientPermissionItem } from '../../api/clientApi'

export default function PermissionsSettingsPage() {
  const queryClient = useQueryClient()
  const { user } = useClientAuthStore()

  // Fetch Users
  const { data: users = [] } = useQuery({
    queryKey: ['client', 'users'],
    queryFn: getCompanyUsersClient,
  })

  // Fetch all Modules
  const { data: modules = [] } = useQuery({
    queryKey: ['client', 'modules-all'],
    queryFn: getModules,
  })

  // Fetch all Reports for all Modules
  const { data: reportsMap = {}, isLoading: isLoadingReports } = useQuery({
    queryKey: ['client', 'all-reports', modules.map(m => m.slug)],
    queryFn: async () => {
      const map: Record<string, ClientReport[]> = {}
      for (const m of modules) {
        map[m.slug] = await getReportsByModule(m.slug)
      }
      return map
    },
    enabled: modules.length > 0
  })

  // Fetch Permissions
  const { data: permissionGroups = [], isLoading: isLoadingPerms } = useQuery({
    queryKey: ['client', 'permissions'],
    queryFn: getCompanyPermissionsClient,
  })

  // Local state for the matrix
  // Key format: `${userId}-${moduleSlug}-${reportId || 'all'}`
  const [localPerms, setLocalPerms] = React.useState<Record<string, boolean>>({})
  
  React.useEffect(() => {
    if (permissionGroups.length > 0) {
      const map: Record<string, boolean> = {}
      permissionGroups.forEach(group => {
        group.permissions.forEach(p => {
          // Per-report only: reportId=null rows are dead data (legacy) and ignored.
          if (p.reportId == null) return
          map[`${group.userId}-${p.moduleSlug}-${p.reportId}`] = p.enabled
        })
      })
      // eslint-disable-next-line react-hooks/set-state-in-effect
      setLocalPerms(map)
    }
  }, [permissionGroups])

  const mutation = useMutation({
    mutationFn: async (payloads: { userId: number; permissions: ClientPermissionItem[] }[]) => {
      // Execute all PUTs concurrently
      await Promise.all(payloads.map(p => updateCompanyPermissionsClient(p)))
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['client', 'permissions'] })
      alert('Permisos guardados exitosamente.')
    },
    onError: () => {
      alert('Error al guardar permisos.')
    }
  })

  const handleSave = () => {
    // Reconstruct payload grouped by userId.
    // The "module" column is UX-only (toggles all reports below it). Backend rejects
    // reportId=null because UserPermission is strict per-report; we filter here too.
    const userPayloads: Record<number, ClientPermissionItem[]> = {}

    Object.entries(localPerms).forEach(([key, enabled]) => {
      const [uIdStr, mSlug, rIdStr] = key.split('-')
      if (rIdStr === 'all') return // module-level toggle is UX, not persisted

      const userId = Number(uIdStr)
      if (!userPayloads[userId]) {
        userPayloads[userId] = []
      }
      userPayloads[userId].push({
        moduleSlug: mSlug,
        reportId: Number(rIdStr),
        enabled
      })
    })

    const payloadArray = Object.entries(userPayloads).map(([userId, permissions]) => ({
      userId: Number(userId),
      permissions
    }))

    mutation.mutate(payloadArray)
  }

  const togglePerm = (userId: number, moduleSlug: string, reportId: number | null) => {
    const key = reportId ? `${userId}-${moduleSlug}-${reportId}` : `${userId}-${moduleSlug}-all`
    setLocalPerms(prev => ({ ...prev, [key]: !prev[key] }))
  }

  const toggleModuleAll = (userId: number, moduleSlug: string) => {
    const isModuleChecked = localPerms[`${userId}-${moduleSlug}-all`] || false
    const newState = !isModuleChecked
    
    setLocalPerms(prev => {
      const next = { ...prev, [`${userId}-${moduleSlug}-all`]: newState }
      // Also update all reports within this module
      const reports = reportsMap[moduleSlug] || []
      reports.forEach(r => {
        next[`${userId}-${moduleSlug}-${r.id}`] = newState
      })
      return next
    })
  }

  if (isLoadingPerms || isLoadingReports) return <div style={{ padding: 24 }}>Cargando matriz de permisos...</div>

  // Create columns list
  const cols: { type: 'module' | 'report', label: string, moduleSlug: string, reportId: number | null }[] = []
  modules.forEach(m => {
    cols.push({ type: 'module', label: m.name, moduleSlug: m.slug, reportId: null })
    const reports = reportsMap[m.slug] || []
    reports.forEach(r => {
      cols.push({ type: 'report', label: `↳ ${r.name}`, moduleSlug: m.slug, reportId: r.id })
    })
  })

  return (
    <div className="cp-report-view">
      <div className="cp-report-header" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <div>
          <h1 className="cp-report-title">Permisos de Informes</h1>
          <p className="cp-report-desc">Asigna acceso detallado por usuario a módulos e informes específicos</p>
        </div>
        <button className="db-btn db-btn--primary" onClick={handleSave} disabled={mutation.isPending || user?.role === 'Viewer'}>
          {mutation.isPending ? 'Guardando...' : 'Guardar cambios'}
        </button>
      </div>
      
      <div className="cp-report-content" style={{ padding: '24px' }}>
        {user?.role === 'Viewer' && (
          <div className="db-alert db-alert--warning" style={{ marginBottom: 16 }}>
            No tienes permisos para editar esta matriz.
          </div>
        )}
        
        <div className="db-card" style={{ overflowX: 'auto' }}>
          <table className="db-table">
            <thead>
              <tr>
                <th style={{ minWidth: 200, position: 'sticky', left: 0, background: 'var(--color-bg)', zIndex: 2 }}>Usuario</th>
                {cols.map((col, i) => (
                  <th key={i} style={{ 
                    minWidth: col.type === 'module' ? 140 : 120,
                    textAlign: 'center',
                    background: col.type === 'module' ? 'var(--color-bg-subtle)' : 'transparent',
                    borderLeft: col.type === 'module' ? '2px solid var(--color-border)' : 'none'
                  }}>
                    {col.label}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {users.map(u => {
                const isAdmin = u.role === 'CompanyAdmin' || u.role === 'SuperAdmin'
                const isViewer = user?.role === 'Viewer'

                return (
                  <tr key={u.id}>
                    <td style={{ position: 'sticky', left: 0, background: 'var(--color-bg)', zIndex: 1, fontWeight: 500 }}>
                      {u.firstName} {u.lastName} <br/><small className="db-muted">{u.role}</small>
                    </td>
                    {cols.map((col, i) => {
                      // Module checkbox is derived from its reports: checked iff every
                      // report under the module is checked. It's UX-only and never saved.
                      let isChecked: boolean
                      if (col.type === 'module') {
                        const moduleReports = reportsMap[col.moduleSlug] || []
                        isChecked = moduleReports.length > 0 &&
                          moduleReports.every(r => localPerms[`${u.id}-${col.moduleSlug}-${r.id}`] === true)
                      } else {
                        isChecked = localPerms[`${u.id}-${col.moduleSlug}-${col.reportId}`] === true
                      }
                      const isDisabled = isAdmin || isViewer

                      return (
                        <td key={i} style={{
                          textAlign: 'center',
                          background: col.type === 'module' ? 'var(--color-bg-subtle)' : 'transparent',
                          borderLeft: col.type === 'module' ? '2px solid var(--color-border)' : 'none'
                        }}>
                          <input
                            type="checkbox"
                            checked={isAdmin ? true : isChecked}
                            disabled={isDisabled}
                            onChange={() => col.type === 'module'
                              ? toggleModuleAll(u.id, col.moduleSlug)
                              : togglePerm(u.id, col.moduleSlug, col.reportId)
                            }
                            style={{ transform: 'scale(1.2)' }}
                          />
                        </td>
                      )
                    })}
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  )
}
