import { useQuery } from '@tanstack/react-query'
import {
  getItemGroupOptions,
  getCustomerGroupOptions,
  getSupplierGroupOptions,
  getWarehouseOptions,
  getSalespersonOptions,
} from '../api/nativeBiApi'

const STALE_TIME = 5 * 60 * 1000 // 5 min — filter options don't change often

export function useItemGroupOptions() {
  return useQuery({ queryKey: ['filter-options', 'item-groups'], queryFn: getItemGroupOptions, staleTime: STALE_TIME })
}

export function useCustomerGroupOptions() {
  return useQuery({ queryKey: ['filter-options', 'customer-groups'], queryFn: getCustomerGroupOptions, staleTime: STALE_TIME })
}

export function useSupplierGroupOptions() {
  return useQuery({ queryKey: ['filter-options', 'supplier-groups'], queryFn: getSupplierGroupOptions, staleTime: STALE_TIME })
}

export function useWarehouseOptions() {
  return useQuery({ queryKey: ['filter-options', 'warehouses'], queryFn: getWarehouseOptions, staleTime: STALE_TIME })
}

export function useSalespersonOptions() {
  return useQuery({ queryKey: ['filter-options', 'salespersons'], queryFn: getSalespersonOptions, staleTime: STALE_TIME })
}
