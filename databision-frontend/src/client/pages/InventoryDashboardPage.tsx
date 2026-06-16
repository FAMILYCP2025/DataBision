import { useState } from 'react'
import NativeBiPageHeader from '../components/nativebi/NativeBiPageHeader'
import KpiCard from '../components/nativebi/KpiCard'
import SortableTable, { type ColumnDef } from '../components/nativebi/SortableTable'
import { NbErrorState, NbEmptyState } from '../components/nativebi/NativeBiState'
import {
  useBiInventoryRotation,
  useBiInventoryWarehouses,
} from '../hooks/useProcessBi'
import type { InventoryRotation, InventoryWarehouse } from '../types/processBi'
import type { NbPagedMeta, PaginationParams } from '../types/nativeBi'

function fmtQty(n: number | null) {
  if (n === null || n === undefined) return '—'
  return n.toLocaleString('es-CL', { maximumFractionDigits: 2 })
}

function fmtDate(iso: string | null) {
  if (!iso) return '—'
  return new Date(iso + 'T00:00:00').toLocaleDateString('es-CL', {
    day: '2-digit', month: 'short', year: 'numeric',
  })
}

const ROTATION_LABEL: Record<string, string> = {
  FAST:        'Alta',
  NORMAL:      'Normal',
  SLOW:        'Baja',
  NO_MOVEMENT: 'Sin movimiento',
}

const ROTATION_COLOR: Record<string, string> = {
  FAST:        '#16A34A',
  NORMAL:      '#2563EB',
  SLOW:        '#D97706',
  NO_MOVEMENT: '#94A3B8',
}

type Tab = 'rotation' | 'warehouses' | 'no-movement'

const LIMIT = 20
const EMPTY_META: NbPagedMeta = { limit: LIMIT, offset: 0, count: 0, hasMore: false }

function initPag(sortBy: string): PaginationParams {
  return { limit: LIMIT, offset: 0, sortBy, sortDir: 'desc' }
}

const tabs: { id: Tab; label: string }[] = [
  { id: 'rotation', label: 'Rotación' },
  { id: 'warehouses', label: 'Almacenes' },
  { id: 'no-movement', label: 'Sin movimiento' },
]

