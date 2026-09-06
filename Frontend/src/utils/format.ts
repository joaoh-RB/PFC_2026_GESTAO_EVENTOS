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
