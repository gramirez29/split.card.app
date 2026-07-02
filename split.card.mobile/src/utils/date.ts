export function toIsoDate(date: Date): string {
  return date.toISOString().slice(0, 10);
}

export function formatDisplayDate(isoDate: string): string {
  const [year, month, day] = isoDate.split('-');
  return `${day}/${month}/${year}`;
}
