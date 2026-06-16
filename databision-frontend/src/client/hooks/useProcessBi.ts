import { keepPreviousData, useQuery } from '@tanstack/react-query'
import type { PaginationParams } from '../types/nativeBi'
import * as pbApi from '../api/processBiApi'

// ── Sales process ─────────────────────────────────────────────────────────────

export function useBiSalesCustomers(params: PaginationParams = {}) {
  return useQuery({
    queryKey: ['pb-sales-customers', params],
    queryFn: () => pbApi.getBiSalesCustomers(params),
    staleTime: 5 * 60_000,
    placeholderData: keepPreviousData,
  })
}

export function useBiSalesItems(params: PaginationParams = {}) {
  return useQuery({
    queryKey: ['pb-sales-items', params],
    queryFn: () => pbApi.getBiSalesItems(params),
    staleTime: 5 * 60_000,
    placeholderData: keepPreviousData,
  })
}

export function useBiSalesFulfillment(days = 30) {
  return useQuery({
    queryKey: ['pb-sales-fulfillment', days],
    queryFn: () => pbApi.getBiSalesFulfillment(days),
    staleTime: 5 * 60_000,
  })
}

// ── Purchasing process ────────────────────────────────────────────────────────

export function useBiPurchasingExecutive(days = 30) {
  return useQuery({
    queryKey: ['pb-purchasing-executive', days],
    queryFn: () => pbApi.getBiPurchasingExecutive(days),
    staleTime: 5 * 60_000,
  })
}

export function useBiPurchasingSuppliers(params: PaginationParams = {}) {
  return useQuery({
    queryKey: ['pb-purchasing-suppliers', params],
    queryFn: () => pbApi.getBiPurchasingSuppliers(params),
    staleTime: 5 * 60_000,
    placeholderData: keepPreviousData,
  })
}

export function useBiPurchasingReceiving(params: PaginationParams = {}) {
  return useQuery({
    queryKey: ['pb-purchasing-receiving', params],
    queryFn: () => pbApi.getBiPurchasingReceiving(params),
    staleTime: 5 * 60_000,
    placeholderData: keepPreviousData,
  })
}

// ── Inventory process ─────────────────────────────────────────────────────────

export function useBiInventoryRotation(params: PaginationParams = {}) {
  return useQuery({
    queryKey: ['pb-inventory-rotation', params],
    queryFn: () => pbApi.getBiInventoryRotation(params),
    staleTime: 5 * 60_000,
    placeholderData: keepPreviousData,
  })
}

export function useBiInventoryStock(params: PaginationParams = {}) {
  return useQuery({
    queryKey: ['pb-inventory-stock', params],
    queryFn: () => pbApi.getBiInventoryStock(params),
    staleTime: 5 * 60_000,
    placeholderData: keepPreviousData,
  })
}

export function useBiInventoryWarehouses() {
  return useQuery({
    queryKey: ['pb-inventory-warehouses'],
    queryFn: () => pbApi.getBiInventoryWarehouses(),
    staleTime: 5 * 60_000,
  })
}

// ── Finance process ───────────────────────────────────────────────────────────

export function useBiFinanceExecutive(days = 30) {
  return useQuery({
    queryKey: ['pb-finance-executive', days],
    queryFn: () => pbApi.getBiFinanceExecutive(days),
    staleTime: 5 * 60_000,
  })
}

export function useBiFinanceArAging(params: PaginationParams = {}) {
  return useQuery({
    queryKey: ['pb-finance-ar-aging', params],
    queryFn: () => pbApi.getBiFinanceArAging(params),
    staleTime: 5 * 60_000,
    placeholderData: keepPreviousData,
  })
}

export function useBiFinanceApAging(params: PaginationParams = {}) {
  return useQuery({
    queryKey: ['pb-finance-ap-aging', params],
    queryFn: () => pbApi.getBiFinanceApAging(params),
    staleTime: 5 * 60_000,
    placeholderData: keepPreviousData,
  })
}

// ── Operations process ────────────────────────────────────────────────────────

export function useBiOperationsPipelineHealth() {
  return useQuery({
    queryKey: ['pb-operations-health'],
    queryFn: () => pbApi.getBiOperationsPipelineHealth(),
    staleTime: 2 * 60_000,
    refetchInterval: 5 * 60_000,
  })
}

export function useBiOperationsAlerts(params: PaginationParams = {}) {
  return useQuery({
    queryKey: ['pb-operations-alerts', params],
    queryFn: () => pbApi.getBiOperationsAlerts(params),
    staleTime: 2 * 60_000,
    placeholderData: keepPreviousData,
  })
}

export function useBiOperationsDataQuality(params: PaginationParams = {}) {
  return useQuery({
    queryKey: ['pb-operations-dq', params],
    queryFn: () => pbApi.getBiOperationsDataQuality(params),
    staleTime: 5 * 60_000,
    placeholderData: keepPreviousData,
  })
}
