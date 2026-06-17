import { useState, useCallback } from 'react'
import type { NativeBiFilterState, FilterModule } from '../types/nativeBiFilters'
import { filterStateToParams } from '../utils/nativeBiFilterUtils'

export type UseNativeBiFiltersReturn = {
  filters: NativeBiFilterState
  setFilter: <K extends keyof NativeBiFilterState>(key: K, value: NativeBiFilterState[K]) => void
  resetFilter: (key: keyof NativeBiFilterState) => void
  resetAll: () => void
  params: Record<string, string | undefined>
  hasActiveFilters: boolean
}

// Filters that should be preserved when switching between pages
const PERSISTENT_FILTER_KEYS: (keyof NativeBiFilterState)[] = [
  'dateFrom', 'dateTo', 'year', 'month', 'salesType',
]

export function useNativeBiFilters(
  _module: FilterModule,
  initialState: NativeBiFilterState = {}
): UseNativeBiFiltersReturn {
  const [filters, setFilters] = useState<NativeBiFilterState>(initialState)

  const setFilter = useCallback(
    <K extends keyof NativeBiFilterState>(key: K, value: NativeBiFilterState[K]) => {
      setFilters((prev) => ({ ...prev, [key]: value }))
    },
    []
  )

  const resetFilter = useCallback((key: keyof NativeBiFilterState) => {
    setFilters((prev) => {
      const next = { ...prev }
      delete next[key]
      return next
    })
  }, [])

  const resetAll = useCallback(() => setFilters({}), [])

  const params = filterStateToParams(filters)
  const hasActiveFilters = Object.keys(params).length > 0

  return { filters, setFilter, resetFilter, resetAll, params, hasActiveFilters }
}

export { PERSISTENT_FILTER_KEYS }
