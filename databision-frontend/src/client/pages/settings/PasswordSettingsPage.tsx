import { useState } from 'react'
import { useMutation } from '@tanstack/react-query'
import { changePassword } from '../../api/clientApi'

export default function PasswordSettingsPage() {
  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [validationError, setValidationError] = useState<string | null>(null)
  const [successMsg, setSuccessMsg] = useState<string | null>(null)

  const mutation = useMutation({
    mutationFn: () => changePassword(currentPassword, newPassword),
    onSuccess: () => {
      setSuccessMsg('Contraseña actualizada correctamente.')
      setCurrentPassword('')
      setNewPassword('')
      setConfirmPassword('')
      setValidationError(null)
    },
    onError: (err: { response?: { data?: { message?: string } } }) => {
      setSuccessMsg(null)
      setValidationError(
        err?.response?.data?.message ?? 'No se pudo cambiar la contraseña. Intenta de nuevo.'
      )
    },
  })

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setSuccessMsg(null)
    setValidationError(null)

    if (newPassword.length < 8) {
      setValidationError('La nueva contraseña debe tener al menos 8 caracteres.')
      return
    }
    if (newPassword !== confirmPassword) {
      setValidationError('Las contraseñas nuevas no coinciden.')
      return
    }

    mutation.mutate()
  }

  return (
    <div style={{ maxWidth: 480, padding: '32px 24px' }}>
      <h1 style={{ fontSize: 20, fontWeight: 700, color: 'var(--c-text)', margin: '0 0 4px' }}>
        Cambiar contraseña
      </h1>
      <p style={{ fontSize: 13.5, color: 'var(--c-text-muted)', margin: '0 0 28px' }}>
        Actualiza tu contraseña de acceso al portal.
      </p>

      <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: 18 }}>
        <div>
          <label style={{ display: 'block', fontSize: 13, fontWeight: 500, color: 'var(--c-text)', marginBottom: 6 }}>
            Contraseña actual
          </label>
          <input
            type="password"
            value={currentPassword}
            onChange={e => setCurrentPassword(e.target.value)}
            required
            autoComplete="current-password"
            style={{
              width: '100%',
              padding: '9px 12px',
              borderRadius: 6,
              border: '1px solid var(--c-border)',
              fontSize: 14,
              fontFamily: 'inherit',
              color: 'var(--c-text)',
              boxSizing: 'border-box',
              outline: 'none',
            }}
          />
        </div>

        <div>
          <label style={{ display: 'block', fontSize: 13, fontWeight: 500, color: 'var(--c-text)', marginBottom: 6 }}>
            Nueva contraseña
          </label>
          <input
            type="password"
            value={newPassword}
            onChange={e => setNewPassword(e.target.value)}
            required
            autoComplete="new-password"
            minLength={8}
            style={{
              width: '100%',
              padding: '9px 12px',
              borderRadius: 6,
              border: '1px solid var(--c-border)',
              fontSize: 14,
              fontFamily: 'inherit',
              color: 'var(--c-text)',
              boxSizing: 'border-box',
              outline: 'none',
            }}
          />
          <span style={{ fontSize: 12, color: 'var(--c-text-faint)', marginTop: 4, display: 'block' }}>
            Mínimo 8 caracteres.
          </span>
        </div>

        <div>
          <label style={{ display: 'block', fontSize: 13, fontWeight: 500, color: 'var(--c-text)', marginBottom: 6 }}>
            Confirmar nueva contraseña
          </label>
          <input
            type="password"
            value={confirmPassword}
            onChange={e => setConfirmPassword(e.target.value)}
            required
            autoComplete="new-password"
            style={{
              width: '100%',
              padding: '9px 12px',
              borderRadius: 6,
              border: '1px solid var(--c-border)',
              fontSize: 14,
              fontFamily: 'inherit',
              color: 'var(--c-text)',
              boxSizing: 'border-box',
              outline: 'none',
            }}
          />
        </div>

        {validationError && (
          <div style={{
            padding: '10px 14px',
            borderRadius: 6,
            background: '#FEF2F2',
            border: '1px solid #FECACA',
            color: '#B91C1C',
            fontSize: 13,
          }}>
            {validationError}
          </div>
        )}

        {successMsg && (
          <div style={{
            padding: '10px 14px',
            borderRadius: 6,
            background: '#F0FDF4',
            border: '1px solid #BBF7D0',
            color: '#15803D',
            fontSize: 13,
          }}>
            {successMsg}
          </div>
        )}

        <button
          type="submit"
          disabled={mutation.isPending}
          style={{
            alignSelf: 'flex-start',
            padding: '9px 20px',
            borderRadius: 6,
            border: 'none',
            background: mutation.isPending ? 'var(--c-text-faint)' : 'var(--brand-primary, #2563EB)',
            color: '#fff',
            fontSize: 13.5,
            fontWeight: 600,
            fontFamily: 'inherit',
            cursor: mutation.isPending ? 'default' : 'pointer',
          }}
        >
          {mutation.isPending ? 'Guardando…' : 'Cambiar contraseña'}
        </button>
      </form>
    </div>
  )
}
