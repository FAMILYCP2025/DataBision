import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { format, parseISO } from 'date-fns'
import { es } from 'date-fns/locale'
import { getAuditLogs } from '../api/adminApi'

const PAGE_SIZE = 50

function Skeleton({ height = 44 }: { height?: number }) {
  return (
    <div
      className="cp-skeleton"
      style={{ height, borderRadius: 4, margin: '4px 0' }}
    />
  )
}

function fmtDate(iso: string) {
  try {
    return format(parseISO(iso), 'dd MMM yyyy HH:mm', { locale: es })
  } catch {
    return iso
  }
}

function ActionBadge({ action }: { action: string }) {
  const isError  = action.toLowerCase().includes('fail') || action.toLowerCase().includes('error')
  const isDelete = action.toLowerCase().includes('delete') || action.toLowerCase().includes('revoke')
  const bg = isError ? '#FEE2E2' : isDelete ? '#FEF3C7' : '#EFF6FF'
  const color = isError ? '#B91C1C' : isDelete ? '#92400E' : '#1D4ED8'
  return (
    <span style={{
      display: 'inline-block',
      padding: '2px 8px',
      borderRadius: 4,
      fontSize: 11.5,
      fontWeight: 600,
      letterSpacing: '0.02em',
      background: bg,
      color,
    }}>
      {action}
    </span>
  )
}

export default function AdminAuditLogPage() {
  const [page, setPage] = useState(1)

  const { data: logs, isLoading, isFetching } = useQuery({
    queryKey: ['audit-logs', page],
    queryFn: () => getAuditLogs(page, PAGE_SIZE),
    placeholderData: prev => prev,
  })

  const hasMore = (logs?.length ?? 0) === PAGE_SIZE

  return (
    <div style={{ padding: '32px 24px', maxWidth: 1200 }}>
      <div style={{ marginBottom: 24 }}>
        <h1 style={{ fontSize: 20, fontWeight: 700, color: 'var(--c-text)', margin: 0 }}>
          Log de auditoría
        </h1>
        <p style={{ fontSize: 13.5, color: 'var(--c-text-muted)', margin: '4px 0 0' }}>
          Registro de acciones del sistema por todos los usuarios y empresas.
        </p>
      </div>

      <div className="db-card" style={{ overflow: 'hidden' }}>
        <div style={{ overflowX: 'auto' }}>
          <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 13 }}>
            <thead>
              <tr style={{ background: '#F8FAFC', borderBottom: '1px solid var(--c-border)' }}>
                {['ID', 'Acción', 'Usuario', 'Empresa', 'Recurso', 'IP', 'Fecha'].map(h => (
                  <th
                    key={h}
                    style={{
                      padding: '10px 14px',
                      textAlign: 'left',
                      fontSize: 12,
                      fontWeight: 600,
                      color: 'var(--c-text-muted)',
                      whiteSpace: 'nowrap',
                      letterSpacing: '0.03em',
                    }}
                  >
                    {h}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {isLoading ? (
                Array.from({ length: 8 }).map((_, i) => (
                  <tr key={i}>
                    <td colSpan={7} style={{ padding: '6px 14px' }}>
                      <Skeleton />
                    </td>
                  </tr>
                ))
              ) : !logs || logs.length === 0 ? (
                <tr>
                  <td
                    colSpan={7}
                    style={{ padding: '48px 24px', textAlign: 'center', color: 'var(--c-text-muted)', fontSize: 13.5 }}
                  >
                    No hay registros de auditoría.
                  </td>
                </tr>
              ) : (
                logs.map(log => (
                  <tr
                    key={log.id}
                    style={{ borderBottom: '1px solid var(--c-border)', height: 44 }}
                  >
                    <td style={{ padding: '0 14px', color: 'var(--c-text-faint)', fontVariantNumeric: 'tabular-nums', fontSize: 12 }}>
                      {log.id}
                    </td>
                    <td style={{ padding: '0 14px' }}>
                      <ActionBadge action={log.action} />
                    </td>
                    <td style={{ padding: '0 14px', color: 'var(--c-text)' }}>
                      {log.user?.email
                        ? log.user.email
                        : log.userId != null
                          ? `#${log.userId}`
                          : <span style={{ color: 'var(--c-text-faint)' }}>—</span>}
                    </td>
                    <td style={{ padding: '0 14px', color: 'var(--c-text)' }}>
                      {log.company?.name
                        ? log.company.name
                        : log.companyId != null
                          ? `#${log.companyId}`
                          : <span style={{ color: 'var(--c-text-faint)' }}>—</span>}
                    </td>
                    <td style={{ padding: '0 14px', color: 'var(--c-text-muted)', fontSize: 12 }}>
                      {[log.resourceType, log.resourceId].filter(Boolean).join(' / ') || (
                        <span style={{ color: 'var(--c-text-faint)' }}>—</span>
                      )}
                    </td>
                    <td style={{ padding: '0 14px', color: 'var(--c-text-muted)', fontFamily: 'monospace', fontSize: 12 }}>
                      {log.ipAddress ?? <span style={{ color: 'var(--c-text-faint)' }}>—</span>}
                    </td>
                    <td style={{ padding: '0 14px', color: 'var(--c-text-muted)', whiteSpace: 'nowrap', fontSize: 12 }}>
                      {fmtDate(log.createdAt)}
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

        {/* Pagination */}
        <div style={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          padding: '12px 16px',
          borderTop: '1px solid var(--c-border)',
          background: '#FAFAFA',
        }}>
          <span style={{ fontSize: 12.5, color: 'var(--c-text-muted)' }}>
            Página {page} · {logs?.length ?? 0} registros
            {isFetching && !isLoading && (
              <span style={{ marginLeft: 8, color: 'var(--brand-primary)' }}>Actualizando…</span>
            )}
          </span>
          <div style={{ display: 'flex', gap: 8 }}>
            <button
              disabled={page === 1}
              onClick={() => setPage(p => Math.max(1, p - 1))}
              style={{
                padding: '5px 14px',
                borderRadius: 6,
                border: '1px solid var(--c-border)',
                background: '#fff',
                color: page === 1 ? 'var(--c-text-faint)' : 'var(--c-text)',
                fontSize: 13,
                cursor: page === 1 ? 'default' : 'pointer',
                fontFamily: 'inherit',
              }}
            >
              ← Anterior
            </button>
            <button
              disabled={!hasMore}
              onClick={() => setPage(p => p + 1)}
              style={{
                padding: '5px 14px',
                borderRadius: 6,
                border: '1px solid var(--c-border)',
                background: '#fff',
                color: !hasMore ? 'var(--c-text-faint)' : 'var(--c-text)',
                fontSize: 13,
                cursor: !hasMore ? 'default' : 'pointer',
                fontFamily: 'inherit',
              }}
            >
              Siguiente →
            </button>
          </div>
        </div>
      </div>
    </div>
  )
}
