import { useParams } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import Layout from '../../components/Layout'
import { toast } from 'sonner'
import { ordersApi, deliveryNotesApi } from '../../api/client'
import type { Order } from '../../types'

async function openPdfBlob(url: string) {
  const token = localStorage.getItem('token')
  const res = await fetch(url, { headers: { Authorization: `Bearer ${token}` } })
  if (!res.ok) { toast.error('PDF indirilemedi'); return }
  const blob = await res.blob()
  const blobUrl = URL.createObjectURL(blob)
  window.open(blobUrl, '_blank')
  setTimeout(() => URL.revokeObjectURL(blobUrl), 10_000)
}

const NAV = (wholesalerId: string) => [
  { to: `/store/${wholesalerId}`, label: 'Ürünler' },
  { to: `/store/${wholesalerId}/orders`, label: 'Siparişlerim' },
  { to: `/store/${wholesalerId}/credit`, label: 'Veresiye' },
  { to: `/store/${wholesalerId}/cart`, label: 'Sepetim' },
  { to: '/store', label: '← Toptancı Seç' }
]

const STATUS_LABELS: Record<string, { label: string; color: string }> = {
  Pending:   { label: 'Bekliyor',   color: 'bg-yellow-100 text-yellow-700' },
  Confirmed: { label: 'Onaylandı',  color: 'bg-blue-100 text-blue-700' },
  Rejected:  { label: 'Reddedildi', color: 'bg-red-100 text-red-700' },
  Delivered: { label: 'Teslim',     color: 'bg-green-100 text-green-700' },
  Cancelled: { label: 'İptal',      color: 'bg-gray-100 text-gray-600' }
}

export default function StoreOrders() {
  const { wholesalerId } = useParams<{ wholesalerId: string }>()

  const { data: allOrders = [], isLoading } = useQuery<Order[]>({
    queryKey: ['my-orders'],
    queryFn: ordersApi.myOrders
  })

  // Bu toptancıya ait siparişleri filtrele
  const orders = allOrders.filter(o => o.wholesalerId === wholesalerId)
  const nav = NAV(wholesalerId ?? '')

  return (
    <Layout navLinks={nav}>
      <h1 className="text-xl font-bold text-gray-900 mb-4">Siparişlerim</h1>
      {isLoading ? (
        <div className="text-center py-12 text-gray-400">Yükleniyor...</div>
      ) : orders.length === 0 ? (
        <div className="text-center py-12 text-gray-400">Henüz sipariş yok</div>
      ) : (
        <div className="space-y-3">
          {orders.map((order: Order) => {
            const { label, color } = STATUS_LABELS[order.status] ?? { label: order.status, color: 'bg-gray-100 text-gray-600' }
            const isOverdue = order.dueDate && new Date(order.dueDate) < new Date()
            return (
              <div key={order.id} className="bg-white rounded-xl border border-gray-200 p-4">
                <div className="flex items-center justify-between mb-2">
                  <div>
                    <span className="text-xs text-gray-400">{new Date(order.createdAt).toLocaleDateString('tr-TR', { day: 'numeric', month: 'long' })}</span>
                    {order.dueDate && (
                      <span className={`text-xs ml-2 font-medium ${isOverdue ? 'text-red-600' : 'text-gray-500'}`}>
                        Vade: {new Date(order.dueDate).toLocaleDateString('tr-TR')} {isOverdue && '⚠️'}
                      </span>
                    )}
                  </div>
                  <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${color}`}>{label}</span>
                </div>
                <div className="space-y-1">
                  {order.items.map((item, i) => (
                    <div key={i} className="flex justify-between text-sm text-gray-600">
                      <span>
                        {item.productName}
                        <span className="text-xs text-gray-400 ml-1">({item.unitType})</span>
                        {' '}× {item.quantity}
                      </span>
                      <div className="text-right">
                        <span className="font-medium text-gray-800">₺{item.total.toFixed(2)}</span>
                        {item.vatRate > 0 && (
                          <span className="block text-xs text-purple-500">KDV %{item.vatRate} dahil</span>
                        )}
                      </div>
                    </div>
                  ))}
                </div>
                <div className="border-t border-gray-100 mt-2 pt-2 flex justify-between items-end">
                  <div>
                    {order.note && <p className="text-xs text-gray-400">Notunuz: {order.note}</p>}
                    {order.wholesalerNote && <p className="text-xs text-blue-600">Toptancı notu: {order.wholesalerNote}</p>}
                  </div>
                  <div className="flex items-center gap-3">
                    {order.status === 'Delivered' && (
                      <button
                        onClick={() => openPdfBlob(deliveryNotesApi.getPdfUrlByOrderForStore(order.id))}
                        className="px-3 py-1 text-xs bg-green-50 text-green-700 rounded-lg hover:bg-green-100 font-medium border border-green-200"
                      >
                        📄 İrsaliye PDF
                      </button>
                    )}
                    <span className="font-semibold text-gray-900">₺{order.totalAmount.toFixed(2)}</span>
                  </div>
                </div>
              </div>
            )
          })}
        </div>
      )}
    </Layout>
  )
}
