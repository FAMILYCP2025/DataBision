import { useState } from 'react'
import NativeBiPageHeader from '../components/nativebi/NativeBiPageHeader'
import KpiCard from '../components/nativebi/KpiCard'
import SortableTable, { type ColumnDef } from '../components/nativebi/SortableTable'
import { NbErrorState, NbEmptyState } from '../components/nativebi/NativeBiState'
import {
  useBiFinanceExecutive,
  useBiFinanceArAging,
  useBiFinanceApAging,
} from '../hooks/useProcessBi'
import type { FinanceArAging, FinanceApAging } from '../types/processBi'
import type { NbPagedMeta, PaginationParams } from '../types/nativeBi'

function fmtAmt(n: number) {
  return n.toLocaleString('es-CL', { maximumFractionDigits: 0 })
}

function fmtPct(n: number) {
  return `${n.toLocaleString('es-CL', { maximumFractionDigits: 1 })}%`
}

function fmtDate(iso: string | null) {
  if (!iso) return '—'
  return new Date(iso + 'T00:00:00').toLocaleDateString('es-CL', {
    day: '2-digit', month: 'short', year: 'numeric',
  })
}

type Tab = 'resumen' | 'ar' | 'ap' | 'risk'

const LIMIT = 20
const EMPTY_META: NbPagedMeta = { limit: LIMIT, offset: 0, count: 0, hasMore: false }

function initPag(sortBy: string): PaginationParams {
  return { limit: LIMIT, offset: 0, sortBy, sortDir: 'desc' }
}

const tabs: { id: Tab; label: string }[] = [
  { id: 'resumen', label: 'Resumen' },
  { id: 'ar',      label: 'Cuentas por cobrar' },
  { id: 'ap',      label: 'Cuentas por pagar' },
  { id: 'risk',    label: 'Riesgo +90d' },
]

