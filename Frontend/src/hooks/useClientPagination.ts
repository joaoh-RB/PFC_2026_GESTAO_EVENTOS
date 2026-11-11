import { useEffect, useMemo, useState } from "react";

export const TABLE_PAGE_SIZE = 20;

export function useClientPagination<T>(
  items: T[],
  resetKey: unknown,
  pageSize = TABLE_PAGE_SIZE,
) {
  const [page, setPage] = useState(1);
  const totalPages = Math.max(1, Math.ceil(items.length / pageSize));

  useEffect(() => {
    setPage(1);
  }, [resetKey]);

  useEffect(() => {
    if (page > totalPages) setPage(totalPages);
  }, [page, totalPages]);

  const pageItems = useMemo(() => {
    const start = (page - 1) * pageSize;
    return items.slice(start, start + pageSize);
  }, [items, page, pageSize]);

  return {
    page,
    setPage,
    totalPages,
    pageItems,
    totalItems: items.length,
    pageSize,
  };
}
