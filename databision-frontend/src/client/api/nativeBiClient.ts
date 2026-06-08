// Thin utilities for Native BI HTTP calls.
// Re-exports the shared Axios instance (JWT injection + 401/refresh already wired in lib/api.ts).
import api from '../../lib/api'

export { api }

type QsVal = string | number | boolean | undefined | null

export function nbQs(params: Record<string, QsVal>): string {
  const p = new URLSearchParams()
  for (const [k, v] of Object.entries(params)) {
    if (v !== undefined && v !== null && v !== '') p.set(k, String(v))
  }
  const s = p.toString()
  return s ? `?${s}` : ''
}
