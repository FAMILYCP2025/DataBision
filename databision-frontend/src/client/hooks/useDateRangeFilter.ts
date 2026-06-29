import { useState } from 'react'

export interface DateRange {
  fromYear: number
  fromMonth: number   // 1–12
  toYear: number
  toMonth: number     // 1–12
}

/** Devuelve el rango predeterminado: últimos N meses hasta hoy */
function defaultRange(months: number): DateRange {
  const to   = new Date()
  const from = new Date(to.getFullYear(), to.getMonth() - months + 1, 1)
  return {
    fromYear:  from.getFullYear(),
    fromMonth: from.getMonth() + 1,
    toYear:    to.getFullYear(),
    toMonth:   to.getMonth() + 1,
  }
}

/** Filtra registros con { year, month } dentro del rango seleccionado */
export function filterByRange<T extends { year: number; month: number }>(
  rows: T[],
  range: DateRange,
): T[] {
  const fromKey = range.fromYear * 100 + range.fromMonth
  const toKey   = range.toYear   * 100 + range.toMonth
  return rows.filter(r => {
    const key = r.year * 100 + r.month
    return key >= fromKey && key <= toKey
  })
}

export function useDateRangeFilter(defaultMonths = 12) {
  const [range, setRange] = useState<DateRange>(() => defaultRange(defaultMonths))

  function setFrom(year: number, month: number) {
    setRange(r => ({ ...r, fromYear: year, fromMonth: month }))
  }

  function setTo(year: number, month: number) {
    setRange(r => ({ ...r, toYear: year, toMonth: month }))
  }

  return { range, setFrom, setTo }
}

/** Desplaza un rango exactamente N años (default: -1) */
export function shiftRangeByYear(range: DateRange, years = -1): DateRange {
  return {
    fromYear:  range.fromYear  + years,
    fromMonth: range.fromMonth,
    toYear:    range.toYear    + years,
    toMonth:   range.toMonth,
  }
}
