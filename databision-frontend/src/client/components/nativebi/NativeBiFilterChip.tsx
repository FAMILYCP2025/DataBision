interface NativeBiFilterChipProps {
  label: string
  value: string
  onRemove: () => void
}

export default function NativeBiFilterChip({ label, value, onRemove }: NativeBiFilterChipProps) {
  return (
    <span
      style={{
        display: 'inline-flex',
        alignItems: 'center',
        gap: 4,
        padding: '2px 8px 2px 10px',
        borderRadius: 12,
        fontSize: 12,
        fontWeight: 500,
        background: 'var(--color-primary, #2563EB)1a',
        color: 'var(--color-primary, #2563EB)',
        border: '1px solid var(--color-primary, #2563EB)33',
        whiteSpace: 'nowrap',
      }}
    >
      <span style={{ color: 'var(--color-text-muted)', fontWeight: 400 }}>{label}:&nbsp;</span>
      {value}
      <button
        onClick={onRemove}
        style={{
          background: 'none',
          border: 'none',
          cursor: 'pointer',
          padding: 0,
          marginLeft: 2,
          lineHeight: 1,
          color: 'var(--color-primary, #2563EB)',
          opacity: 0.7,
          fontSize: 14,
        }}
        aria-label={`Quitar filtro ${label}`}
      >
        ×
      </button>
    </span>
  )
}
