import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import Layout from '../../components/Layout'
import { MovementTypeBadge } from '../../components/ui/StatusBadge'
import LoadingSkeleton from '../../components/ui/LoadingSkeleton'
import EmptyState from '../../components/ui/EmptyState'
import ErrorState from '../../components/ui/ErrorState'
import { wholesalersApi } from '../../api/client'
import { formatDate, qtySign } from '../../lib/utils'
import type { StockMovementPage } from '../../types'

const NAV = [
  { to: '/wholesaler', label: 'Ana Sayfa' },
  { to: '/wholesaler/products', label: 'Ürünler' },
  { to: '/wholesaler/orders', label: 'Siparişler' },
  { to: '/wholesaler/credit', label: 'Veresiye' },
  { to: '/wholesaler/settings', label: 'Ayarlar' },
]

export default function WholesalerStockMovements() {
  const navigate = useNavigate()
  const [page, setPage] = useState(1)

  const { data, isLoading, isError, refetch } = useQuery<StockMovementPage>({
    queryKey: ['wholesaler-stock-movements', page],
    queryFn: () => wholesalersApi.getStockMovements({ page, pageSize: 30 }),
  })

  const totalPages = data ? Math.ceil(data.total / data.pageSize) : 1

  return (
    <Layout navLinks={NAV}>
      <div className="flex items-center justify-between mb-4">
        <h1 className="text-xl font-bold text-gray-900">Tüm Stok Hareketleri</h1>
        {data && <span className="text-xs text-gray-500">Toplam {data.total} kayıt</span>}
      </div>

      {isLoading ? (
        <LoadingSkeleton rows={8} />
      ) : isError ? (
        <ErrorState onRetry={refetch} />
      ) : !data || data.items.length === 0 ? (
        <EmptyState icon="📊" title="Stok hareketi yok" />
      ) : (
        <>
          <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
            <table className="w-full text-sm">
              <thead>
                <tr className="bg-gray-50 text-left text-xs text-gray-500 border-b border-gray-200">
                  <th className="px-4 py-3 font-medium">Tarih</th>
                  <th className="px-4 py-3 font-medium">Ürün</th>
                  <th className="px-4 py-3 font-medium">Tip</th>
                  <th className="px-4 py-3 font-medium text-right">Değişim</th>
                  <th className="px-4 py-3 font-medium text-right">Bakiye</th>
                  <th className="px-4 py-3 font-medium">Sipariş</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-50">
                {data.items.map(m => (
                  <tr key={m.id} className="hover:bg-gray-50 cursor-pointer"
                    onClick={() => m.orderId && navigate(`/wholesaler/orders`)}>
                    <td className="px-4 py-3 text-xs text-gray-500 whitespace-nowrap">{formatDate(m.createdAt)}</td>
                    <td className="px-4 py-3 text-sm font-medium text-gray-800 max-w-40 truncate">{m.productName}</td>
                    <td className="px-4 py-3"><MovementTypeBadge type={m.movementType} /></td>
                    <td className={`px-4 py-3 text-right font-mono font-semibold ${
                      m.quantityChange >= 0 ? 'text-green-600' : 'text-red-600'
                    }`}>{qtySign(m.quantityChange)}</td>
                    <td className="px-4 py-3 text-right font-mono text-gray-700">{m.balanceAfter}</td>
                    <td className="px-4 py-3 text-xs font-mono text-blue-600">
                      {m.orderId ? m.orderId.slice(0, 8).toUpperCase() : '—'}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {totalPages > 1 && (
            <div className="flex items-center justify-between mt-4">
              <span className="text-xs text-gray-500">Sayfa {page}/{totalPages}</span>
              <div className="flex gap-2">
                <button disabled={page <= 1} onClick={() => setPage(p => p - 1)}
                  className="px-3 py-1.5 text-xs border border-gray-300 rounded-lg disabled:opacity-40 hover:bg-gray-50">
                  ← Önceki
                </button>
                <button disabled={page >= totalPages} onClick={() => setPage(p => p + 1)}
                  className="px-3 py-1.5 text-xs border border-gray-300 rounded-lg disabled:opacity-40 hover:bg-gray-50">
                  Sonraki →
                </button>
              </div>
            </div>
          )}
        </>
      )}
    </Layout>
  )
}
