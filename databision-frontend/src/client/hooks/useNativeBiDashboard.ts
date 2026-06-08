import { keepPreviousData, useQuery } from '@tanstack/react-query'
import type { PaginationParams } from '../types/nativeBi'
import * as nbApi from '../api/nativeBiApi'

export function useDashboardSummary() {
  return useQuery({
    queryKey: ['nb-dashboard-summary'],
    queryFn: nbApi.getDashboardSummary,
    staleTime: 5 * 60_000,
  })
}

export function useDashboardSalesDaily(days = 30) {
  return useQuery({
    queryKey: ['nb-dashboard-sales-daily', days],
    queryFn: () => nbApi.getDashboardSalesDaily(days),
    staleTime: 5 * 60_000,
  })
}

export function useDashboardSalesMonthly(months = 12) {
  return useQuery({
    queryKey: ['nb-dashboard-sales-monthly', months],
    queryFn: () => nbApi.getDashboardSalesMonthly(months),
    staleTime: 5 * 60_000,
  })
}

export function useDashboardTopCustomers(params: PaginationParams = {}) {
  return useQuery({
    queryKey: ['nb-dashboard-top-customers', params],
    queryFn: () => nbApi.getDashboardTopCustomers(params),
    staleTime: 5 * 60_000,
    placeholderData: keepPreviousData,
  })
}

export function useDashboardTopItems(params: PaginationParams = {}) {
  return useQuery({
    queryKey: ['nb-dashboard-top-items', params],
    queryFn: () => nbApi.getDashboardTopItems(params),
    staleTime: 5 * 60_000,
    placeholderData: keepPreviousData,
  })
}

export function useDashboardSalespersons(params: PaginationParams = {}) {
  return useQuery({
    queryKey: ['nb-dashboard-salespersons', params],
    queryFn: () => nbApi.getDashboardSalespersons(params),
    staleTime: 5 * 60_000,
    placeholderData: keepPreviousData,
  })
}
