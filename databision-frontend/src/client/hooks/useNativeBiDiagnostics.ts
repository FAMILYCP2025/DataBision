import { useQuery } from '@tanstack/react-query'
import * as nbApi from '../api/nativeBiApi'

export function useNativeBiDiagnostics() {
  return useQuery({
    queryKey: ['nb-diagnostics'],
    queryFn: nbApi.getNativeBiDiagnostics,
    staleTime: 5 * 60_000,
    refetchInterval: 5 * 60_000,
  })
}

export function useNativeBiTableCounts() {
  return useQuery({
    queryKey: ['nb-table-counts'],
    queryFn: nbApi.getNativeBiTableCounts,
    staleTime: 5 * 60_000,
  })
}