export default function FinanceDashboardPage() {
  const [tab, setTab] = useState<Tab>('resumen')
  const [arP, setArP] = useState<PaginationParams>(initPag('overdueAmount'))
  const [apP, setApP] = useState<PaginationParams>(initPag('overdueAmount'))

  const { data: execData, isLoading: loadingExec, error: execErr, refetch: refetchExec } = useBiFinanceExecutive(30)
  const { data: arData, isLoading: loadingAr } = useBiFinanceArAging(arP)
  const { data: apData, isLoading: loadingAp } = useBiFinanceApAging(apP)

  // KPIs from most recent period
  const latest      = execData && execData.length > 0 ? execData[execData.length - 1] : null
  const totalArOv   = execData?.reduce((s, d) => s + d.arOverdue, 0) ?? 0
  const totalInvAmt = execData?.reduce((s, d) => s + d.newInvoicesAmount, 0) ?? 0
  const totalInvCnt = execData?.reduce((s, d) => s + d.newInvoicesCount, 0) ?? 0
  const avgOvPct    = latest?.arOverduePct ?? 0
  // Clients with aging over 90 days (client-side filter from already-fetched AR data)
  const riskItems   = arData?.data.filter((r) => r.aging90Plus > 0) ?? []

  const arCols: ColumnDef<FinanceArAging>[] = [
    {
      key: 'name',
      label: 'Cliente',
      render: (r) => (
        <div>
          <div style={{ fontWeight: 500 }}>{r.cardName ?? r.cardCode}</div>
          <div style={{ fontSize: 11.5, color: 'var(--c-text-faint)' }}>{r.cardCode}</div>
        </div>
      ),
    },
    {
      key: 'balanceDue',
      label: 'Saldo',
      sortKey: 'balanceDue',
      align: 'right',
      render: (r) => <span style={{ fontVariantNumeric: 'tabular-nums' }}>{fmtAmt(r.balanceDue)}</span>,
    },
    {
      key: 'overdueAmount',
      label: 'Vencido',
      sortKey: 'overdueAmount',
      align: 'right',
      render: (r) => (
        <span style={{ fontVariantNumeric: 'tabular-nums', color: r.overdueAmount > 0 ? '#DC2626' : 'inherit' }}>
          {fmtAmt(r.overdueAmount)}
        </span>
      ),
    },
    {
      key: '0-30',
      label: '0-30d',
      align: 'right',
      render: (r) => <span style={{ fontVariantNumeric: 'tabular-nums' }}>{fmtAmt(r.aging0To30)}</span>,
    },
    {
      key: '31-60',
      label: '31-60d',
      align: 'right',
      render: (r) => <span style={{ fontVariantNumeric: 'tabular-nums' }}>{fmtAmt(r.aging31To60)}</span>,
    },
    {
      key: '90+',
      label: '+90d',
      align: 'right',
      render: (r) => (
        <span style={{ fontVariantNumeric: 'tabular-nums', color: r.aging90Plus > 0 ? '#DC2626' : 'inherit' }}>
          {fmtAmt(r.aging90Plus)}
        </span>
      ),
    },
    {
      key: 'lastInvoice',
      label: 'Últ. factura',
      align: 'right',
      render: (r) => fmtDate(r.lastInvoiceDate),
    },
  ]

  const apCols: ColumnDef<FinanceApAging>[] = [
    {
      key: 'name',
      label: 'Proveedor',
      render: (r) => (
        <div>
          <div style={{ fontWeight: 500 }}>{r.supplierName ?? r.supplierCode}</div>
          <div style={{ fontSize: 11.5, color: 'var(--c-text-faint)' }}>{r.supplierCode}</div>
        </div>
      ),
    },
    {
      key: 'balanceDue',
      label: 'Saldo',
      sortKey: 'balanceDue',
      align: 'right',
      render: (r) => <span style={{ fontVariantNumeric: 'tabular-nums' }}>{fmtAmt(r.balanceDue)}</span>,
    },
    {
      key: 'overdueAmount',
      label: 'Vencido',
      sortKey: 'overdueAmount',
      align: 'right',
      render: (r) => (
        <span style={{ fontVariantNumeric: 'tabular-nums', color: r.overdueAmount > 0 ? '#DC2626' : 'inherit' }}>
          {fmtAmt(r.overdueAmount)}
        </span>
      ),
    },
    {
      key: '0-30',
      label: '0-30d',
      align: 'right',
      render: (r) => <span style={{ fontVariantNumeric: 'tabular-nums' }}>{fmtAmt(r.aging0To30)}</span>,
    },
    {
      key: '31-60',
      label: '31-60d',
      align: 'right',
      render: (r) => <span style={{ fontVariantNumeric: 'tabular-nums' }}>{fmtAmt(r.aging31To60)}</span>,
    },
    {
      key: '90+',
      label: '+90d',
      align: 'right',
      render: (r) => (
        <span style={{ fontVariantNumeric: 'tabular-nums', color: r.aging90Plus > 0 ? '#DC2626' : 'inherit' }}>
          {fmtAmt(r.aging90Plus)}
        </span>
      ),
    },
  ]

  return (
    <div className="cp-page">
      <NativeBiPageHeader
        title="Finanzas"
        description="Cuentas por cobrar y por pagar — vencimientos y aging"
      />

      {execErr ? (
        <NbErrorState
          message="Error al cargar datos financieros."
          onRetry={() => refetchExec()}
        />
      ) : (
        <div className="nb-card-grid">
          <KpiCard
            label="AR vencido"
            value={loadingExec ? '—' : fmtAmt(totalArOv)}
            subLabel="Cuentas por cobrar vencidas"
            loading={loadingExec}
          />
          <KpiCard
            label="% vencido (último período)"
            value={loadingExec ? '—' : fmtPct(avgOvPct)}
            loading={loadingExec}
          />
          <KpiCard
            label="Facturas emitidas (30d)"
            value={totalInvCnt}
            loading={loadingExec}
          />
          <KpiCard
            label="Monto facturado (30d)"
            value={loadingExec ? '—' : fmtAmt(totalInvAmt)}
            loading={loadingExec}
          />
        </div>
      )}

      <div className="db-card">
        <div
          className="db-card-header nb-tab-bar"
          style={{ paddingLeft: 4, paddingRight: 16, gap: 0, borderBottom: '1px solid var(--c-border)', overflowX: 'auto' }}
          role="tablist"
          aria-label="Secciones de finanzas"
        >
          {tabs.map((t) => (
            <button
              key={t.id}
              role="tab"
              aria-selected={tab === t.id}
              onClick={() => setTab(t.id)}
              style={{
                padding: '0 16px',
                height: 44,
                background: 'none',
                border: 'none',
                borderBottom: tab === t.id ? '2px solid var(--brand-primary, #2563EB)' : '2px solid transparent',
                color: tab === t.id ? 'var(--brand-primary, #2563EB)' : 'var(--c-text-muted)',
                fontWeight: tab === t.id ? 600 : 500,
                fontSize: 13.5,
                cursor: 'pointer',
                fontFamily: 'inherit',
                marginBottom: -1,
                transition: 'color 150ms, border-color 150ms',
              }}
            >
              {t.label}
            </button>
          ))}
        </div>

        {tab === 'resumen' && (
          loadingExec ? (
            <div style={{ padding: 24 }}>
              {Array.from({ length: 4 }).map((_, i) => (
                <div key={i} className="cp-skeleton" style={{ height: 44, borderRadius: 6, marginBottom: 8 }} />
              ))}
            </div>
          ) : !execData || execData.length === 0 ? (
            <NbEmptyState
              message="Sin datos financieros disponibles. Disponible al completar carga histórica."
              icon="table"
            />
          ) : (
            <div style={{ padding: '16px 20px' }}>
              <div style={{ fontSize: 12.5, fontWeight: 600, color: 'var(--c-text-muted)', marginBottom: 10, textTransform: 'uppercase', letterSpacing: '0.04em' }}>
                Evolución financiera — últimos 5 períodos
              </div>
              <div className="nb-table-scroll">
                <table className="db-table">
                  <thead>
                    <tr>
                      <th>Fecha</th>
                      <th style={{ textAlign: 'right' }}>AR vencido</th>
                      <th style={{ textAlign: 'right' }}>% vencido</th>
                      <th style={{ textAlign: 'right' }}>Facturas emitidas</th>
                      <th style={{ textAlign: 'right' }}>Monto facturado</th>
                    </tr>
                  </thead>
                  <tbody>
                    {execData.slice(-5).reverse().map((d, i) => (
                      <tr key={i}>
                        <td style={{ fontSize: 13 }}>{fmtDate(d.periodDate)}</td>
                        <td style={{ textAlign: 'right', fontVariantNumeric: 'tabular-nums', color: d.arOverdue > 0 ? '#DC2626' : 'inherit' }}>
                          {fmtAmt(d.arOverdue)}
                        </td>
                        <td style={{ textAlign: 'right', fontVariantNumeric: 'tabular-nums' }}>
                          {fmtPct(d.arOverduePct)}
                        </td>
                        <td style={{ textAlign: 'right', fontVariantNumeric: 'tabular-nums' }}>{d.newInvoicesCount}</td>
                        <td style={{ textAlign: 'right', fontVariantNumeric: 'tabular-nums' }}>{fmtAmt(d.newInvoicesAmount)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          )
        )}

        {tab === 'ar' && (
          arData?.data.length === 0 && !loadingAr ? (
            <NbEmptyState message="Sin datos de cuentas por cobrar en el período analizado." icon="table" />
          ) : (
            <SortableTable
              data={arData?.data ?? []}
              columns={arCols}
              meta={arData?.meta ?? EMPTY_META}
              sortBy={arP.sortBy}
              sortDir={arP.sortDir}
              onPageChange={(offset) => setArP((p) => ({ ...p, offset }))}
              onSortChange={(sortBy, sortDir) => setArP((p) => ({ ...p, sortBy, sortDir, offset: 0 }))}
              isLoading={loadingAr}
              rowKey={(r) => r.cardCode}
            />
          )
        )}

        {tab === 'ap' && (
          apData?.data.length === 0 && !loadingAp ? (
            <NbEmptyState message="Sin datos de cuentas por pagar en el ambiente de demo. Este indicador queda disponible al completar la carga histórica de facturas de proveedor." icon="table" />
          ) : (
            <SortableTable
              data={apData?.data ?? []}
              columns={apCols}
              meta={apData?.meta ?? EMPTY_META}
              sortBy={apP.sortBy}
              sortDir={apP.sortDir}
              onPageChange={(offset) => setApP((p) => ({ ...p, offset }))}
              onSortChange={(sortBy, sortDir) => setApP((p) => ({ ...p, sortBy, sortDir, offset: 0 }))}
              isLoading={loadingAp}
              rowKey={(r) => r.supplierCode}
            />
          )
        )}

        {tab === 'risk' && (
          riskItems.length === 0 && !loadingAr ? (
            <NbEmptyState message="Sin cuentas con aging superior a 90 días en el período analizado." icon="table" />
          ) : (
            <SortableTable
              data={riskItems}
              columns={arCols}
              meta={{ limit: riskItems.length, offset: 0, count: riskItems.length, hasMore: false }}
              isLoading={loadingAr}
              rowKey={(r) => r.cardCode}
              onPageChange={() => {}}
              onSortChange={() => {}}
            />
          )
        )}
      </div>
    </div>
  )
}