export default function InventoryDashboardPage() {
  const [tab, setTab] = useState<Tab>('rotation')
  const [rotP, setRotP] = useState<PaginationParams>(initPag('qtySold90d'))

  const { data: rotData, isLoading: loadingRot, error: rotErr, refetch: refetchRot } = useBiInventoryRotation(rotP)
  const { data: whData, isLoading: loadingWh, error: whErr, refetch: refetchWh } = useBiInventoryWarehouses()

  const allRotation    = rotData?.data ?? []
  const fastCount      = allRotation.filter((r) => r.rotationStatus === 'FAST').length
  const normalCount    = allRotation.filter((r) => r.rotationStatus === 'NORMAL').length
  const slowCount      = allRotation.filter((r) => r.rotationStatus === 'SLOW').length
  const noMoveCount    = allRotation.filter((r) => r.rotationStatus === 'NO_MOVEMENT').length
  const noMoveItems    = allRotation.filter((r) => r.rotationStatus === 'NO_MOVEMENT')

  const rotCols: ColumnDef<InventoryRotation>[] = [
    {
      key: 'item',
      label: 'Artículo',
      render: (r) => (
        <div>
          <div style={{ fontWeight: 500 }}>{r.itemName ?? r.itemCode}</div>
          <div style={{ fontSize: 11.5, color: 'var(--c-text-faint)' }}>{r.itemCode}</div>
        </div>
      ),
    },
    {
      key: 'status',
      label: 'Rotación',
      render: (r) => (
        <span
          style={{
            display: 'inline-block',
            padding: '2px 8px',
            borderRadius: 4,
            fontSize: 12,
            fontWeight: 600,
            color: '#fff',
            backgroundColor: ROTATION_COLOR[r.rotationStatus] ?? '#94A3B8',
          }}
        >
          {ROTATION_LABEL[r.rotationStatus] ?? r.rotationStatus}
        </span>
      ),
    },
    {
      key: 'qty30',
      label: 'Cant. 30d',
      sortKey: 'qtySold30d',
      align: 'right',
      render: (r) => <span style={{ fontVariantNumeric: 'tabular-nums' }}>{fmtQty(r.qtySold30d)}</span>,
    },
    {
      key: 'qty90',
      label: 'Cant. 90d',
      sortKey: 'qtySold90d',
      align: 'right',
      render: (r) => <span style={{ fontVariantNumeric: 'tabular-nums' }}>{fmtQty(r.qtySold90d)}</span>,
    },
    {
      key: 'coverage',
      label: 'Cobertura días',
      align: 'right',
      render: (r) => (
        <span style={{ fontVariantNumeric: 'tabular-nums' }}>
          {r.coverageDays !== null && r.coverageDays !== undefined ? fmtQty(r.coverageDays) : '—'}
        </span>
      ),
    },
    {
      key: 'lastSale',
      label: 'Última venta',
      align: 'right',
      render: (r) => fmtDate(r.lastSaleDate),
    },
  ]

  const whCols: ColumnDef<InventoryWarehouse>[] = [
    {
      key: 'name',
      label: 'Almacén',
      render: (r) => (
        <div>
          <div style={{ fontWeight: 500 }}>{r.warehouseName ?? r.warehouseCode}</div>
          <div style={{ fontSize: 11.5, color: 'var(--c-text-faint)' }}>{r.warehouseCode}</div>
        </div>
      ),
    },
    {
      key: 'inCount',
      label: 'Entradas',
      align: 'right',
      render: (r) => <span style={{ fontVariantNumeric: 'tabular-nums' }}>{r.transferInCount}</span>,
    },
    {
      key: 'inQty',
      label: 'Cant. entrada',
      align: 'right',
      render: (r) => <span style={{ fontVariantNumeric: 'tabular-nums' }}>{fmtQty(r.transferInQty)}</span>,
    },
    {
      key: 'outCount',
      label: 'Salidas',
      align: 'right',
      render: (r) => <span style={{ fontVariantNumeric: 'tabular-nums' }}>{r.transferOutCount}</span>,
    },
    {
      key: 'outQty',
      label: 'Cant. salida',
      align: 'right',
      render: (r) => <span style={{ fontVariantNumeric: 'tabular-nums' }}>{fmtQty(r.transferOutQty)}</span>,
    },
    {
      key: 'lastTransfer',
      label: 'Último traslado',
      align: 'right',
      render: (r) => fmtDate(r.lastTransferDate),
    },
  ]

  const rotLoading = loadingRot && !rotData

  return (
    <div className="cp-page">
      <NativeBiPageHeader
        title="Inventario"
        description="Rotación de artículos y movimientos por almacén"
      />

      {rotErr ? (
        <NbErrorState
          message="Error al cargar datos de inventario."
          onRetry={() => refetchRot()}
        />
      ) : (
        <div className="nb-card-grid">
          <KpiCard label="Alta rotación" value={fastCount} loading={rotLoading} />
          <KpiCard label="Rotación normal" value={normalCount} loading={rotLoading} />
          <KpiCard label="Baja rotación" value={slowCount} loading={rotLoading} />
          <KpiCard label="Sin movimiento" value={noMoveCount} loading={rotLoading} />
        </div>
      )}

      {whErr ? (
        <NbErrorState
          message="Error al cargar datos de almacenes."
          onRetry={() => refetchWh()}
        />
      ) : null}

      {/* Tabbed tables */}
      <div className="db-card">
        <div
          className="db-card-header nb-tab-bar"
          style={{ paddingLeft: 4, paddingRight: 16, gap: 0, borderBottom: '1px solid var(--c-border)', overflowX: 'auto' }}
          role="tablist"
          aria-label="Secciones de inventario"
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

        {tab === 'rotation' && (
          allRotation.length === 0 && !loadingRot ? (
            <NbEmptyState message="Sin datos de rotación de artículos en el período analizado." icon="table" />
          ) : (
            <SortableTable
              data={allRotation}
              columns={rotCols}
              meta={rotData?.meta ?? EMPTY_META}
              sortBy={rotP.sortBy}
              sortDir={rotP.sortDir}
              onPageChange={(offset) => setRotP((p) => ({ ...p, offset }))}
              onSortChange={(sortBy, sortDir) => setRotP((p) => ({ ...p, sortBy, sortDir, offset: 0 }))}
              isLoading={loadingRot}
              rowKey={(r) => r.itemCode}
            />
          )
        )}

        {tab === 'warehouses' && (
          loadingWh ? (
            <div style={{ padding: 24 }}>
              {Array.from({ length: 5 }).map((_, i) => (
                <div key={i} className="cp-skeleton" style={{ height: 44, borderRadius: 6, marginBottom: 8 }} />
              ))}
            </div>
          ) : whData?.length === 0 ? (
            <NbEmptyState message="Stock por almacén pendiente de habilitar según endpoint disponible en Service Layer. Actualmente se muestran movimientos y rotación." icon="table" />
          ) : (
            <SortableTable
              data={whData ?? []}
              columns={whCols}
              meta={{ limit: whData?.length ?? 0, offset: 0, count: whData?.length ?? 0, hasMore: false }}
              isLoading={false}
              rowKey={(r) => r.warehouseCode}
              onPageChange={() => {}}
              onSortChange={() => {}}
            />
          )
        )}

        {tab === 'no-movement' && (
          noMoveItems.length === 0 && !loadingRot ? (
            <NbEmptyState message="Sin artículos sin movimiento en el período analizado." icon="table" />
          ) : (
            <SortableTable
              data={noMoveItems}
              columns={rotCols}
              meta={{ limit: noMoveItems.length, offset: 0, count: noMoveItems.length, hasMore: false }}
              isLoading={rotLoading}
              rowKey={(r) => r.itemCode}
              onPageChange={() => {}}
              onSortChange={() => {}}
            />
          )
        )}
      </div>
    </div>
  )
}
