import * as XLSX from "xlsx";
import type { PagedResult } from "@/types/utils";

export function exportToExcel<T extends Record<string, unknown>>(
  rows: T[],
  fileName: string,
  sheetName = "Dados",
): void {
  const worksheet = XLSX.utils.json_to_sheet(rows);

  if (rows.length > 0) {
    const keys = Object.keys(rows[0]);
    worksheet["!cols"] = keys.map((key) => {
      const maxCellLength = rows.reduce((max, row) => {
        const cellValue = row[key] != null ? String(row[key]) : "";
        return Math.max(max, cellValue.length);
      }, key.length);
      return { wch: Math.min(Math.max(maxCellLength + 2, 12), 60) };
    });
  }

  const workbook = XLSX.utils.book_new();
  XLSX.utils.book_append_sheet(workbook, worksheet, sheetName);

  const normalizedFileName = fileName.endsWith(".xlsx")
    ? fileName
    : `${fileName}.xlsx`;

  XLSX.writeFile(workbook, normalizedFileName);
}

export async function fetchAllPages<T>(
  fetchPage: (pageNumber: number, pageSize: number) => Promise<PagedResult<T>>,
  pageSize = 30,
): Promise<T[]> {
  const firstPage = await fetchPage(1, pageSize);
  const allItems = [...firstPage.items];

  const totalPages = Math.max(
    firstPage.totalPages,
    Math.ceil(firstPage.totalItems / pageSize),
  );

  if (totalPages > 1) {
    const remainingPages = Array.from(
      { length: totalPages - 1 },
      (_, idx) => idx + 2,
    );
    const results = await Promise.all(
      remainingPages.map((page) => fetchPage(page, pageSize)),
    );
    for (const pageResult of results) {
      allItems.push(...pageResult.items);
    }
  }

  return allItems;
}

