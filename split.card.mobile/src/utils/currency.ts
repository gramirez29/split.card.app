import type { Currency } from '@/types/enums';

const FORMATTERS: Record<Currency, Intl.NumberFormat> = {
  CRC: new Intl.NumberFormat('es-CR', { style: 'currency', currency: 'CRC', maximumFractionDigits: 0 }),
  USD: new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }),
};

export function formatCurrency(amount: number, currency: Currency): string {
  return FORMATTERS[currency].format(amount);
}
