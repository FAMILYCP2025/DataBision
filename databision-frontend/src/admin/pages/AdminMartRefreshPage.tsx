import { useState } from 'react'
import api from '../../lib/api'

interface RefreshResult {
  module: string
  object: string
  rowsAffected: number
}

interface RefreshResponse {
  companyId: string
  refreshedAt: string
  results: RefreshResult[]
}

async function triggerMartRefresh(companyId: string): Promise<RefreshResponse> {
  const { data } = await api.post<{ data: RefreshResponse }>(
    `/admin/mart/refresh/${encodeURIComponent(companyId)}`
  )
  return data.data
}

type Status = 'idle' | 'loading' | 'success' | 'error'

export default function AdminMartRefreshPage() {
  const [companyId, setCompanyId] = useState('')
  const [status, setStatus]       = useState<Status>('idle')
  const [response, setResponse]   = useState<RefreshResponse | null>(null)
  const [errorMsg, setErrorMsg]   = useState('')

  async function handleRefresh() {
    if (!companyId.trim()) return
    setStatus('loading')
    setResponse(null)
    setErrorMsg('')
    try {
      const res = await triggerMartRefresh(companyId.trim())
      setResponse(res)
      setStatus('success')
    } catch (err: unknown) {
      const message =
        (err as { response?: { data?: { message?: string } } })?.response?.data?.message ??
        (err instanceof Error ? err.message : 'Error desconocido')
      setErrorMsg(message)
      setStatus('error')
    }
  }

  const totalRows = response?.results.reduce((s, r) => s + r.rowsAffected, 0) ?? 0

  return (
    <div className="db-page">
      <div className="db-page-header">
        <div>
          <h1 className="db-page-title">Refresh MART por Tenant</h1>
          <p className="db-page-subtitle">
            Ejecuta mart.refresh_sales, refresh_purchases, refresh_inventory y refresh_finance para una empresa.
          </p>
        </div>
      </div>

      <div className="db-card" style={{ padding: 24, maxWidth: 560 }}>
        <div style={{ display: 'flex', gap: 8, alignItems: 'flex-end', marginBottom: 20 }}>
          <div style={{ flex: 1 }}>
            <label style={{ display: 'block', fontSize: 12, fontWeight: 500, color: 'var(--c-text-muted)', marginBottom: 6 }}>
              Company ID (slug / analytics_company_id)
            </label>
            <input
              value={companyId}
              onChange={e => setCompanyId(e.target.value)}
              onKeyDown={e => e.key === 'Enter' && !status.includes('loading') && companyId && handleRefresh()}
              placeholder="ej: acme-corp"
              style={{
                width: '100%',
                padding: '8px 12px',
                borderRadius: 6,
                border: '1px solid var(--c-border)',
                fontSize: 14,
                fontFamily: 'inherit',
                outline: 'none',
                boxSizing: 'border-box',
              }}
            />
          </div>
          <button
            onClick={handleRefresh}
            disabled={status === 'loading' || !companyId.trim()}
            className="db-btn db-btn--primary"
          >
            {status === 'loading' ? 'Actualizando...' : 'Ejecutar Refresh MART'}
          </button>
        </div>

        {status === 'error' && (
          <div className="db-alert db-alert--error" role="alert" style={{ marginBottom: 16 }}>
            Error: {errorMsg}
          </div>
        )}
      </div>

      {status === 'success' && response && (
        <div className="db-card" style={{ marginTop: 20, overflow: 'hidden' }}>
          <div style={{ padding: '16px 20px', borderBottom: '1px solid var(--c-border)', display: 'flex', alignItems: 'center', gap: 12 }}>
            <span style={{ fontSize: 14, fontWeight: 600, color: '#16A34A' }}>
              ✓ Refresh completado
            </span>
            <span style={{ fontSize: 13, color: 'var(--c-text-muted)' }}>
              {response.companyId} · {new Date(response.refreshedAt).toLocaleString('es-CL')} ·{' '}
              {totalRows.toLocaleString('es-CL')} filas totales
            </span>
          </div>
          <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 13 }}>
            <thead>
              <tr style={{ background: 'var(--c-surface-alt, #F8FAFC)' }}>
                <th style={{ padding: '8px 16px', textAlign: 'left', fontWeight: 500, color: 'var(--c-text-muted)', borderBottom: '1px solid var(--c-border)' }}>
                  Módulo
                </th>
                <th style={{ padding: '8px 16px', textAlign: 'left', fontWeight: 500, color: 'var(--c-text-muted)', borderBottom: '1px solid var(--c-border)' }}>
                  Objeto
                </th>
                <th style={{ padding: '8px 16px', textAlign: 'right', fontWeight: 500, color: 'var(--c-text-muted)', borderBottom: '1px solid var(--c-border)' }}>
                  Filas
                </th>
              </tr>
            </thead>
            <tbody>
              {response.results.map((r, i) => (
                <tr
                  key={i}
                  style={{
                    height: 44,
                    borderBottom: '1px solid var(--c-border)',
                    background: i % 2 === 1 ? 'var(--c-surface-alt, #F8FAFC)' : undefined,
                  }}
                >
                  <td style={{ padding: '8px 16px', fontWeight: 500, textTransform: 'capitalize' }}>{r.module}</td>
                  <td style={{ padding: '8px 16px', fontFamily: 'monospace', fontSize: 12, color: 'var(--c-text-muted)' }}>
                    {r.object}
                  </td>
                  <td style={{ padding: '8px 16px', textAlign: 'right', fontVariantNumeric: 'tabular-nums' }}>
                    {r.rowsAffected.toLocaleString('es-CL')}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
