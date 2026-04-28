import { useQuery } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import Layout from '../../components/Layout'
import { MovementTypeBadge } from '../../components/ui/StatusBadge'
import { ordersApi, creditApi, wholesalersApi, productsApi } from '../../api/client'
import { formatDateShort, formatCurrency, qtySign } from '../../lib/utils'
import type { Order, StockMovement, Product } from '../../types'

const NAV = [
  { to: '/wholesaler', label: 'Ana Sayfa' },
  { to: '/wholesaler/products', label: 'Ürünler' },
  { to: '/wholesaler/orders', label: 'Siparişler' },
  { to: '/wholesaler/credit', label: 'Veresiye' },
  { to: '/wholesaler/settings', label: 'Ayarlar' },
  { to: '/notifications', label: 'Bildirimler' },
]

export default function WholesalerHome() {
  const navigate = useNavigate()

  const { data: orders = [] } = useQuery<Order[]>({
    queryKey: ['incoming-orders'],
    queryFn: ordersApi.incoming,
    refetchInterval: 30_000,
  })

  const { data: creditStores = [] } = useQuery<{ storeId: string; storeName: string; balance: number; overdueAmount: number }[]>({
    queryKey: ['credit-all-stores'],
    queryFn: creditApi.getAllStores,
  })

  const { data: recentMovements = [] } = useQuery<StockMovement[]>({
    queryKey: ['wholesaler-stock-movements-recent'],
    queryFn: () => wholesalersApi.getRecentStockMovements(10),
  })

  const { data: lowStockProducts = [] } = useQuery<Product[]>({
    queryKey: ['my-products-lowstock'],
    queryFn: () => productsApi.getAll({ includeInactive: false, lowStock: true }),
  })

  const pending = orders.filter(o => o.status === 'Pending')
  const confirmedNotDelivered = orders.filter(o => o.status === 'Confirmed')
  const totalBalance = creditStores.reduce((s, c) => s + c.balance, 0)
  const totalOverdue = creditStores.reduce((s, c) => s + c.overdueAmount, 0)

  const features = [
    {
      icon: '📦', title: 'Ürünler', description: 'Ürün kataloğunu yönet',
      to: '/wholesaler/products', color: 'border-blue-200 hover:border-blue-400', iconBg: 'bg-blue-50',
    },
    {
      icon: '📋', title: 'Siparişler', description: 'Gelen siparişleri yönet',
      to: '/wholesaler/orders', color: 'border-orange-200 hover:border-orange-400', iconBg: 'bg-orange-50',
      badge: pending.length > 0 ? pending.length : undefined,
    },
    {
      icon: '💰', title: 'Veresiye Defteri', description: 'Mağaza bakiyelerini takip et',
      to: '/wholesaler/credit', color: 'border-green-200 hover:border-green-400', iconBg: 'bg-green-50',
      badge: totalOverdue > 0 ? '!' : undefined,
    },
    {
      icon: '⚙️', title: 'Ayarlar', description: 'Profil ve tercihler',
      to: '/wholesaler/settings', color: 'border-gray-200 hover:border-gray-400', iconBg: 'bg-gray-50',
    },
  ]

  return (
    <Layout navLinks={NAV}>
      {/* Özet kartlar */}
      <div className="grid grid-cols-4 gap-4 mb-8">
        <div className="bg-white rounded-xl border border-gray-200 p-4">
          <p className="text-xs text-gray-500 mb-1">Bekleyen Sipariş</p>
          <p className="text-2xl font-bold text-orange-500">{pending.length}</p>
        </div>
        <div className="bg-white rounded-xl border border-gray-200 p-4">
          <p className="text-xs text-gray-500 mb-1">Teslim Bekleyen</p>
          <p className="text-2xl font-bold text-blue-600">{confirmedNotDelivered.length}</p>
        </div>
        <div className="bg-white rounded-xl border border-gray-200 p-4">
          <p className="text-xs text-gray-500 mb-1">Toplam Alacak</p>
          <p className="text-2xl font-bold text-green-600">{formatCurrency(totalBalance)}</p>
        </div>
        <div className="bg-white rounded-xl border border-gray-200 p-4">
          <p className="text-xs text-gray-500 mb-1">Vadesi Geçmiş</p>
          <p className={`text-2xl font-bold ${totalOverdue > 0 ? 'text-red-600' : 'text-gray-400'}`}>
            {formatCurrency(totalOverdue)}
          </p>
        </div>
      </div>

      {/* Feature kartlar */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-8">
        {features.map(f => (
          <button key={f.to} onClick={() => navigate(f.to)}
            className={`bg-white rounded-xl border-2 p-6 text-left transition-all hover:shadow-md relative ${f.color}`}
          >
            {f.badge !== undefined && (
              <span className="absolute top-3 right-3 min-w-5 h-5 bg-red-500 text-white text-xs font-bold rounded-full flex items-center justify-center px-1">
                {f.badge}
              </span>
            )}
            <div className={`w-12 h-12 rounded-xl ${f.iconBg} flex items-center justify-center text-2xl mb-3`}>
              {f.icon}
            </div>
            <p className="font-semibold text-gray-900 mb-1">{f.title}</p>
            <p className="text-xs text-gray-500">{f.description}</p>
          </button>
        ))}
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mb-4">
        {/* Düşük stok uyarıları */}
        {lowStockProducts.length > 0 && (
          <div className="bg-white rounded-xl border border-red-200 overflow-hidden">
            <div className="px-4 py-3 border-b border-red-100 bg-red-50 flex items-center justify-between">
              <h2 className="font-semibold text-red-800 text-sm">⚠️ Düşük Stok Ürünleri</h2>
              <button onClick={() => navigate('/wholesaler/products')}
                className="text-xs text-red-600 hover:text-red-800">
                Ürünlere git →
              </button>
            </div>
            <div className="divide-y divide-gray-50">
              {lowStockProducts.slice(0, 5).map(p => (
                <div key={p.id} className="px-4 py-2.5 flex items-center justify-between">
                  <span className="text-sm text-gray-900 font-medium truncate flex-1">{p.name}</span>
                  <div className="flex items-center gap-3 flex-shrink-0 ml-2">
                    <span className="text-xs text-red-600 font-semibold">Stok: {p.stock}</span>
                    <span className="text-xs text-gray-400">Min: {p.minimumStockLevel}</span>
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}

        {/* Son stok hareketleri */}
        {recentMovements.length > 0 && (
          <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
            <div className="px-4 py-3 border-b border-gray-100 bg-gray-50 flex items-center justify-between">
              <h2 className="font-semibold text-gray-800 text-sm">📊 Son Stok Hareketleri</h2>
              <button onClick={() => navigate('/wholesaler/stock-movements')}
                className="text-xs text-blue-600 hover:text-blue-800">
                Tümünü gör →
              </button>
            </div>
            <div className="divide-y divide-gray-50">
              {recentMovements.slice(0, 8).map(m => (
                <div key={m.id} className="px-4 py-2.5 flex items-center gap-3">
                  <div className="flex-1 min-w-0">
                    <p className="text-xs font-medium text-gray-800 truncate">{m.productName}</p>
                    <p className="text-xs text-gray-400">{formatDateShort(m.createdAt)}</p>
                  </div>
                  <MovementTypeBadge type={m.movementType} />
                  <span className={`text-sm font-mono font-semibold w-12 text-right ${
                    m.quantityChange >= 0 ? 'text-green-600' : 'text-red-600'
                  }`}>
                    {qtySign(m.quantityChange)}
                  </span>
                </div>
              ))}
            </div>
          </div>
        )}
      </div>

      {/* Bekleyen siparişler hızlı liste */}
      {pending.length > 0 && (
        <div className="bg-white rounded-xl border border-orange-200 overflow-hidden">
          <div className="px-4 py-3 border-b border-orange-100 bg-orange-50 flex items-center justify-between">
            <h2 className="font-semibold text-orange-800 text-sm">⚡ Bekleyen Siparişler</h2>
            <button onClick={() => navigate('/wholesaler/orders')} className="text-xs text-orange-600 hover:text-orange-800">
              Tümünü Gör →
            </button>
          </div>
          <div className="divide-y divide-gray-50">
            {pending.slice(0, 5).map(o => (
              <div key={o.id} className="px-4 py-3 flex items-center justify-between hover:bg-gray-50">
                <div>
                  <span className="font-medium text-sm text-gray-900">{o.storeName}</span>
                  <span className="text-xs text-gray-400 ml-2">{new Date(o.createdAt).toLocaleDateString('tr-TR')}</span>
                </div>
                <span className="font-semibold text-gray-900 text-sm">{formatCurrency(o.totalAmount)}</span>
              </div>
            ))}
          </div>
        </div>
      )}
    </Layout>
  )
}
