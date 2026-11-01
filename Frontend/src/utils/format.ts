export const formatDateForShort = (isoString: string) => {
  return new Date(isoString).toLocaleString("pt-BR", {
    dateStyle: "short",
    timeStyle: "short",
  });
};
export const formatForDatetimeLocal = (dateString?: string): string => {
  if (!dateString) return "";
  const date = new Date(dateString);
  if (isNaN(date.getTime())) return "";

  const pad = (num: number) => num.toString().padStart(2, "0");
  const year = date.getFullYear();
  const month = pad(date.getMonth() + 1);
  const day = pad(date.getDate());
  const hours = pad(date.getHours());
  const minutes = pad(date.getMinutes());

  return `${year}-${month}-${day}T${hours}:${minutes}`;
};
export const formatWithBrazilTimezone = (dateTimeLocal: string): string => {
  if (!dateTimeLocal) return dateTimeLocal;
  if (/(Z|[+-]\d{2}:\d{2})$/i.test(dateTimeLocal)) {
    return dateTimeLocal;
  }
  const normalized =
    dateTimeLocal.length === 16 ? `${dateTimeLocal}:00` : dateTimeLocal;
  return `${normalized}-03:00`;
};
