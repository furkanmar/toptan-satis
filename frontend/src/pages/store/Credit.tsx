import { useParams } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import Layout from '../../components/Layout'
import { creditApi } from '../../api/client'
import type { CreditSummary } from '../../types'

const NAV = (wholesalerId: string) => [
  { to: `/store/${wholesalerId}`, label: 'Ürünler' },
  { to: `/store/${wholesalerId}/orders`, label: 'Siparişlerim' },
  { to: `/store/${wholesalerId}/credit`, label: 'Veresiye' },
  { to: '/store', label: '← Toptancı Seç' }
]

const TYPE_LABELS: Record<string, { label: string; color: string; sign: string }> = {
  OrderDebit:  { label: 'Sipariş', color: 'text-red-600',   sign: '+' },
  ManualDebit: { label: 'Borç',    color: 'text-red-600',   sign: '+' },
  Payment:     { label: 'Ödeme',   color: 'text-green-600', sign: '-' }
}

export default function StoreCredit() {
  const { wholesalerId } = useParams<{ wholesalerId: string }>()

  const { data: credit, isLoading } = useQuery<CreditSummary>({
    queryKey: ['credit', wholesalerId],
    queryFn: () => creditApi.getWholesalerCredit(wholesalerId!),
    enabled: !!wholesalerId
  })

  const nav = NAV(wholesalerId ?? '')

  return (
    <Layout navLinks={nav}>
      <h1 className="text-xl font-bold text-gray-900 mb-4">Veresiye Durumum</h1>

      {isLoading ? (
        <div className="text-center py-12 text-gray-400">Yükleniyor...</div>
      ) : !credit ? null : (
        <>
          {/* Özet */}
          <div className="grid grid-cols-3 gap-4 mb-6">
            <div className="bg-white rounded-xl border border-gray-200 p-4">
              <p className="text-xs text-gray-500 mb-1">Toplam Borç</p>
              <p className="text-xl font-bold text-red-600">₺{credit.totalDebt.toFixed(2)}</p>
            </div>
            <div className="bg-white rounded-xl border border-gray-200 p-4">
              <p className="text-xs text-gray-500 mb-1">Toplam Ödeme</p>
              <p className="text-xl font-bold text-green-600">₺{credit.totalPaid.toFixed(2)}</p>
            </div>
            <div className={`rounded-xl border p-4 ${credit.balance > 0 ? 'bg-red-50 border-red-200' : 'bg-green-50 border-green-200'}`}>
              <p className="text-xs text-gray-500 mb-1">Kalan Bakiye</p>
              <p className={`text-xl font-bold ${credit.balance > 0 ? 'text-red-600' : 'text-green-600'}`}>
                ₺{credit.balance.toFixed(2)}
              </p>
              {credit.overdueAmount > 0 && (
                <p className="text-xs text-red-500 mt-1">⚠️ Vadesi geçmiş: ₺{credit.overdueAmount.toFixed(2)}</p>
              )}
            </div>
          </div>

          {/* İşlem geçmişi */}
          <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
            <div className="px-4 py-3 border-b border-gray-100">
              <h2 className="font-semibold text-gray-900 text-sm">İşlem Geçmişi</h2>
            </div>
            {credit.transactions.length === 0 ? (
              <div className="text-center py-8 text-gray-400 text-sm">İşlem yok</div>
            ) : (
              <table className="w-full text-sm">
                <thead>
                  <tr className="bg-gray-50 border-b border-gray-100">
                    <th className="text-left px-4 py-2 text-xs font-medium text-gray-500">Tarih</th>
                    <th className="text-left px-4 py-2 text-xs font-medium text-gray-500">Açıklama</th>
                    <th className="text-left px-4 py-2 text-xs font-medium text-gray-500">Vade</th>
                    <th className="text-right px-4 py-2 text-xs font-medium text-gray-500">Tutar</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-50">
                  {credit.transactions.map(tx => {
                    const { label, color, sign } = TYPE_LABELS[tx.type] ?? { label: tx.type, color: 'text-gray-600', sign: '' }
                    const isOverdue = tx.dueDate && new Date(tx.dueDate) < new Date() && tx.type !== 'Payment'
                    return (
                      <tr key={tx.id} className="hover:bg-gray-50">
                        <td className="px-4 py-2.5 text-gray-400 text-xs">{new Date(tx.createdAt).toLocaleDateString('tr-TR')}</td>
                        <td className="px-4 py-2.5">
                          <span className="text-gray-900">{tx.description}</span>
                          <span className={`text-xs ml-2 ${color}`}>{label}</span>
                        </td>
                        <td className="px-4 py-2.5 text-xs">
                          {tx.dueDate
                            ? <span className={isOverdue ? 'text-red-600 font-medium' : 'text-gray-500'}>
                                {new Date(tx.dueDate).toLocaleDateString('tr-TR')} {isOverdue && '⚠️'}
                              </span>
                            : <span className="text-gray-300">—</span>
                          }
                        </td>
                        <td className={`px-4 py-2.5 text-right font-semibold ${color}`}>{sign}₺{tx.amount.toFixed(2)}</td>
                      </tr>
                    )
                  })}
                </tbody>
              </table>
            )}
          </div>
        </>
      )}
    </Layout>
  )
}
