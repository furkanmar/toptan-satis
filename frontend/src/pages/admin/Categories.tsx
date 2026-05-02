import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import Layout from '../../components/Layout'
import { api } from '../../api/client'
import type { Category } from '../../types'


export default function AdminCategories() {
  const qc = useQueryClient()
  const [name, setName] = useState('')
  const [error, setError] = useState('')

  const { data: categories = [], isLoading } = useQuery<Category[]>({
    queryKey: ['categories'],
    queryFn: () => api.get('/categories').then(r => r.data)
  })

  const createMutation = useMutation({
    mutationFn: () => api.post('/categories', { name }).then(r => r.data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['categories'] })
      setName('')
      setError('')
    },
    onError: (err: unknown) => {
      const msg = (err as { response?: { data?: { error?: string } } })?.response?.data?.error
      setError(msg ?? 'Hata oluştu')
    }
  })

  const deleteMutation = useMutation({
    mutationFn: (id: string) => api.delete(`/categories/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['categories'] })
  })

  return (
    <Layout>
      <h1 className="text-xl font-bold text-gray-900 mb-4">Kategori Yönetimi</h1>

      <div className="grid grid-cols-3 gap-6">
        {/* Form */}
        <div className="bg-white rounded-xl border border-gray-200 p-4 h-fit">
          <h2 className="font-semibold text-gray-900 mb-3">Yeni Kategori</h2>
          <input
            value={name}
            onChange={e => setName(e.target.value)}
            onKeyDown={e => e.key === 'Enter' && name && createMutation.mutate()}
            placeholder="Kategori adı"
            className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 mb-3"
          />
          {error && (
            <p className="text-xs text-red-600 mb-2">{error}</p>
          )}
          <button
            onClick={() => createMutation.mutate()}
            disabled={!name || createMutation.isPending}
            className="w-full py-2 bg-blue-600 text-white rounded-lg text-sm font-medium hover:bg-blue-700 disabled:opacity-50 transition-colors"
          >
            Ekle
          </button>
        </div>

        {/* Liste */}
        <div className="col-span-2 bg-white rounded-xl border border-gray-200 overflow-hidden h-fit">
          <div className="px-4 py-3 border-b border-gray-100 flex items-center justify-between">
            <h2 className="font-semibold text-gray-900 text-sm">Kategoriler</h2>
            <span className="text-xs text-gray-400">{categories.length} kayıt</span>
          </div>
          {isLoading ? (
            <div className="text-center py-8 text-gray-400 text-sm">Yükleniyor...</div>
          ) : categories.length === 0 ? (
            <div className="text-center py-8 text-gray-400 text-sm">Kategori yok</div>
          ) : (
            <table className="w-full text-sm">
              <thead>
                <tr className="bg-gray-50 border-b border-gray-100">
                  <th className="text-left px-4 py-2 text-xs font-medium text-gray-500">Ad</th>
                  <th className="text-left px-4 py-2 text-xs font-medium text-gray-500">Slug</th>
                  <th className="w-16"></th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-50">
                {categories.map((cat: Category) => (
                  <tr key={cat.id} className="hover:bg-gray-50">
                    <td className="px-4 py-2.5 font-medium text-gray-900">{cat.name}</td>
                    <td className="px-4 py-2.5 text-gray-400 font-mono text-xs">{cat.slug}</td>
                    <td className="px-4 py-2.5 text-right">
                      <button
                        onClick={() => deleteMutation.mutate(cat.id)}
                        className="text-xs text-red-500 hover:text-red-700 transition-colors"
                      >
                        Sil
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      </div>
    </Layout>
  )
}
