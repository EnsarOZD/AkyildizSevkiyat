import * as XLSX from 'xlsx';

/**
 * Veriyi Excel (.xlsx) dosyası olarak indirir.
 * @param rows    - Satır array'i (her satır { [sütun başlığı]: değer } objesi)
 * @param sheetName - Excel sheet adı
 * @param fileName  - İndirilecek dosya adı (.xlsx uzantısız)
 */
export function exportToExcel(
  rows: Record<string, unknown>[],
  sheetName: string,
  fileName: string,
): void {
  const worksheet = XLSX.utils.json_to_sheet(rows);
  const workbook = XLSX.utils.book_new();
  XLSX.utils.book_append_sheet(workbook, worksheet, sheetName);
  XLSX.writeFile(workbook, `${fileName}.xlsx`);
}
