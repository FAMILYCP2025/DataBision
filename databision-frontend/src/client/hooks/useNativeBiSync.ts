import { useQuery } from '@tanstack/react-query'
import * as nbApi from '../api/nativeBiApi'

export function useSyncStatus() {
  return useQuery({
    queryKey: ['nb-sync-status'],
    queryFn: nbApi.getSyncStatus,
    staleTime: 60_000,
    refetchInterval: 60_000,
  })
}

export function useSyncObjects() {
  return useQuery({
    queryKey: ['nb-sync-objects'],
    queryFn: nbApi.getSyncObjects,
    staleTime: 60_000,
  })
}

export function useSyncTransformStatus() {
  return useQuery({
    queryKey: ['nb-sync-transform'],
    queryFn: nbApi.getSyncTransformStatus,
    staleTime: 60_000,
  })
}
