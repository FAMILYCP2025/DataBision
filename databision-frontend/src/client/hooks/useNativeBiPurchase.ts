import { useQuery } from '@tanstack/react-query'
import * as nbApi from '../api/nativeBiApi'

// ── Sprint 4 — Purchase MART hooks ────────────────────────────────────────────

export function usePurchaseMartKpi() {
  return useQuery({
    queryKey: ['nb-mart-purchase-kpi'],
    queryFn: () => nbApi.getPurchaseMartKpi(),
    staleTime: 15 * 60_000,
  })
}

export function usePurchaseMartByPeriod(months = 12) {
  return useQuery({
    queryKey: ['nb-mart-purchase-by-period', months],
    queryFn: () => nbApi.getPurchaseMartByPeriod(months),
    staleTime: 15 * 60_000,
  })
}

export function usePurchaseMartTopSuppliers(limit = 10) {
  return useQuery({
    queryKey: ['nb-mart-top-suppliers', limit],
    queryFn: () => nbApi.getPurchaseMartTopSuppliers(limit),
    staleTime: 15 * 60_000,
  })
}

export function usePurchaseMartTopItems(limit = 10) {
  return useQuery({
    queryKey: ['nb-mart-purchase-top-items', limit],
    queryFn: () => nbApi.getPurchaseMartTopItems(limit),
    staleTime: 15 * 60_000,
  })
}

export function usePurchaseMartOpenOrders(overdueOnly = false) {
  return useQuery({
    queryKey: ['nb-mart-purchase-open-orders', overdueOnly],
    queryFn: () => nbApi.getPurchaseMartOpenOrders(overdueOnly),
    staleTime: 5 * 60_000,
  })
}
