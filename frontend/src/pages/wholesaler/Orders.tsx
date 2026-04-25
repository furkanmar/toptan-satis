import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import Layout from '../../components/Layout'
import { ordersApi } from '../../api/client'
import type { Order } from '../../types'

const NAV = [
  { to: '/wholesaler/products', label: 'Ürünlerim' },
  { to: '/wholesaler/orders', label: 'Siparişler' }
]

const STATUS_LABELS: Record<string, { label: string; color: string }> = {
  Pending:   { label: 'Bekliyor',   color: 'bg-yellow-100 text-yellow-700' },
  Confirmed: { label: 'Onaylandı',  color: 'bg-blue-100 text-blue-700' },
  Rejected:  { label: 'Reddedildi', color: 'bg-red-100 text-red-700' },
  Delivered: { label: 'Teslim',     color: 'bg-green-100 text-green-700' },
  Cancelled: { label: 'İptal',      color: 'bg-gray-100 text-gray-600' }
}

const NEXT_STATUSES: Record<string, string[]> = {
  Pending:   ['Confirmed', 'Rejected'],
  Confirmed: ['Delivered'],
  Delivered: [],
  Rejected:  [],
  Cancelled: []
}

const STATUS_BUTTON_LABELS: Record<string, string> = {
  Confirmed: 'Onayla',
  Rejected:  'Reddet',
  Delivered: 'Teslim Edildi'
}

const STATUS_BUTTON_COLORS: Record<string, string> = {
  Confirmed: 'bg-blue-600 text-white hover:bg-blue-700',
  Rejected:  'bg-red-50 text-red-600 border border-red-200 hover:bg-red-100',
  Delivered: 'bg-green-600 text-white hover:bg-green-700'
}

export default function WholesalerOrders() {
  const qc = useQueryClient()

  const { data: orders = [], isLoading } = useQuery<Order[]>({
    queryKey: ['incoming-orders'],
    queryFn: ordersApi.incoming,
    refetchInterval: 30_000 // 30 sn'de bir yenile
  })

  const updateStatus = useMutation({
    mutationFn: ({ id, status }: { id: string; status: string }) =>
      ordersApi.updateStatus(id, status),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['incoming-orders'] })
  })

  return (
    <Layout navLinks={NAV}>
      <h1 className="text-xl font-bold text-gray-900 mb-4">Gelen Siparişler</h1>
      {isLoading ? (
        <div className="text-center py-12 text-gray-400">Yükleniyor...</div>
      ) : orders.length === 0 ? (
        <div className="text-center py-12 text-gray-400">Sipariş yok</div>
      ) : (
        <div className="space-y-3">
          {orders.map((order: Order) => {
            const { label, color } = STATUS_LABELS[order.status] ?? { label: order.status, color: 'bg-gray-100 text-gray-600' }
            const nextStatuses = NEXT_STATUSES[order.status] ?? []
            return (
              <div key={order.id} className="bg-white rounded-xl border border-gray-200 p-4">
                <div className="flex items-start justify-between mb-3">
                  <div>
                    <span className="font-medium text-gray-900">{order.storeName}</span>
                    <span className="text-xs text-gray-400 ml-2">{new Date(order.createdAt).toLocaleDateString('tr-TR', { day: 'numeric', month: 'long', hour: '2-digit', minute: '2-digit' })}</span>
                    {order.note && <p className="text-xs text-gray-500 mt-0.5">Not: {order.note}</p>}
                  </div>
                  <div className="flex items-center gap-2">
                    <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${color}`}>{label}</span>
                  </div>
                </div>
                <div className="space-y-1 mb-3">
                  {order.items.map((item, i) => (
                    <div key={i} className="flex justify-between text-sm text-gray-600">
                      <span>{item.productName} × {item.quantity}</span>
                      <span>₺{item.total.toFixed(2)}</span>
                    </div>
                  ))}
                </div>
                <div className="flex items-center justify-between border-t border-gray-100 pt-3">
                  <span className="font-semibold text-gray-900">Toplam: ₺{order.totalAmount.toFixed(2)}</span>
                  {nextStatuses.length > 0 && (
                    <div className="flex gap-2">
                      {nextStatuses.map(status => (
                        <button
                          key={status}
                          onClick={() => updateStatus.mutate({ id: order.id, status })}
                          disabled={updateStatus.isPending}
                          className={`px-3 py-1.5 rounded-lg text-xs font-medium disabled:opacity-50 transition-colors ${STATUS_BUTTON_COLORS[status] ?? ''}`}
                        >
                          {STATUS_BUTTON_LABELS[status] ?? status}
                        </button>
                      ))}
                    </div>
                  )}
                </div>
              </div>
            )
          })}
        </div>
      )}
    </Layout>
  )
}
