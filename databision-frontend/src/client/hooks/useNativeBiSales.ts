import { keepPreviousData, useQuery } from '@tanstack/react-query'
import type { PaginationParams, DateRangeParams } from '../types/nativeBi'
import * as nbApi from '../api/nativeBiApi'

export function useSalesOverview(params: DateRangeParams = {}) {
  return useQuery({
    queryKey: ['nb-sales-overview', params],
    queryFn: () => nbApi.getSalesOverview(params),
    staleTime: 5 * 60_000,
  })
}

export function useSalesDaily(params: DateRangeParams = {}) {
  return useQuery({
    queryKey: ['nb-sales-daily', params],
    queryFn: () => nbApi.getSalesDaily(params),
    staleTime: 5 * 60_000,
  })
}

export function useSalesMonthly(params: DateRangeParams = {}) {
  return useQuery({
    queryKey: ['nb-sales-monthly', params],
    queryFn: () => nbApi.getSalesMonthly(params),
    staleTime: 5 * 60_000,
  })
}

export function useSalesCustomers(params: PaginationParams = {}) {
  return useQuery({
    queryKey: ['nb-sales-customers', params],
    queryFn: () => nbApi.getSalesCustomers(params),
    staleTime: 5 * 60_000,
    placeholderData: keepPreviousData,
  })
}

export function useSalesItems(params: PaginationParams = {}) {
  return useQuery({
    queryKey: ['nb-sales-items', params],
    queryFn: () => nbApi.getSalesItems(params),
    staleTime: 5 * 60_000,
    placeholderData: keepPreviousData,
  })
}

export function useSalesSalespersons(params: PaginationParams = {}) {
  return useQuery({
    queryKey: ['nb-sales-salespersons', params],
    queryFn: () => nbApi.getSalesSalespersons(params),
    staleTime: 5 * 60_000,
    placeholderData: keepPreviousData,
  })
}
