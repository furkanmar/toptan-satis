import { useQuery } from '@tanstack/react-query'
import Layout from '../../components/Layout'
import { ordersApi } from '../../api/client'
import type { Order } from '../../types'

const NAV = [
  { to: '/store', label: 'Ürünler' },
  { to: '/store/orders', label: 'Siparişlerim' }
]

const STATUS_LABELS: Record<string, { label: string; color: string }> = {
  Pending:   { label: 'Bekliyor',   color: 'bg-yellow-100 text-yellow-700' },
  Confirmed: { label: 'Onaylandı',  color: 'bg-blue-100 text-blue-700' },
  Rejected:  { label: 'Reddedildi', color: 'bg-red-100 text-red-700' },
  Delivered: { label: 'Teslim',     color: 'bg-green-100 text-green-700' },
  Cancelled: { label: 'İptal',      color: 'bg-gray-100 text-gray-600' }
}

export default function StoreOrders() {
  const { data: orders = [], isLoading } = useQuery<Order[]>({
    queryKey: ['my-orders'],
    queryFn: ordersApi.myOrders
  })

  return (
    <Layout navLinks={NAV}>
      <h1 className="text-xl font-bold text-gray-900 mb-4">Siparişlerim</h1>
      {isLoading ? (
        <div className="text-center py-12 text-gray-400">Yükleniyor...</div>
      ) : orders.length === 0 ? (
        <div className="text-center py-12 text-gray-400">Henüz sipariş yok</div>
      ) : (
        <div className="space-y-3">
          {orders.map((order: Order) => {
            const { label, color } = STATUS_LABELS[order.status] ?? { label: order.status, color: 'bg-gray-100 text-gray-600' }
            return (
              <div key={order.id} className="bg-white rounded-xl border border-gray-200 p-4">
                <div className="flex items-center justify-between mb-2">
                  <div>
                    <span className="font-medium text-gray-900">{order.wholesalerName}</span>
                    <span className="text-xs text-gray-400 ml-2">{new Date(order.createdAt).toLocaleDateString('tr-TR')}</span>
                  </div>
                  <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${color}`}>{label}</span>
                </div>
                <div className="space-y-1">
                  {order.items.map((item, i) => (
                    <div key={i} className="flex justify-between text-sm text-gray-600">
                      <span>{item.productName} × {item.quantity}</span>
                      <span>₺{item.total.toFixed(2)}</span>
                    </div>
                  ))}
                </div>
                <div className="border-t border-gray-100 mt-2 pt-2 flex justify-between">
                  <span className="text-sm text-gray-500">{order.note && `Not: ${order.note}`}</span>
                  <span className="font-semibold text-gray-900">₺{order.totalAmount.toFixed(2)}</span>
                </div>
              </div>
            )
          })}
        </div>
      )}
    </Layout>
  )
}
