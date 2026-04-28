import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { productsApi } from '../api/client'
import { MovementTypeBadge } from './ui/StatusBadge'
import LoadingSkeleton from './ui/LoadingSkeleton'
import EmptyState from './ui/EmptyState'
import ErrorState from './ui/ErrorState'
import { formatDate, qtySign } from '../lib/utils'
import type { StockMovementPage } from '../types'

const MOVEMENT_TYPES = [
  'InitialBalance', 'OrderConfirm', 'OrderCancel', 'OrderReject',
  'ManualAdjustment', 'Return', 'ForceConfirmNegative'
]

interface Props {
  productId: string
  productName: string
  onClose: () => void
}

export default function StockHistoryModal({ productId, productName, onClose }: Props) {
  const [page, setPage] = useState(1)
  const [from, setFrom] = useState('')
  const [to, setTo] = useState('')
  const [selectedTypes, setSelectedTypes] = useState<string[]>([])

  const typesParam = selectedTypes.length > 0 ? selectedTypes.join(',') : undefined

  const { data, isLoading, isError, refetch } = useQuery<StockMovementPage>({
    queryKey: ['stock-movements', productId, page, from, to, typesParam],
    queryFn: () => productsApi.getStockMovements(productId, {
      from: from || undefined,
      to: to || undefined,
      types: typesParam,
      page,
      pageSize: 20,
    }),
  })

  const totalPages = data ? Math.ceil(data.total / data.pageSize) : 1

  const toggleType = (t: string) =>
    setSelectedTypes(prev => prev.includes(t) ? prev.filter(x => x !== t) : [...prev, t])

  const resetFilters = () => {
    setFrom(''); setTo(''); setSelectedTypes([]); setPage(1)
  }

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-2xl shadow-xl w-full max-w-3xl max-h-[90vh] flex flex-col">
        {/* Header */}
        <div className="px-6 py-4 border-b border-gray-100 flex items-center justify-between flex-shrink-0">
          <div>
            <h2 className="font-bold text-gray-900">Stok Hareketleri</h2>
            <p className="text-xs text-gray-500 mt-0.5">{productName}</p>
          </div>
          <button onClick={onClose} className="text-gray-400 hover:text-gray-700 text-xl leading-none">✕</button>
        </div>

        {/* Filters */}
        <div className="px-6 py-3 border-b border-gray-100 flex-shrink-0 space-y-2">
          <div className="flex gap-3 items-end">
            <div>
              <label className="block text-xs text-gray-500 mb-1">Başlangıç</label>
              <input type="date" value={from} onChange={e => { setFrom(e.target.value); setPage(1) }}
                className="px-2 py-1.5 border border-gray-300 rounded-lg text-xs focus:outline-none focus:ring-2 focus:ring-blue-500" />
            </div>
            <div>
              <label className="block text-xs text-gray-500 mb-1">Bitiş</label>
              <input type="date" value={to} onChange={e => { setTo(e.target.value); setPage(1) }}
                className="px-2 py-1.5 border border-gray-300 rounded-lg text-xs focus:outline-none focus:ring-2 focus:ring-blue-500" />
            </div>
            {(from || to || selectedTypes.length > 0) && (
              <button onClick={resetFilters}
                className="px-3 py-1.5 text-xs text-gray-500 hover:text-gray-700 border border-gray-300 rounded-lg">
                Temizle
              </button>
            )}
          </div>
          <div className="flex flex-wrap gap-1.5">
            {MOVEMENT_TYPES.map(t => (
              <button key={t}
                onClick={() => { toggleType(t); setPage(1) }}
                className={`px-2 py-0.5 rounded-full text-xs border transition-colors ${
                  selectedTypes.includes(t)
                    ? 'bg-blue-600 text-white border-blue-600'
                    : 'border-gray-300 text-gray-600 hover:border-blue-400'
                }`}>
                <MovementTypeBadge type={t} />
              </button>
            ))}
          </div>
        </div>

        {/* Table */}
        <div className="flex-1 overflow-y-auto px-6 py-3">
          {isLoading ? (
            <LoadingSkeleton rows={6} />
          ) : isError ? (
            <ErrorState onRetry={refetch} />
          ) : !data || data.items.length === 0 ? (
            <EmptyState icon="📊" title="Stok hareketi yok" description="Bu ürün için henüz kayıt oluşmamış" />
          ) : (
            <table className="w-full text-sm">
              <thead>
                <tr className="text-left text-xs text-gray-500 border-b border-gray-100">
                  <th className="pb-2 font-medium">Tarih</th>
                  <th className="pb-2 font-medium">Tip</th>
                  <th className="pb-2 font-medium text-right">Değişim</th>
                  <th className="pb-2 font-medium text-right">Bakiye</th>
                  <th className="pb-2 font-medium">Sipariş</th>
                  <th className="pb-2 font-medium">Açıklama</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-50">
                {data.items.map(m => (
                  <tr key={m.id} className="hover:bg-gray-50">
                    <td className="py-2 text-xs text-gray-500 whitespace-nowrap">{formatDate(m.createdAt)}</td>
                    <td className="py-2"><MovementTypeBadge type={m.movementType} /></td>
                    <td className={`py-2 text-right font-mono font-semibold text-sm ${
                      m.quantityChange >= 0 ? 'text-green-600' : 'text-red-600'
                    }`}>
                      {qtySign(m.quantityChange)}
                    </td>
                    <td className="py-2 text-right font-mono text-sm text-gray-700">{m.balanceAfter}</td>
                    <td className="py-2 text-xs">
                      {m.orderId
                        ? <span className="font-mono text-blue-600">{m.orderId.slice(0, 8).toUpperCase()}</span>
                        : <span className="text-gray-300">—</span>}
                    </td>
                    <td className="py-2 text-xs text-gray-500 max-w-32 truncate">{m.reason ?? '—'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>

        {/* Pagination */}
        {data && totalPages > 1 && (
          <div className="px-6 py-3 border-t border-gray-100 flex items-center justify-between flex-shrink-0">
            <span className="text-xs text-gray-500">
              Toplam {data.total} kayıt · Sayfa {page}/{totalPages}
            </span>
            <div className="flex gap-2">
              <button disabled={page <= 1} onClick={() => setPage(p => p - 1)}
                className="px-3 py-1 text-xs border border-gray-300 rounded-lg disabled:opacity-40 hover:bg-gray-50">
                ← Önceki
              </button>
              <button disabled={page >= totalPages} onClick={() => setPage(p => p + 1)}
                className="px-3 py-1 text-xs border border-gray-300 rounded-lg disabled:opacity-40 hover:bg-gray-50">
                Sonraki →
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  )
}
