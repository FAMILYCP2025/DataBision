import { useQuery } from '@tanstack/react-query'
import * as nbApi from '../api/nativeBiApi'

export function useInventoryMartKpi() {
  return useQuery({
    queryKey: ['nb-mart-inventory-kpi'],
    queryFn: () => nbApi.getInventoryMartKpi(),
    staleTime: 15 * 60_000,
  })
}

export function useInventoryMartSnapshot(limit = 50) {
  return useQuery({
    queryKey: ['nb-mart-inventory-snapshot', limit],
    queryFn: () => nbApi.getInventoryMartSnapshot(limit),
    staleTime: 15 * 60_000,
  })
}

export function useInventoryMartMovement(months = 12) {
  return useQuery({
    queryKey: ['nb-mart-inventory-movement', months],
    queryFn: () => nbApi.getInventoryMartMovement(months),
    staleTime: 15 * 60_000,
  })
}

export function useInventoryMartSlowMoving(minDays = 90) {
  return useQuery({
    queryKey: ['nb-mart-inventory-slow-moving', minDays],
    queryFn: () => nbApi.getInventoryMartSlowMoving(minDays),
    staleTime: 15 * 60_000,
  })
}

export function useInventoryMartWarehouses() {
  return useQuery({
    queryKey: ['nb-mart-inventory-warehouses'],
    queryFn: () => nbApi.getInventoryMartWarehouses(),
    staleTime: 15 * 60_000,
  })
}
