import { useBiFinanceReadiness } from '../../hooks/useProcessBi'
import type { FinanceReadiness } from '../../types/processBi'

function StatusDot({ ok }: { ok: boolean }) {
  return (
    <span style={{
      display: 'inline-block', width: 8, height: 8, borderRadius: '50%',
      backgroundColor: ok ? '#16A34A' : '#DC2626', flexShrink: 0,
    }} />
  )
}

function LayerRow({ label, count }: { label: string; count: number }) {
  return (
    <div style={{ display: 'flex', alignItems: 'center', gap: 8, fontSize: 13, padding: '4px 0' }}>
      <StatusDot ok={count > 0} />
      <span style={{ color: 'var(--c-text)', minWidth: 200 }}>{label}</span>
      <span style={{ fontVariantNumeric: 'tabular-nums', color: count > 0 ? '#16A34A' : '#DC2626', fontWeight: 600 }}>
        {count > 0 ? count.toLocaleString() : 'Vacío'}
      </span>
    </div>
  )
}

function ReadinessBadge({ status }: { status: FinanceReadiness['readinessStatus'] }) {
  const cfg = {
    ready:   { bg: '#F0FDF4', color: '#15803D', label: 'Listo para demo' },
    warning: { bg: '#FFFBEB', color: '#92400E', label: 'Revisión requerida' },
    blocked: { bg: '#FEF2F2', color: '#991B1B', label: 'Bloqueado' },
  }[status]
  return (
    <span style={{
      padding: '3px 12px', borderRadius: 20, fontSize: 12, fontWeight: 700,
      backgroundColor: cfg.bg, color: cfg.color,
    }}>
      {cfg.label}
    </span>
  )
}

export default function NativeBiFinanceReadinessPanel() {
  const { data, isLoading, isError } = useBiFinanceReadiness()

  if (isLoading) {
    return (
      <div style={{ padding: '16px', fontSize: 13, color: 'var(--c-text-muted)' }}>
        Verificando disponibilidad de datos…
      </div>
    )
  }

  if (isError || !data) {
    return null
  }

  return (
    <div style={{
      border: '1px solid var(--c-border)', borderRadius: 8,
      backgroundColor: 'var(--c-surface)', marginBottom: 20,
    }}>
      {/* Header */}
      <div style={{
        display: 'flex', alignItems: 'center', gap: 12, justifyContent: 'space-between',
        padding: '12px 16px', borderBottom: '1px solid var(--c-border)',
        backgroundColor: 'var(--c-surface-subtle, #F8FAFC)',
      }}>
        <span style={{ fontSize: 13, fontWeight: 600, color: 'var(--c-text)' }}>Estado de datos contables</span>
        <ReadinessBadge status={data.readinessStatus} />
      </div>

      <div style={{ padding: '12px 16px' }}>
        {/* Layer status grid */}
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: '8px 24px', marginBottom: 16 }}>
          <div>
            <p style={{ fontSize: 11, fontWeight: 600, textTransform: 'uppercase', letterSpacing: '0.5px', color: 'var(--c-text-muted)', marginBottom: 6 }}>
              Capa RAW
            </p>
            <LayerRow label="OACT (maestro cuentas)" count={data.rawOactCount} />
            <LayerRow label="OJDT (cabeceras asiento)" count={data.rawOjdtCount} />
            <LayerRow label="JDT1 (líneas asiento)"   count={data.rawJdt1Count} />
          </div>
          <div>
            <p style={{ fontSize: 11, fontWeight: 600, textTransform: 'uppercase', letterSpacing: '0.5px', color: 'var(--c-text-muted)', marginBottom: 6 }}>
              Capa STG
            </p>
            <LayerRow label="stg.sap_oact" count={data.stgOactCount} />
            <LayerRow label="stg.sap_ojdt" count={data.stgOjdtCount} />
            <LayerRow label="stg.sap_jdt1" count={data.stgJdt1Count} />
          </div>
          <div>
            <p style={{ fontSize: 11, fontWeight: 600, textTransform: 'uppercase', letterSpacing: '0.5px', color: 'var(--c-text-muted)', marginBottom: 6 }}>
              Capa MART
            </p>
            <LayerRow label="gl_accounts"          count={data.martGlAccounts} />
            <LayerRow label="income_statement"      count={data.martIncomeStatement} />
            <LayerRow label="balance_sheet"         count={data.martBalanceSheet} />
            <LayerRow label="ebitda_summary"        count={data.martEbitda} />
          </div>
        </div>

        {/* Classification */}
        <div style={{ padding: '8px 12px', backgroundColor: 'var(--c-surface-subtle, #F8FAFC)', borderRadius: 6, marginBottom: 12, display: 'flex', gap: 24 }}>
          <div style={{ fontSize: 13 }}>
            <span style={{ color: 'var(--c-text-muted)' }}>Reglas clasificación: </span>
            <strong style={{ color: data.classificationRules > 0 ? '#16A34A' : '#D97706' }}>{data.classificationRules}</strong>
          </div>
          <div style={{ fontSize: 13 }}>
            <span style={{ color: 'var(--c-text-muted)' }}>Cuentas sin clasificar: </span>
            <strong style={{ color: data.unclassifiedPostable > 0 ? '#D97706' : '#16A34A' }}>{data.unclassifiedPostable}</strong>
          </div>
        </div>

        {/* Blocking reasons */}
        {data.blockingReasons.length > 0 && (
          <div style={{ backgroundColor: '#FEF2F2', border: '1px solid #FECACA', borderRadius: 6, padding: '10px 12px', marginBottom: 8 }}>
            <p style={{ fontSize: 12, fontWeight: 700, color: '#991B1B', marginBottom: 6 }}>Bloqueadores</p>
            {data.blockingReasons.map((r, i) => (
              <div key={i} style={{ fontSize: 12, color: '#991B1B', display: 'flex', gap: 6, marginBottom: 3 }}>
                <span>•</span><span>{r}</span>
              </div>
            ))}
          </div>
        )}

        {/* Warnings */}
        {data.warnings.length > 0 && (
          <div style={{ backgroundColor: '#FFFBEB', border: '1px solid #FDE68A', borderRadius: 6, padding: '10px 12px' }}>
            <p style={{ fontSize: 12, fontWeight: 700, color: '#92400E', marginBottom: 6 }}>Advertencias</p>
            {data.warnings.map((w, i) => (
              <div key={i} style={{ fontSize: 12, color: '#92400E', display: 'flex', gap: 6, marginBottom: 3 }}>
                <span>•</span><span>{w}</span>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  )
}
