import * as XLSX from 'xlsx'

/**
 * Descarga un archivo .xlsx con una sola hoja.
 * @param filename  Nombre del archivo SIN extensión
 * @param sheetName Nombre de la hoja
 * @param rows      Array de objetos — las claves se usan como encabezados
 */
export function exportXlsx(filename: string, sheetName: string, rows: Record<string, unknown>[]): void {
  const ws = XLSX.utils.json_to_sheet(rows)
  const wb = XLSX.utils.book_new()
  XLSX.utils.book_append_sheet(wb, ws, sheetName)
  XLSX.writeFile(wb, `${filename}.xlsx`)
}
