import type { MovementType, NotificationStatus, OrderStatus } from '../../types'

// ─── OrderStatus Badge ────────────────────────────────────────────────────────

const ORDER_STATUS_MAP: Record<OrderStatus, { label: string; className: string }> = {
  Pending:   { label: 'Bekliyor',   className: 'bg-yellow-100 text-yellow-700' },
  Confirmed: { label: 'Onaylandı',  className: 'bg-blue-100 text-blue-700' },
  Rejected:  { label: 'Reddedildi', className: 'bg-red-100 text-red-700' },
  Delivered: { label: 'Teslim',     className: 'bg-green-100 text-green-700' },
  Cancelled: { label: 'İptal',      className: 'bg-gray-100 text-gray-600' },
}

export function OrderStatusBadge({ status }: { status: string }) {
  const cfg = ORDER_STATUS_MAP[status as OrderStatus] ?? { label: status, className: 'bg-gray-100 text-gray-600' }
  return (
    <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${cfg.className}`}>
      {cfg.label}
    </span>
  )
}

// ─── MovementType Badge ───────────────────────────────────────────────────────

const MOVEMENT_MAP: Record<MovementType, { label: string; className: string }> = {
  InitialBalance:       { label: 'Başlangıç',     className: 'bg-gray-100 text-gray-600' },
  OrderConfirm:         { label: 'Sipariş Onayı', className: 'bg-blue-100 text-blue-700' },
  OrderCancel:          { label: 'Sipariş İptal',  className: 'bg-orange-100 text-orange-700' },
  OrderReject:          { label: 'Sipariş Red',    className: 'bg-red-100 text-red-700' },
  ManualAdjustment:     { label: 'Manuel',         className: 'bg-purple-100 text-purple-700' },
  Return:               { label: 'İade',           className: 'bg-teal-100 text-teal-700' },
  ForceConfirmNegative: { label: 'Zorla Onay',     className: 'bg-red-200 text-red-800' },
}

export function MovementTypeBadge({ type }: { type: string }) {
  const cfg = MOVEMENT_MAP[type as MovementType] ?? { label: type, className: 'bg-gray-100 text-gray-600' }
  return (
    <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${cfg.className}`}>
      {cfg.label}
    </span>
  )
}

// ─── NotificationStatus Badge ─────────────────────────────────────────────────

const NOTIF_MAP: Record<NotificationStatus, { label: string; className: string }> = {
  Pending: { label: 'Bekliyor', className: 'bg-yellow-100 text-yellow-700' },
  Sent:    { label: 'Gönderildi', className: 'bg-green-100 text-green-700' },
  Failed:  { label: 'Başarısız', className: 'bg-red-100 text-red-700' },
}

export function NotificationStatusBadge({ status }: { status: string }) {
  const cfg = NOTIF_MAP[status as NotificationStatus] ?? { label: status, className: 'bg-gray-100 text-gray-600' }
  return (
    <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${cfg.className}`}>
      {cfg.label}
    </span>
  )
}
