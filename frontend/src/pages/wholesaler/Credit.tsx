import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import Layout from '../../components/Layout'
import { creditApi } from '../../api/client'
import type { CreditSummary } from '../../types'

const NAV = [
  { to: '/wholesaler', label: 'Ana Sayfa' },
  { to: '/wholesaler/products', label: 'Ürünler' },
  { to: '/wholesaler/orders', label: 'Siparişler' },
  { to: '/wholesaler/credit', label: 'Veresiye' },
  { to: '/wholesaler/settings', label: 'Ayarlar' }
]

const TYPE_LABELS: Record<string, { label: string; color: string; sign: string }> = {
  OrderDebit:   { label: 'Sipariş',    color: 'text-red-600',   sign: '+' },
  ManualDebit:  { label: 'Borç',       color: 'text-red-600',   sign: '+' },
  Payment:      { label: 'Ödeme',      color: 'text-green-600', sign: '-' }
}

export default function WholesalerCredit() {
  const qc = useQueryClient()
  const [selectedStoreId, setSelectedStoreId] = useState<string | null>(null)
  const [showForm, setShowForm] = useState(false)
  const [form, setForm] = useState({ type: 'ManualDebit', amount: '', description: '', dueDate: '' })

  const { data: stores = [] } = useQuery<{ storeId: string; storeName: string; balance: number; overdueAmount: number }[]>({
    queryKey: ['credit-all-stores'],
    queryFn: creditApi.getAllStores
  })

  const { data: detail } = useQuery<CreditSummary>({
    queryKey: ['credit-store', selectedStoreId],
    queryFn: () => creditApi.getStoreCredit(selectedStoreId!),
    enabled: !!selectedStoreId
  })

  const addMutation = useMutation({
    mutationFn: () => creditApi.addTransaction({
      storeId: selectedStoreId,
      type: form.type,
      amount: parseFloat(form.amount),
      description: form.description,
      dueDate: form.dueDate || undefined
    }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['credit-store', selectedStoreId] })
      qc.invalidateQueries({ queryKey: ['credit-all-stores'] })
      setShowForm(false)
      setForm({ type: 'ManualDebit', amount: '', description: '', dueDate: '' })
    }
  })

  return (
    <Layout navLinks={NAV}>
      <h1 className="text-xl font-bold text-gray-900 mb-4">Veresiye Defteri</h1>

      <div className="grid grid-cols-3 gap-4">
        {/* Sol — mağaza listesi */}
        <div className="space-y-2">
          <h2 className="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-3">Mağazalar</h2>
          {stores.length === 0 && <p className="text-sm text-gray-400">Mağaza yok</p>}
          {stores.map(s => (
            <button
              key={s.storeId}
              onClick={() => setSelectedStoreId(s.storeId)}
              className={`w-full text-left p-3 rounded-xl border transition-all ${selectedStoreId === s.storeId ? 'border-blue-400 bg-blue-50' : 'border-gray-200 bg-white hover:border-gray-300'}`}
            >
              <p className="font-medium text-sm text-gray-900">{s.storeName}</p>
              <div className="flex justify-between mt-1">
                <span className={`text-xs font-semibold ${s.balance > 0 ? 'text-red-600' : 'text-green-600'}`}>
                  ₺{s.balance.toFixed(2)}
                </span>
                {s.overdueAmount > 0 && (
                  <span className="text-xs text-red-500">⚠️ ₺{s.overdueAmount.toFixed(2)}</span>
                )}
              </div>
            </button>
          ))}
        </div>

        {/* Sağ — detay */}
        <div className="col-span-2">
          {!selectedStoreId ? (
            <div className="flex items-center justify-center h-64 text-gray-400 text-sm">
              Soldaki listeden bir mağaza seçin
            </div>
          ) : !detail ? (
            <div className="flex items-center justify-center h-64 text-gray-400 text-sm">Yükleniyor...</div>
          ) : (
            <>
              {/* Özet */}
              <div className="grid grid-cols-3 gap-3 mb-4">
                <div className="bg-white rounded-xl border border-gray-200 p-3">
                  <p className="text-xs text-gray-500">Toplam Borç</p>
                  <p className="text-lg font-bold text-red-600">₺{detail.totalDebt.toFixed(2)}</p>
                </div>
                <div className="bg-white rounded-xl border border-gray-200 p-3">
                  <p className="text-xs text-gray-500">Toplam Ödeme</p>
                  <p className="text-lg font-bold text-green-600">₺{detail.totalPaid.toFixed(2)}</p>
                </div>
                <div className={`rounded-xl border p-3 ${detail.balance > 0 ? 'bg-red-50 border-red-200' : 'bg-green-50 border-green-200'}`}>
                  <p className="text-xs text-gray-500">Bakiye</p>
                  <p className={`text-lg font-bold ${detail.balance > 0 ? 'text-red-600' : 'text-green-600'}`}>
                    ₺{detail.balance.toFixed(2)}
                  </p>
                </div>
              </div>

              {/* İşlem ekle */}
              <div className="flex items-center justify-between mb-3">
                <h2 className="font-semibold text-gray-900 text-sm">İşlemler</h2>
                <button
                  onClick={() => setShowForm(!showForm)}
                  className="px-3 py-1.5 bg-blue-600 text-white rounded-lg text-xs font-medium hover:bg-blue-700 transition-colors"
                >
                  + İşlem Ekle
                </button>
              </div>

              {showForm && (
                <div className="bg-white rounded-xl border border-gray-200 p-4 mb-3">
                  <div className="grid grid-cols-2 gap-3">
                    <div>
                      <label className="block text-xs font-medium text-gray-700 mb-1">Tür</label>
                      <select
                        value={form.type}
                        onChange={e => setForm(f => ({ ...f, type: e.target.value, dueDate: '' }))}
                        className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                      >
                        <option value="ManualDebit">Borç</option>
                        <option value="Payment">Ödeme</option>
                      </select>
                    </div>
                    <div>
                      <label className="block text-xs font-medium text-gray-700 mb-1">Tutar (₺)</label>
                      <input
                        type="number"
                        value={form.amount}
                        onChange={e => setForm(f => ({ ...f, amount: e.target.value }))}
                        className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                      />
                    </div>
                    <div className="col-span-2">
                      <label className="block text-xs font-medium text-gray-700 mb-1">Açıklama</label>
                      <input
                        value={form.description}
                        onChange={e => setForm(f => ({ ...f, description: e.target.value }))}
                        className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                      />
                    </div>
                    {form.type === 'ManualDebit' && (
                      <div className="col-span-2">
                        <label className="block text-xs font-medium text-gray-700 mb-1">Vade Tarihi</label>
                        <div className="flex flex-wrap gap-1 mb-2">
                          {[
                            { label: '1 Hafta', days: 7 },
                            { label: '2 Hafta', days: 14 },
                            { label: '3 Hafta', days: 21 },
                            { label: '4 Hafta', days: 28 },
                            { label: '1 Ay',    days: 30 },
                            { label: '2 Ay',    days: 60 },
                          ].map(({ label, days }) => {
                            const d = new Date()
                            d.setDate(d.getDate() + days)
                            const val = d.toISOString().split('T')[0]
                            return (
                              <button
                                key={label}
                                type="button"
                                onClick={() => setForm(f => ({ ...f, dueDate: val }))}
                                className={`px-2 py-1 rounded text-xs border transition-colors ${form.dueDate === val ? 'bg-blue-600 text-white border-blue-600' : 'border-gray-300 text-gray-600 hover:bg-gray-50'}`}
                              >
                                {label}
                              </button>
                            )
                          })}
                        </div>
                        <input
                          type="date"
                          value={form.dueDate}
                          onChange={e => setForm(f => ({ ...f, dueDate: e.target.value }))}
                          className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                        />
                      </div>
                    )}
                  </div>
                  <div className="flex gap-2 mt-3">
                    <button
                      onClick={() => addMutation.mutate()}
                      disabled={addMutation.isPending || !form.amount || !form.description}
                      className="px-4 py-2 bg-blue-600 text-white rounded-lg text-sm font-medium hover:bg-blue-700 disabled:opacity-50 transition-colors"
                    >
                      Kaydet
                    </button>
                    <button onClick={() => setShowForm(false)} className="px-4 py-2 border border-gray-300 text-gray-700 rounded-lg text-sm hover:bg-gray-50 transition-colors">
                      İptal
                    </button>
                  </div>
                </div>
              )}

              {/* İşlem listesi */}
              <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
                {detail.transactions.length === 0 ? (
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
                      {detail.transactions.map(tx => {
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
                            <td className={`px-4 py-2.5 text-right font-semibold ${color}`}>
                              {sign}₺{tx.amount.toFixed(2)}
                            </td>
                          </tr>
                        )
                      })}
                    </tbody>
                  </table>
                )}
              </div>
            </>
          )}
        </div>
      </div>
    </Layout>
  )
}
