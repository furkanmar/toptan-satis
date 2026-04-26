import { useQuery } from '@tanstack/react-query'
import Layout from '../../components/Layout'
import { ordersApi, wholesalersApi } from '../../api/client'
import type { Order, Wholesaler } from '../../types'

const NAV = [
  { to: '/admin', label: 'Dashboard' },
  { to: '/admin/users', label: 'Kullanıcılar' },
  { to: '/admin/store-wholesalers', label: 'Mağaza-Toptancı' },
  { to: '/admin/categories', label: 'Kategoriler' },
]

export default function AdminDashboard() {
  const { data: orders = [] } = useQuery<Order[]>({
    queryKey: ['all-orders'],
    queryFn: ordersApi.getAll
  })

  const { data: wholesalers = [] } = useQuery<Wholesaler[]>({
    queryKey: ['wholesalers'],
    queryFn: wholesalersApi.getAll
  })

  const total = orders.reduce((s, o) => s + o.totalAmount, 0)
  const pending = orders.filter(o => o.status === 'Pending').length
  const confirmed = orders.filter(o => o.status === 'Confirmed').length
  const delivered = orders.filter(o => o.status === 'Delivered').length

  return (
    <Layout navLinks={NAV}>
      <h1 className="text-xl font-bold text-gray-900 mb-4">Admin Paneli</h1>

      {/* Stats */}
      <div className="grid grid-cols-4 gap-4 mb-6">
        {[
          { label: 'Toplam Sipariş', value: orders.length, color: 'text-gray-900' },
          { label: 'Bekleyen', value: pending, color: 'text-yellow-600' },
          { label: 'Onaylanan', value: confirmed, color: 'text-blue-600' },
          { label: 'Teslim', value: delivered, color: 'text-green-600' },
        ].map(s => (
          <div key={s.label} className="bg-white rounded-xl border border-gray-200 p-4">
            <p className="text-xs text-gray-500 mb-1">{s.label}</p>
            <p className={`text-2xl font-bold ${s.color}`}>{s.value}</p>
          </div>
        ))}
      </div>

      <div className="grid grid-cols-3 gap-4 mb-4">
        <div className="bg-white rounded-xl border border-gray-200 p-4 col-span-2">
          <p className="text-xs text-gray-500 mb-1">Toplam Ciro (Tüm Zamanlar)</p>
          <p className="text-3xl font-bold text-gray-900">₺{total.toLocaleString('tr-TR', { minimumFractionDigits: 2 })}</p>
        </div>
        <div className="bg-white rounded-xl border border-gray-200 p-4">
          <p className="text-xs text-gray-500 mb-1">Aktif Toptancı</p>
          <p className="text-3xl font-bold text-purple-600">{wholesalers.length}</p>
        </div>
      </div>

      {/* Son siparişler */}
      <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
        <div className="px-4 py-3 border-b border-gray-100">
          <h2 className="font-semibold text-gray-900 text-sm">Son Siparişler</h2>
        </div>
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-gray-100 bg-gray-50">
              <th className="text-left px-4 py-2 text-xs font-medium text-gray-500">Mağaza</th>
              <th className="text-left px-4 py-2 text-xs font-medium text-gray-500">Toptancı</th>
              <th className="text-left px-4 py-2 text-xs font-medium text-gray-500">Tarih</th>
              <th className="text-right px-4 py-2 text-xs font-medium text-gray-500">Tutar</th>
              <th className="text-center px-4 py-2 text-xs font-medium text-gray-500">Durum</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-50">
            {orders.slice(0, 20).map(o => (
              <tr key={o.id} className="hover:bg-gray-50">
                <td className="px-4 py-2.5 text-gray-900">{o.storeName}</td>
                <td className="px-4 py-2.5 text-gray-600">{o.wholesalerName}</td>
                <td className="px-4 py-2.5 text-gray-500 text-xs">{new Date(o.createdAt).toLocaleDateString('tr-TR')}</td>
                <td className="px-4 py-2.5 text-right font-medium text-gray-900">₺{o.totalAmount.toFixed(2)}</td>
                <td className="px-4 py-2.5 text-center">
                  <span className="text-xs px-2 py-0.5 rounded-full bg-gray-100 text-gray-600">{o.status}</span>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
        {orders.length === 0 && <div className="text-center py-12 text-gray-400">Sipariş yok</div>}
      </div>
    </Layout>
  )
}
