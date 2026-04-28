import { format, formatDistanceToNow } from 'date-fns'
import { tr } from 'date-fns/locale'

/** "28 Nis 2026, 14:30" */
export function formatDate(iso: string): string {
  return format(new Date(iso), 'd MMM yyyy, HH:mm', { locale: tr })
}

/** "28 Nis 2026" */
export function formatDateShort(iso: string): string {
  return format(new Date(iso), 'd MMM yyyy', { locale: tr })
}

/** "3 saat önce" */
export function formatRelative(iso: string): string {
  return formatDistanceToNow(new Date(iso), { addSuffix: true, locale: tr })
}

/** ₺1.234,56 */
export function formatCurrency(amount: number): string {
  return new Intl.NumberFormat('tr-TR', {
    style: 'currency',
    currency: 'TRY',
    minimumFractionDigits: 2,
  }).format(amount)
}

/** +12 / -5 (renkli class için) */
export function qtySign(n: number): string {
  return n >= 0 ? `+${n}` : `${n}`
}
