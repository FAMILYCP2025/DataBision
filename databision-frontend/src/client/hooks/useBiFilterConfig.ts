import { useQuery } from '@tanstack/react-query'
import { getBiFilterConfig } from '../api/nativeBiApi'
import type { BiFilterConfigItem } from '../api/nativeBiApi'
import type { NativeBiFilterDefinition } from '../types/nativeBiFilters'

// Fetches filter config set by SuperAdmin for this tenant.
// Returns null when the tenant has no overrides or the endpoint is unavailable.
export function useBiFilterConfig() {
  return useQuery({
    queryKey: ['bi-filter-config'],
    queryFn: getBiFilterConfig,
    staleTime: 30 * 60_000,
    retry: false,
  })
}

// Applies admin label overrides to a set of static filter definitions.
// Filters marked disabled by the admin are removed.
// Filters not in the admin config are kept as-is (additive config).
export function applyFilterConfig(
  staticDefs: NativeBiFilterDefinition[],
  adminFilters: BiFilterConfigItem[] | undefined
): NativeBiFilterDefinition[] {
  if (!adminFilters || adminFilters.length === 0) return staticDefs

  const configMap = new Map(adminFilters.map((f) => [f.filterKey, f]))

  return staticDefs
    .filter((def) => {
      const cfg = configMap.get(def.key)
      return cfg ? cfg.isEnabled : true
    })
    .map((def) => {
      const cfg = configMap.get(def.key)
      if (!cfg) return def
      return {
        ...def,
        label: cfg.label ?? def.label,
        isAdvanced: cfg.isAdvanced,
        isEnabled: cfg.isEnabled,
      }
    })
    .sort((a, b) => {
      const aOrder = configMap.get(a.key)?.displayOrder ?? 0
      const bOrder = configMap.get(b.key)?.displayOrder ?? 0
      return aOrder - bOrder
    })
}
