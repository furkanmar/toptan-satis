import { useState, useRef } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import Layout from '../../components/Layout'
import { productsApi, categoriesApi } from '../../api/client'
import { useAuthStore } from '../../store/authStore'
import type { Product, Category } from '../../types'

const NAV = [
  { to: '/wholesaler', label: 'Ana Sayfa' },
  { to: '/wholesaler/products', label: 'Ürünler' },
  { to: '/wholesaler/orders', label: 'Siparişler' },
  { to: '/wholesaler/credit', label: 'Veresiye' },
  { to: '/wholesaler/settings', label: 'Ayarlar' }
]

export default function WholesalerProducts() {
  const qc = useQueryClient()
  const profileId = useAuthStore(s => s.profileId)
  const [showForm, setShowForm] = useState(false)
  const [selectedProduct, setSelectedProduct] = useState<Product | null>(null)
  const [form, setForm] = useState({ name: '', description: '', price: '', unit: 'Adet', minOrderQty: '1', stock: '0', categoryId: '' })
  const fileInputRef = useRef<HTMLInputElement>(null)

  const uploadMutation = useMutation({
    mutationFn: ({ id, file }: { id: string; file: File }) => productsApi.uploadImage(id, file),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['my-products'] })
    }
  })

  const { data: products = [], isLoading } = useQuery<Product[]>({
    queryKey: ['my-products', profileId],
    queryFn: () => productsApi.getAll({ wholesalerId: profileId ?? undefined }),
    enabled: !!profileId
  })

  const { data: categories = [] } = useQuery<Category[]>({
    queryKey: ['categories', profileId],
    queryFn: () => categoriesApi.getAll(profileId ?? undefined),
    enabled: !!profileId
  })

  const createMutation = useMutation({
    mutationFn: (data: typeof form) => productsApi.create({
      ...data,
      price: parseFloat(data.price),
      minOrderQty: parseInt(data.minOrderQty),
      stock: parseInt(data.stock),
    }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['my-products'] })
      setShowForm(false)
      setForm({ name: '', description: '', price: '', unit: 'Adet', minOrderQty: '1', stock: '0', categoryId: '' })
    }
  })

  const toggleActive = useMutation({
    mutationFn: ({ id, isActive }: { id: string; isActive: boolean }) =>
      productsApi.update(id, { isActive }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['my-products'] })
  })

  return (
    <Layout navLinks={NAV}>
      <div className="flex items-center justify-between mb-4">
        <h1 className="text-xl font-bold text-gray-900">Ürünlerim</h1>
        <button
          onClick={() => setShowForm(!showForm)}
          className="px-4 py-2 bg-blue-600 text-white rounded-lg text-sm font-medium hover:bg-blue-700 transition-colors"
        >
          + Yeni Ürün
        </button>
      </div>

      {showForm && (
        <div className="bg-white rounded-xl border border-gray-200 p-4 mb-4">
          <h2 className="font-semibold text-gray-900 mb-3">Yeni Ürün Ekle</h2>
          <div className="grid grid-cols-2 gap-3">
            <div className="col-span-2">
              <label className="block text-xs font-medium text-gray-700 mb-1">Ürün Adı</label>
              <input
                value={form.name}
                onChange={e => setForm(f => ({ ...f, name: e.target.value }))}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-700 mb-1">Fiyat (₺)</label>
              <input
                type="number"
                value={form.price}
                onChange={e => setForm(f => ({ ...f, price: e.target.value }))}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-700 mb-1">Birim</label>
              <select
                value={form.unit}
                onChange={e => setForm(f => ({ ...f, unit: e.target.value }))}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              >
                {['Adet', 'Kg', 'Koli', 'Litre', 'Paket'].map(u => <option key={u}>{u}</option>)}
              </select>
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-700 mb-1">Stok</label>
              <input
                type="number"
                value={form.stock}
                onChange={e => setForm(f => ({ ...f, stock: e.target.value }))}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-700 mb-1">Min Sipariş</label>
              <input
                type="number"
                value={form.minOrderQty}
                onChange={e => setForm(f => ({ ...f, minOrderQty: e.target.value }))}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
            <div className="col-span-2">
              <label className="block text-xs font-medium text-gray-700 mb-1">Kategori</label>
              <select
                value={form.categoryId}
                onChange={e => setForm(f => ({ ...f, categoryId: e.target.value }))}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              >
                <option value="">Seçin...</option>
                {categories.map((c: Category) => <option key={c.id} value={c.id}>{c.name}</option>)}
              </select>
            </div>
          </div>
          <div className="flex gap-2 mt-3">
            <button
              onClick={() => createMutation.mutate(form)}
              disabled={createMutation.isPending || !form.name || !form.price || !form.categoryId}
              className="px-4 py-2 bg-blue-600 text-white rounded-lg text-sm font-medium hover:bg-blue-700 disabled:opacity-50 transition-colors"
            >
              {createMutation.isPending ? 'Kaydediliyor...' : 'Kaydet'}
            </button>
            <button
              onClick={() => setShowForm(false)}
              className="px-4 py-2 border border-gray-300 text-gray-700 rounded-lg text-sm hover:bg-gray-50 transition-colors"
            >
              İptal
            </button>
          </div>
        </div>
      )}

      {isLoading ? (
        <div className="text-center py-12 text-gray-400">Yükleniyor...</div>
      ) : (
        <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-gray-100 bg-gray-50">
                <th className="text-left px-4 py-3 text-xs font-medium text-gray-500">Ürün</th>
                <th className="text-left px-4 py-3 text-xs font-medium text-gray-500">Kategori</th>
                <th className="text-right px-4 py-3 text-xs font-medium text-gray-500">Fiyat</th>
                <th className="text-right px-4 py-3 text-xs font-medium text-gray-500">Stok</th>
                <th className="text-center px-4 py-3 text-xs font-medium text-gray-500">Durum</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-50">
              {products.map((p: Product) => {
                const mainImg = p.images.find(i => i.isMain) ?? p.images[0]
                const isSelected = selectedProduct?.id === p.id
                return (
                  <>
                    <tr
                      key={p.id}
                      onClick={() => setSelectedProduct(isSelected ? null : p)}
                      className="hover:bg-gray-50 transition-colors cursor-pointer"
                    >
                      <td className="px-4 py-3">
                        <div className="flex items-center gap-3">
                          <div className="w-10 h-10 rounded-lg bg-gray-100 overflow-hidden shrink-0">
                            {mainImg
                              ? <img src={mainImg.url} alt={p.name} className="w-full h-full object-cover" />
                              : <div className="w-full h-full flex items-center justify-center text-gray-300 text-lg">📦</div>
                            }
                          </div>
                          <div>
                            <span className="font-medium text-gray-900">{p.name}</span>
                            {p.description && <span className="block text-xs text-gray-400 truncate max-w-xs">{p.description}</span>}
                          </div>
                        </div>
                      </td>
                      <td className="px-4 py-3 text-gray-600">{p.categoryName}</td>
                      <td className="px-4 py-3 text-right font-medium text-gray-900">₺{p.price.toFixed(2)} / {p.unit}</td>
                      <td className="px-4 py-3 text-right text-gray-600">{p.stock}</td>
                      <td className="px-4 py-3 text-center">
                        <button
                          onClick={e => { e.stopPropagation(); toggleActive.mutate({ id: p.id, isActive: !p.isActive }) }}
                          className={`px-2 py-0.5 rounded-full text-xs font-medium transition-colors ${p.isActive ? 'bg-green-100 text-green-700 hover:bg-green-200' : 'bg-gray-100 text-gray-500 hover:bg-gray-200'}`}
                        >
                          {p.isActive ? 'Aktif' : 'Pasif'}
                        </button>
                      </td>
                    </tr>
                    {isSelected && (
                      <tr key={`${p.id}-detail`} className="bg-blue-50 border-t border-blue-100">
                        <td colSpan={5} className="px-4 py-3">
                          <div className="flex items-center gap-3">
                            <span className="text-xs font-medium text-gray-600">Görseller:</span>
                            {p.images.map(img => (
                              <img key={img.id} src={img.url} alt="" className="w-12 h-12 rounded-lg object-cover border border-gray-200" />
                            ))}
                            <input
                              ref={fileInputRef}
                              type="file"
                              accept="image/*"
                              className="hidden"
                              onChange={e => {
                                const file = e.target.files?.[0]
                                if (file) uploadMutation.mutate({ id: p.id, file })
                                e.target.value = ''
                              }}
                            />
                            <button
                              onClick={() => fileInputRef.current?.click()}
                              disabled={uploadMutation.isPending}
                              className="px-3 py-1.5 border border-dashed border-blue-400 text-blue-600 rounded-lg text-xs hover:bg-blue-100 transition-colors disabled:opacity-50"
                            >
                              {uploadMutation.isPending ? 'Yükleniyor...' : '+ Görsel Ekle'}
                            </button>
                          </div>
                        </td>
                      </tr>
                    )}
                  </>
                )
              })}
            </tbody>
          </table>
          {products.length === 0 && (
            <div className="text-center py-12 text-gray-400">Henüz ürün yok</div>
          )}
        </div>
      )}
    </Layout>
  )
}
