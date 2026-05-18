export default function SessionSplash() {
  return (
    <div
      style={{
        position: 'fixed',
        inset: 0,
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        flexDirection: 'column',
        gap: 12,
        background: 'var(--color-background, #F8FAFC)',
        color: 'var(--color-muted, #64748B)',
        fontFamily: 'Inter, system-ui, sans-serif',
        fontSize: 14,
      }}
    >
      <div
        style={{
          width: 32,
          height: 32,
          border: '3px solid var(--color-border, #E2E8F0)',
          borderTopColor: 'var(--brand-primary, #2563EB)',
          borderRadius: '50%',
          animation: 'db-spin 0.8s linear infinite',
        }}
      />
      <div>Restaurando sesión…</div>
      <style>{`@keyframes db-spin { to { transform: rotate(360deg); } }`}</style>
    </div>
  )
}
