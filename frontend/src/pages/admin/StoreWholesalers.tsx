import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import Layout from '../../components/Layout'
import { adminApi, wholesalersApi } from '../../api/client'
import type { StoreWholesalerRelation, Wholesaler } from '../../types'

const NAV = [
  { to: '/admin', label: 'Dashboard' },
  { to: '/admin/users', label: 'Kullanıcılar' },
  { to: '/admin/store-wholesalers', label: 'Mağaza-Toptancı' },
  { to: '/admin/categories', label: 'Kategoriler' },
  { to: '/admin/audit-logs', label: 'Audit Log' },
]

export default function AdminStoreWholesalers() {
  const qc = useQueryClient()
  const [search, setSearch] = useState('')
  const [confirmDelete, setConfirmDelete] = useState<{ storeId: string; wholesalerId: string; label: string } | null>(null)

  // New relation form
  const [showAdd, setShowAdd] = useState(false)
  const [newStoreId, setNewStoreId] = useState('')
  const [newWholesalerId, setNewWholesalerId] = useState('')

  const { data: relations = [], isLoading } = useQuery<StoreWholesalerRelation[]>({
    queryKey: ['admin-store-wholesalers'],
    queryFn: adminApi.getStoreWholesalers,
  })

  const { data: wholesalers = [] } = useQuery<Wholesaler[]>({
    queryKey: ['wholesalers'],
    queryFn: wholesalersApi.getAll,
  })

  const deleteRelation = useMutation({
    mutationFn: ({ storeId, wholesalerId }: { storeId: string; wholesalerId: string }) =>
      adminApi.deleteStoreWholesaler(storeId, wholesalerId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['admin-store-wholesalers'] })
      setConfirmDelete(null)
    }
  })

  const createRelation = useMutation({
    mutationFn: () => adminApi.createStoreWholesaler(newStoreId.trim(), newWholesalerId.trim()),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['admin-store-wholesalers'] })
      setShowAdd(false)
      setNewStoreId('')
      setNewWholesalerId('')
    }
  })

  const filtered = relations.filter(r => {
    if (!search) return true
    const q = search.toLowerCase()
    return r.storeName.toLowerCase().includes(q) ||
      r.wholesalerName.toLowerCase().includes(q) ||
      r.storePhone?.toLowerCase().includes(q)
  })

  // Group by wholesaler for summary counts
  const wholesalerCounts = relations.reduce<Record<string, number>>((acc, r) => {
    acc[r.wholesalerName] = (acc[r.wholesalerName] ?? 0) + 1
    return acc
  }, {})

  return (
    <Layout navLinks={NAV}>
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-xl font-bold text-gray-900">Mağaza-Toptancı İlişkileri</h1>
          <p className="text-sm text-gray-500 mt-0.5">{relations.length} ilişki toplam</p>
        </div>
        <button
          onClick={() => setShowAdd(s => !s)}
          className="px-4 py-2 bg-blue-600 text-white rounded-lg text-sm font-medium hover:bg-blue-700 transition-colors"
        >
          + İlişki Ekle
        </button>
      </div>

      {/* Özet kartlar */}
      {wholesalers.length > 0 && (
        <div className="flex gap-3 overflow-x-auto pb-2 mb-5 scrollbar-hide">
          {wholesalers.map(w => (
            <div key={w.id} className="shrink-0 bg-white border border-gray-200 rounded-xl px-4 py-3 min-w-[140px]">
              <p className="text-xs text-gray-500 truncate">{w.companyName}</p>
              <p className="text-2xl font-bold text-gray-900 mt-0.5">{wholesalerCounts[w.companyName] ?? 0}</p>
              <p className="text-xs text-gray-400">mağaza</p>
            </div>
          ))}
        </div>
      )}

      {/* Yeni ilişki formu */}
      {showAdd && (
        <div className="bg-blue-50 border border-blue-200 rounded-xl p-4 mb-5">
          <h3 className="text-sm font-semibold text-blue-800 mb-3">Yeni İlişki</h3>
          <div className="grid grid-cols-2 gap-3 mb-3">
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">Mağaza ID</label>
              <input
                value={newStoreId}
                onChange={e => setNewStoreId(e.target.value)}
                placeholder="UUID…"
                className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 bg-white font-mono"
              />
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">Toptancı</label>
              <select
                value={newWholesalerId}
                onChange={e => setNewWholesalerId(e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 bg-white"
              >
                <option value="">Seçin…</option>
                {wholesalers.map(w => <option key={w.id} value={w.id}>{w.companyName}</option>)}
              </select>
            </div>
          </div>
          <div className="flex gap-2">
            <button
              onClick={() => createRelation.mutate()}
              disabled={!newStoreId.trim() || !newWholesalerId || createRelation.isPending}
              className="px-4 py-2 bg-blue-600 text-white rounded-lg text-sm font-medium hover:bg-blue-700 disabled:opacity-50 transition-colors"
            >
              {createRelation.isPending ? 'Ekleniyor…' : 'Ekle'}
            </button>
            <button onClick={() => setShowAdd(false)}
              className="px-4 py-2 border border-gray-300 text-gray-700 rounded-lg text-sm hover:bg-gray-50">
              İptal
            </button>
          </div>
          {createRelation.isError && (
            <p className="text-xs text-red-600 mt-2">Hata oluştu — ID'leri kontrol edin.</p>
          )}
        </div>
      )}

      {/* Arama */}
      <div className="relative mb-4">
        <span className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400 text-sm">🔍</span>
        <input
          value={search}
          onChange={e => setSearch(e.target.value)}
          placeholder="Mağaza adı, toptancı veya telefon ara…"
          className="w-full pl-8 pr-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
        />
      </div>

      {/* Tablo */}
      {isLoading ? (
        <div className="text-center py-16 text-gray-400">Yükleniyor…</div>
      ) : (
        <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
          {filtered.length === 0 ? (
            <div className="text-center py-16 text-gray-400">
              {search ? 'Arama sonucu bulunamadı' : 'Henüz ilişki yok'}
            </div>
          ) : (
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-gray-100 bg-gray-50">
                  <th className="text-left px-4 py-3 text-xs font-medium text-gray-500">Mağaza</th>
                  <th className="text-left px-4 py-3 text-xs font-medium text-gray-500">Toptancı</th>
                  <th className="text-left px-4 py-3 text-xs font-medium text-gray-500 hidden sm:table-cell">Atanma tarihi</th>
                  <th className="px-4 py-3"></th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-50">
                {filtered.map(r => (
                  <tr key={`${r.storeId}::${r.wholesalerId}`} className="hover:bg-gray-50 transition-colors">
                    <td className="px-4 py-3">
                      <span className="font-medium text-gray-900">{r.storeName}</span>
                      {r.storePhone && (
                        <span className="text-xs text-gray-400 ml-2">{r.storePhone}</span>
                      )}
                    </td>
                    <td className="px-4 py-3 text-gray-700">{r.wholesalerName}</td>
                    <td className="px-4 py-3 text-gray-400 text-xs hidden sm:table-cell">
                      {new Date(r.assignedAt).toLocaleDateString('tr-TR')}
                    </td>
                    <td className="px-4 py-3 text-right">
                      {confirmDelete?.storeId === r.storeId && confirmDelete?.wholesalerId === r.wholesalerId ? (
                        <div className="flex items-center gap-2 justify-end">
                          <span className="text-xs text-red-600">Silinsin mi?</span>
                          <button
                            onClick={() => deleteRelation.mutate({ storeId: r.storeId, wholesalerId: r.wholesalerId })}
                            disabled={deleteRelation.isPending}
                            className="px-2 py-1 bg-red-600 text-white rounded text-xs hover:bg-red-700 disabled:opacity-50"
                          >
                            {deleteRelation.isPending ? '…' : 'Evet'}
                          </button>
                          <button
                            onClick={() => setConfirmDelete(null)}
                            className="px-2 py-1 border border-gray-300 rounded text-xs hover:bg-gray-50"
                          >
                            Hayır
                          </button>
                        </div>
                      ) : (
                        <button
                          onClick={() => setConfirmDelete({ storeId: r.storeId, wholesalerId: r.wholesalerId, label: `${r.storeName} ↔ ${r.wholesalerName}` })}
                          className="px-3 py-1.5 text-xs text-red-500 hover:bg-red-50 rounded-lg transition-colors font-medium"
                        >
                          Kaldır
                        </button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      )}
    </Layout>
  )
}
