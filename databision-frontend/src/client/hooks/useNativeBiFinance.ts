import { useQuery } from '@tanstack/react-query'
import * as nbApi from '../api/nativeBiApi'

export function useFinanceMartSummary() {
  return useQuery({
    queryKey: ['nb-mart-finance-summary'],
    queryFn: () => nbApi.getFinanceMartSummary(),
    staleTime: 15 * 60_000,
  })
}

export function useFinanceMartArAging(limit = 50) {
  return useQuery({
    queryKey: ['nb-mart-ar-aging', limit],
    queryFn: () => nbApi.getFinanceMartArAging(limit),
    staleTime: 15 * 60_000,
  })
}

export function useFinanceMartApAging(limit = 50) {
  return useQuery({
    queryKey: ['nb-mart-ap-aging', limit],
    queryFn: () => nbApi.getFinanceMartApAging(limit),
    staleTime: 15 * 60_000,
  })
}

export function useFinanceMartPeriodKpi(months = 12) {
  return useQuery({
    queryKey: ['nb-mart-finance-period-kpi', months],
    queryFn: () => nbApi.getFinanceMartPeriodKpi(months),
    staleTime: 15 * 60_000,
  })
}
