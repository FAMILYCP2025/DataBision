import { useState } from 'react'
import NativeBiPageHeader from '../components/nativebi/NativeBiPageHeader'
import KpiCard from '../components/nativebi/KpiCard'
import SortableTable, { type ColumnDef } from '../components/nativebi/SortableTable'
import { NbErrorState, NbEmptyState } from '../components/nativebi/NativeBiState'
import {
  useBiPurchasingExecutive,
  useBiPurchasingSuppliers,
  useBiPurchasingReceiving,
} from '../hooks/useProcessBi'
import type { PurchasingSupplier, PurchasingReceiving } from '../types/processBi'
import type { NbPagedMeta, PaginationParams } from '../types/nativeBi'

function fmtAmt(n: number) {
  return n.toLocaleString('es-CL', { maximumFractionDigits: 0 })
}

function fmtDate(iso: string | null) {
  if (!iso) return '—'
  return new Date(iso + 'T00:00:00').toLocaleDateString('es-CL', {
    day: '2-digit', month: 'short', year: 'numeric',
  })
}

type Tab = 'suppliers' | 'receiving'

const LIMIT = 20
const EMPTY_META: NbPagedMeta = { limit: LIMIT, offset: 0, count: 0, hasMore: false }

function initPag(sortBy: string): PaginationParams {
  return { limit: LIMIT, offset: 0, sortBy, sortDir: 'desc' }
}

const tabs: { id: Tab; label: string }[] = [
  { id: 'suppliers', label: 'Proveedores' },
  { id: 'receiving', label: 'Recepciones' },
]

export default function PurchasingDashboardPage() {
  const [tab, setTab] = useState<Tab>('suppliers')
  const [suppP, setSuppP] = useState<PaginationParams>(initPag('poAmount'))
  const [recvP, setRecvP] = useState<PaginationParams>(initPag('grAmount'))

  const { data: execData, isLoading: loadingExec, error: execErr, refetch: refetchExec } = useBiPurchasingExecutive(30)
  const { data: suppData, isLoading: loadingSupp } = useBiPurchasingSuppliers(suppP)
  const { data: recvData, isLoading: loadingRecv } = useBiPurchasingReceiving(recvP)

  // Aggregate KPIs from time-series
  const totalPo      = execData?.reduce((s, d) => s + d.poCount, 0) ?? 0
  const totalPoAmt   = execData?.reduce((s, d) => s + d.poAmount, 0) ?? 0
  const totalRecvAmt = execData?.reduce((s, d) => s + d.receivedAmount, 0) ?? 0
  const maxSuppliers = execData?.reduce((m, d) => Math.max(m, d.activeSuppliers), 0) ?? 0

  const suppCols: ColumnDef<PurchasingSupplier>[] = [
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
      key: 'poCount',
      label: 'OC',
      sortKey: 'poCount',
      align: 'right',
      render: (r) => <span style={{ fontVariantNumeric: 'tabular-nums' }}>{r.poCount}</span>,
    },
    {
      key: 'poAmount',
      label: 'Monto OC',
      sortKey: 'poAmount',
      align: 'right',
      render: (r) => <span style={{ fontVariantNumeric: 'tabular-nums' }}>{fmtAmt(r.poAmount)}</span>,
    },
    {
      key: 'receivedAmount',
      label: 'Recibido',
      sortKey: 'receivedAmount',
      align: 'right',
      render: (r) => <span style={{ fontVariantNumeric: 'tabular-nums' }}>{fmtAmt(r.receivedAmount)}</span>,
    },
    {
      key: 'avgPoAmount',
      label: 'Prom. OC',
      align: 'right',
      render: (r) => <span style={{ fontVariantNumeric: 'tabular-nums' }}>{fmtAmt(r.avgPoAmount)}</span>,
    },
    {
      key: 'lastPoDate',
      label: 'Última OC',
      align: 'right',
      render: (r) => fmtDate(r.lastPoDate),
    },
  ]

  const recvCols: ColumnDef<PurchasingReceiving>[] = [
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
      key: 'grCount',
      label: 'Recepciones',
      sortKey: 'grCount',
      align: 'right',
      render: (r) => <span style={{ fontVariantNumeric: 'tabular-nums' }}>{r.grCount}</span>,
    },
    {
      key: 'grAmount',
      label: 'Monto recibido',
      sortKey: 'grAmount',
      align: 'right',
      render: (r) => <span style={{ fontVariantNumeric: 'tabular-nums' }}>{fmtAmt(r.grAmount)}</span>,
    },
    {
      key: 'lastGrDate',
      label: 'Última recepción',
      align: 'right',
      render: (r) => fmtDate(r.lastGrDate),
    },
  ]

  return (
    <div className="cp-page">
      <NativeBiPageHeader
        title="Compras"
        description="Órdenes de compra, proveedores y recepciones — últimos 30 días"
      />

      {/* KPI cards */}
      {execErr ? (
        <NbErrorState
          message="Error al cargar datos de compras."
          onRetry={() => refetchExec()}
        />
      ) : (
        <div className="nb-card-grid">
          <KpiCard label="Órdenes de compra" value={totalPo} loading={loadingExec} />
          <KpiCard label="Monto OC" value={loadingExec ? '—' : fmtAmt(totalPoAmt)} loading={loadingExec} />
          <KpiCard label="Monto recibido" value={loadingExec ? '—' : fmtAmt(totalRecvAmt)} loading={loadingExec} />
          <KpiCard label="Proveedores activos" value={maxSuppliers} loading={loadingExec} />
        </div>
      )}

      {/* Tabbed tables */}
      <div className="db-card">
        <div
          className="db-card-header nb-tab-bar"
          style={{ paddingLeft: 4, paddingRight: 16, gap: 0, borderBottom: '1px solid var(--c-border)', overflowX: 'auto' }}
          role="tablist"
          aria-label="Secciones de compras"
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

        {tab === 'suppliers' && (
          suppData?.data.length === 0 && !loadingSupp ? (
            <NbEmptyState message="Sin datos de proveedores en el período analizado." icon="table" />
          ) : (
            <SortableTable
              data={suppData?.data ?? []}
              columns={suppCols}
              meta={suppData?.meta ?? EMPTY_META}
              sortBy={suppP.sortBy}
              sortDir={suppP.sortDir}
              onPageChange={(offset) => setSuppP((p) => ({ ...p, offset }))}
              onSortChange={(sortBy, sortDir) => setSuppP((p) => ({ ...p, sortBy, sortDir, offset: 0 }))}
              isLoading={loadingSupp}
              rowKey={(r) => r.supplierCode}
            />
          )
        )}

        {tab === 'receiving' && (
          recvData?.data.length === 0 && !loadingRecv ? (
            <NbEmptyState message="Sin datos de recepciones en el período analizado." icon="table" />
          ) : (
            <SortableTable
              data={recvData?.data ?? []}
              columns={recvCols}
              meta={recvData?.meta ?? EMPTY_META}
              sortBy={recvP.sortBy}
              sortDir={recvP.sortDir}
              onPageChange={(offset) => setRecvP((p) => ({ ...p, offset }))}
              onSortChange={(sortBy, sortDir) => setRecvP((p) => ({ ...p, sortBy, sortDir, offset: 0 }))}
              isLoading={loadingRecv}
              rowKey={(r) => r.supplierCode}
            />
          )
        )}
      </div>
    </div>
  )
}
