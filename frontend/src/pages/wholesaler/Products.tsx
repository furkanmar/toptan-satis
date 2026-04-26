import { useState, useRef, useCallback } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import Layout from '../../components/Layout'
import { productsApi, categoriesApi, catalogApi } from '../../api/client'
import { useAuthStore } from '../../store/authStore'
import type { Product, Category, CatalogItem } from '../../types'

const NAV = [
  { to: '/wholesaler', label: 'Ana Sayfa' },
  { to: '/wholesaler/products', label: 'Ürünler' },
  { to: '/wholesaler/orders', label: 'Siparişler' },
  { to: '/wholesaler/credit', label: 'Veresiye' },
  { to: '/wholesaler/settings', label: 'Ayarlar' },
]

const UNITS = ['Adet', 'Kg', 'Koli', 'Litre', 'Paket']

// ─── Types ────────────────────────────────────────────────────────────────────

interface ProductForm {
  name: string
  description: string
  price: string
  unit: string
  minOrderQty: string
  stock: string
  categoryId: string
  catalogItemId: string
  brand: string
  manufacturer: string
  newBarcodes: string  // virgülle ayrılmış
}

const emptyForm = (): ProductForm => ({
  name: '', description: '', price: '', unit: 'Adet',
  minOrderQty: '1', stock: '0', categoryId: '',
  catalogItemId: '', brand: '', manufacturer: '', newBarcodes: ''
})

// ─── Sub-components ───────────────────────────────────────────────────────────

function StockBadge({ stock }: { stock: number }) {
  if (stock <= 0) return <span className="px-2 py-0.5 rounded-full text-xs font-medium bg-red-100 text-red-700">Stok yok</span>
  if (stock <= 10) return <span className="px-2 py-0.5 rounded-full text-xs font-medium bg-amber-100 text-amber-700">{stock}</span>
  return <span className="px-2 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-700">{stock}</span>
}

// ─── Catalog search panel inside slide-over ───────────────────────────────────

function CatalogSearch({ onSelect }: { onSelect: (item: CatalogItem) => void }) {
  const [q, setQ] = useState('')
  const [barcode, setBarcode] = useState('')

  const { data, isFetching } = useQuery<{ items: CatalogItem[] }>({
    queryKey: ['catalog-search', q, barcode],
    queryFn: () => catalogApi.search({ q: q || undefined, barcode: barcode || undefined, pageSize: 8 }),
    enabled: q.length >= 2 || barcode.length >= 3,
    staleTime: 30_000,
  })

  return (
    <div>
      <div className="flex gap-2 mb-2">
        <input
          placeholder="Ürün adı veya marka…"
          value={q}
          onChange={e => { setQ(e.target.value); setBarcode('') }}
          className="flex-1 px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
        />
        <input
          placeholder="Barkod"
          value={barcode}
          onChange={e => { setBarcode(e.target.value); setQ('') }}
          className="w-28 px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
        />
      </div>
      {isFetching && <p className="text-xs text-gray-400 mb-2">Aranıyor…</p>}
      {data?.items && data.items.length > 0 && (
        <div className="border border-gray-200 rounded-lg divide-y divide-gray-100 max-h-48 overflow-y-auto mb-2">
          {data.items.map(item => (
            <button
              key={item.id}
              onClick={() => onSelect(item)}
              className="w-full text-left px-3 py-2 hover:bg-blue-50 transition-colors"
            >
              <span className="block text-sm font-medium text-gray-900">{item.name}</span>
              <span className="block text-xs text-gray-500">
                {[item.brand, item.manufacturer, item.unit].filter(Boolean).join(' · ')}
                {item.barcodes.length > 0 && ` · ${item.barcodes[0].barcode}`}
              </span>
            </button>
          ))}
        </div>
      )}
      {data?.items && data.items.length === 0 && (q.length >= 2 || barcode.length >= 3) && (
        <p className="text-xs text-gray-400 mb-2">Katalogda bulunamadı — aşağıda yeni ürün bilgilerini girin.</p>
      )}
    </div>
  )
}

// ─── Image Manager inside slide-over ─────────────────────────────────────────

function ImageManager({ product, onRefresh }: { product: Product; onRefresh: () => void }) {
  const fileRef = useRef<HTMLInputElement>(null)
  const qc = useQueryClient()

  const upload = useMutation({
    mutationFn: (file: File) => productsApi.uploadImage(product.id, file),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['my-products'] }); onRefresh() }
  })

  const deleteImg = useMutation({
    mutationFn: (imageId: string) => productsApi.deleteImage(product.id, imageId),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['my-products'] }); onRefresh() }
  })

  const setMain = useMutation({
    mutationFn: (imageId: string) => productsApi.setMainImage(product.id, imageId),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['my-products'] }); onRefresh() }
  })

  return (
    <div>
      <div className="flex flex-wrap gap-2 mb-2">
        {product.images.map(img => (
          <div key={img.id} className="relative group w-20 h-20 rounded-lg overflow-hidden border-2 border-gray-200">
            <img src={img.url} alt="" className="w-full h-full object-cover" />
            {img.isMain && (
              <span className="absolute top-0 left-0 right-0 text-center text-xs bg-blue-600 text-white py-0.5">Ana</span>
            )}
            <div className="absolute inset-0 bg-black/50 opacity-0 group-hover:opacity-100 transition-opacity flex items-center justify-center gap-1">
              {!img.isMain && (
                <button
                  onClick={() => setMain.mutate(img.id)}
                  title="Ana yap"
                  className="p-1 bg-blue-500 text-white rounded text-xs"
                >★</button>
              )}
              <button
                onClick={() => deleteImg.mutate(img.id)}
                title="Sil"
                className="p-1 bg-red-500 text-white rounded text-xs"
              >✕</button>
            </div>
          </div>
        ))}
        <button
          onClick={() => fileRef.current?.click()}
          disabled={upload.isPending}
          className="w-20 h-20 rounded-lg border-2 border-dashed border-gray-300 hover:border-blue-400 flex items-center justify-center text-gray-400 hover:text-blue-500 text-2xl transition-colors disabled:opacity-50"
        >
          {upload.isPending ? '…' : '+'}
        </button>
      </div>
      <input
        ref={fileRef}
        type="file"
        accept="image/*"
        className="hidden"
        onChange={e => {
          const file = e.target.files?.[0]
          if (file) upload.mutate(file)
          e.target.value = ''
        }}
      />
    </div>
  )
}

// ─── Slide-over panel ─────────────────────────────────────────────────────────

interface SlideOverProps {
  product: Product | null   // null = yeni ürün
  categories: Category[]
  profileId: string
  onClose: () => void
}

function SlideOver({ product, categories, profileId, onClose }: SlideOverProps) {
  const qc = useQueryClient()
  const isEdit = !!product
  const [form, setForm] = useState<ProductForm>(() => product
    ? {
        name: product.name,
        description: product.description ?? '',
        price: product.price.toString(),
        unit: product.unit,
        minOrderQty: product.minOrderQty.toString(),
        stock: product.stock.toString(),
        categoryId: product.categoryId,
        catalogItemId: product.catalogItemId ?? '',
        brand: product.brand ?? '',
        manufacturer: product.manufacturer ?? '',
        newBarcodes: '',
      }
    : emptyForm()
  )
  const [showCatalogSearch, setShowCatalogSearch] = useState(!isEdit && !form.catalogItemId)
  const set = (k: keyof ProductForm) => (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>) =>
    setForm(f => ({ ...f, [k]: e.target.value }))

  const onCatalogSelect = useCallback((item: CatalogItem) => {
    setForm(f => ({
      ...f,
      name: f.name || item.name,
      unit: item.unit,
      brand: item.brand ?? '',
      manufacturer: item.manufacturer ?? '',
      catalogItemId: item.id,
    }))
    setShowCatalogSearch(false)
  }, [])

  const saveMutation = useMutation({
    mutationFn: async (f: ProductForm) => {
      const payload = {
        name: f.name,
        description: f.description || undefined,
        price: parseFloat(f.price),
        unit: f.unit,
        minOrderQty: parseInt(f.minOrderQty),
        stock: parseInt(f.stock),
        categoryId: f.categoryId,
        catalogItemId: f.catalogItemId || undefined,
      }
      if (isEdit) {
        return productsApi.update(product!.id, payload)
      } else {
        return productsApi.create({ ...payload, wholesalerId: profileId })
      }
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['my-products'] })
      onClose()
    }
  })

  const valid = form.name.trim() && form.price && parseFloat(form.price) > 0 && form.categoryId

  return (
    <div className="fixed inset-0 z-40 flex justify-end">
      {/* Backdrop */}
      <div className="absolute inset-0 bg-black/30" onClick={onClose} />

      {/* Panel */}
      <div className="relative w-full max-w-md bg-white shadow-2xl flex flex-col h-full overflow-y-auto">
        <div className="flex items-center justify-between px-5 py-4 border-b border-gray-200 sticky top-0 bg-white z-10">
          <h2 className="text-base font-semibold text-gray-900">
            {isEdit ? 'Ürünü Düzenle' : 'Yeni Ürün Ekle'}
          </h2>
          <button onClick={onClose} className="text-gray-400 hover:text-gray-600 text-xl font-light">✕</button>
        </div>

        <div className="flex-1 px-5 py-4 space-y-5">
          {/* Katalog bağlantısı */}
          <section>
            <div className="flex items-center justify-between mb-2">
              <h3 className="text-xs font-semibold text-gray-500 uppercase tracking-wide">Katalog Bilgisi</h3>
              {form.catalogItemId
                ? <button onClick={() => { setForm(f => ({ ...f, catalogItemId: '' })); setShowCatalogSearch(true) }} className="text-xs text-red-500 hover:text-red-700">Bağlantıyı kaldır</button>
                : <button onClick={() => setShowCatalogSearch(s => !s)} className="text-xs text-blue-600 hover:text-blue-800">{showCatalogSearch ? 'Gizle' : 'Katalogdan seç'}</button>
              }
            </div>

            {form.catalogItemId && (
              <div className="flex items-center gap-2 p-2 bg-blue-50 border border-blue-200 rounded-lg mb-2">
                <span className="text-blue-600 text-sm">🔗</span>
                <span className="text-sm text-blue-700 font-medium">Kataloga bağlı</span>
              </div>
            )}

            {showCatalogSearch && !form.catalogItemId && (
              <CatalogSearch onSelect={onCatalogSelect} />
            )}

            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Marka</label>
                <input value={form.brand} onChange={set('brand')} placeholder="Ülker, Pınar…"
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
              </div>
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Menşei / Üretici</label>
                <input value={form.manufacturer} onChange={set('manufacturer')} placeholder="Türkiye"
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
              </div>
            </div>
          </section>

          {/* Ürün bilgileri */}
          <section>
            <h3 className="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-2">Ürün Bilgileri</h3>
            <div className="space-y-3">
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Ürün Adı *</label>
                <input value={form.name} onChange={set('name')}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
              </div>
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Açıklama</label>
                <textarea value={form.description} onChange={set('description')} rows={2}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 resize-none" />
              </div>
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Kategori *</label>
                <select value={form.categoryId} onChange={set('categoryId')}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500">
                  <option value="">Seçin…</option>
                  {categories.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
                </select>
              </div>
            </div>
          </section>

          {/* Fiyat & Stok */}
          <section>
            <h3 className="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-2">Fiyat & Stok</h3>
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Fiyat (₺) *</label>
                <input type="number" min="0" step="0.01" value={form.price} onChange={set('price')}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
              </div>
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Birim</label>
                <select value={form.unit} onChange={set('unit')}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500">
                  {UNITS.map(u => <option key={u}>{u}</option>)}
                </select>
              </div>
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Stok</label>
                <input type="number" min="0" value={form.stock} onChange={set('stock')}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
              </div>
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Min Sipariş</label>
                <input type="number" min="1" value={form.minOrderQty} onChange={set('minOrderQty')}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
              </div>
            </div>
          </section>

          {/* Görsel yönetimi (sadece düzenleme modunda) */}
          {isEdit && (
            <section>
              <h3 className="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-2">Görseller</h3>
              <ImageManager product={product!} onRefresh={() => qc.invalidateQueries({ queryKey: ['my-products'] })} />
            </section>
          )}
        </div>

        {/* Footer */}
        <div className="px-5 py-4 border-t border-gray-200 flex gap-2 sticky bottom-0 bg-white">
          <button
            onClick={() => saveMutation.mutate(form)}
            disabled={!valid || saveMutation.isPending}
            className="flex-1 px-4 py-2.5 bg-blue-600 text-white rounded-lg text-sm font-medium hover:bg-blue-700 disabled:opacity-50 transition-colors"
          >
            {saveMutation.isPending ? 'Kaydediliyor…' : isEdit ? 'Güncelle' : 'Ekle'}
          </button>
          <button onClick={onClose}
            className="px-4 py-2.5 border border-gray-300 text-gray-700 rounded-lg text-sm hover:bg-gray-50 transition-colors">
            İptal
          </button>
        </div>
      </div>
    </div>
  )
}

// ─── Main page ────────────────────────────────────────────────────────────────

export default function WholesalerProducts() {
  const qc = useQueryClient()
  const profileId = useAuthStore(s => s.profileId)

  const [selectedCategoryId, setSelectedCategoryId] = useState<string | null>(null)  // null = Tümü
  const [search, setSearch] = useState('')
  const [filterActive, setFilterActive] = useState<'all' | 'active' | 'passive'>('all')
  const [slideOver, setSlideOver] = useState<{ open: boolean; product: Product | null }>({ open: false, product: null })
  // Inline stok düzenleme
  const [editingStock, setEditingStock] = useState<{ id: string; value: string } | null>(null)

  const { data: allProducts = [], isLoading } = useQuery<Product[]>({
    queryKey: ['my-products', profileId],
    queryFn: () => productsApi.getAll({ wholesalerId: profileId ?? undefined, includeInactive: true }),
    enabled: !!profileId,
  })

  const { data: categories = [] } = useQuery<Category[]>({
    queryKey: ['categories', profileId],
    queryFn: () => categoriesApi.getAll(profileId ?? undefined),
    enabled: !!profileId,
  })

  const toggleActive = useMutation({
    mutationFn: ({ id, isActive }: { id: string; isActive: boolean }) => productsApi.update(id, { isActive }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['my-products'] })
  })

  const updateStock = useMutation({
    mutationFn: ({ id, stock }: { id: string; stock: number }) => productsApi.update(id, { stock }),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['my-products'] }); setEditingStock(null) }
  })

  // Kategori bazlı ürün sayıları
  const countByCategory = (catId: string) => allProducts.filter(p => p.categoryId === catId).length

  // Filtrelenmiş ürünler
  const filtered = allProducts.filter(p => {
    if (selectedCategoryId && p.categoryId !== selectedCategoryId) return false
    if (filterActive === 'active' && !p.isActive) return false
    if (filterActive === 'passive' && p.isActive) return false
    if (search && !p.name.toLowerCase().includes(search.toLowerCase()) &&
        !(p.brand?.toLowerCase().includes(search.toLowerCase()))) return false
    return true
  })

  const openNew = () => setSlideOver({ open: true, product: null })
  const openEdit = (p: Product) => setSlideOver({ open: true, product: p })
  const closeSlide = () => setSlideOver({ open: false, product: null })

  return (
    <Layout navLinks={NAV}>
      {/* Header */}
      <div className="flex items-center justify-between mb-4">
        <h1 className="text-xl font-bold text-gray-900">Ürünlerim</h1>
        <button
          onClick={openNew}
          className="px-4 py-2 bg-blue-600 text-white rounded-lg text-sm font-medium hover:bg-blue-700 transition-colors"
        >
          + Yeni Ürün
        </button>
      </div>

      {/* Kategori kartları */}
      <div className="flex gap-2 overflow-x-auto pb-2 mb-4 scrollbar-hide">
        <button
          onClick={() => setSelectedCategoryId(null)}
          className={`shrink-0 px-4 py-2 rounded-xl text-sm font-medium transition-colors border ${
            selectedCategoryId === null
              ? 'bg-blue-600 text-white border-blue-600'
              : 'bg-white text-gray-700 border-gray-200 hover:border-blue-300'
          }`}
        >
          Tümü
          <span className={`ml-1.5 text-xs px-1.5 py-0.5 rounded-full ${selectedCategoryId === null ? 'bg-blue-500 text-white' : 'bg-gray-100 text-gray-600'}`}>
            {allProducts.length}
          </span>
        </button>
        {categories.map(cat => (
          <button
            key={cat.id}
            onClick={() => setSelectedCategoryId(selectedCategoryId === cat.id ? null : cat.id)}
            className={`shrink-0 px-4 py-2 rounded-xl text-sm font-medium transition-colors border ${
              selectedCategoryId === cat.id
                ? 'bg-blue-600 text-white border-blue-600'
                : 'bg-white text-gray-700 border-gray-200 hover:border-blue-300'
            }`}
          >
            {cat.name}
            <span className={`ml-1.5 text-xs px-1.5 py-0.5 rounded-full ${selectedCategoryId === cat.id ? 'bg-blue-500 text-white' : 'bg-gray-100 text-gray-600'}`}>
              {countByCategory(cat.id)}
            </span>
          </button>
        ))}
      </div>

      {/* Arama + Filtreler */}
      <div className="flex gap-3 mb-4">
        <div className="relative flex-1">
          <span className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400 text-sm">🔍</span>
          <input
            value={search}
            onChange={e => setSearch(e.target.value)}
            placeholder="Ürün adı veya marka ara…"
            className="w-full pl-8 pr-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
        </div>
        <select
          value={filterActive}
          onChange={e => setFilterActive(e.target.value as typeof filterActive)}
          className="px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 bg-white"
        >
          <option value="all">Tüm durumlar</option>
          <option value="active">Aktif</option>
          <option value="passive">Pasif</option>
        </select>
      </div>

      {/* Ürün tablosu */}
      {isLoading ? (
        <div className="text-center py-16 text-gray-400">Yükleniyor…</div>
      ) : (
        <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
          {filtered.length === 0 ? (
            <div className="text-center py-16 text-gray-400">
              {search || filterActive !== 'all' ? 'Filtrelerle eşleşen ürün yok' : 'Henüz ürün yok — + Yeni Ürün ile başlayın'}
            </div>
          ) : (
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-gray-100 bg-gray-50">
                  <th className="text-left px-4 py-3 text-xs font-medium text-gray-500">Ürün</th>
                  <th className="text-left px-4 py-3 text-xs font-medium text-gray-500 hidden md:table-cell">Kategori</th>
                  <th className="text-right px-4 py-3 text-xs font-medium text-gray-500">Fiyat</th>
                  <th className="text-center px-4 py-3 text-xs font-medium text-gray-500">Stok</th>
                  <th className="text-right px-4 py-3 text-xs font-medium text-gray-500 hidden sm:table-cell">Min</th>
                  <th className="text-center px-4 py-3 text-xs font-medium text-gray-500">Durum</th>
                  <th className="px-4 py-3"></th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-50">
                {filtered.map(p => {
                  const mainImg = p.images.find(i => i.isMain) ?? p.images[0]
                  return (
                    <tr key={p.id} className="hover:bg-gray-50 transition-colors">
                      {/* Ürün */}
                      <td className="px-4 py-3">
                        <div className="flex items-center gap-3">
                          <div className="w-10 h-10 rounded-lg bg-gray-100 overflow-hidden shrink-0">
                            {mainImg
                              ? <img src={mainImg.url} alt={p.name} className="w-full h-full object-cover" />
                              : <div className="w-full h-full flex items-center justify-center text-gray-300 text-lg">📦</div>
                            }
                          </div>
                          <div className="min-w-0">
                            <span className="font-medium text-gray-900 block truncate">{p.name}</span>
                            <div className="flex items-center gap-1.5 flex-wrap">
                              {p.brand && <span className="text-xs text-gray-400">{p.brand}</span>}
                              {p.barcodes.length > 0 && (
                                <span className="text-xs text-gray-300 font-mono">{p.barcodes[0].barcode}</span>
                              )}
                              {p.catalogItemId && <span className="text-xs text-blue-400" title="Kataloga bağlı">🔗</span>}
                            </div>
                          </div>
                        </div>
                      </td>

                      {/* Kategori */}
                      <td className="px-4 py-3 text-gray-500 hidden md:table-cell">{p.categoryName}</td>

                      {/* Fiyat */}
                      <td className="px-4 py-3 text-right font-medium text-gray-900 whitespace-nowrap">
                        ₺{p.price.toFixed(2)}
                        <span className="text-xs text-gray-400 ml-1">/{p.unit}</span>
                      </td>

                      {/* Stok — inline düzenleme */}
                      <td className="px-4 py-3 text-center">
                        {editingStock?.id === p.id ? (
                          <div className="flex items-center gap-1 justify-center">
                            <input
                              type="number"
                              min="0"
                              value={editingStock.value}
                              onChange={e => setEditingStock(s => s && ({ ...s, value: e.target.value }))}
                              onKeyDown={e => {
                                if (e.key === 'Enter') updateStock.mutate({ id: p.id, stock: parseInt(editingStock.value) || 0 })
                                if (e.key === 'Escape') setEditingStock(null)
                              }}
                              autoFocus
                              className="w-16 px-2 py-1 border border-blue-400 rounded text-sm text-center focus:outline-none focus:ring-1 focus:ring-blue-500"
                            />
                            <button onClick={() => updateStock.mutate({ id: p.id, stock: parseInt(editingStock.value) || 0 })}
                              className="text-green-600 text-xs hover:text-green-700">✓</button>
                            <button onClick={() => setEditingStock(null)} className="text-gray-400 text-xs hover:text-gray-600">✕</button>
                          </div>
                        ) : (
                          <button
                            onClick={() => setEditingStock({ id: p.id, value: p.stock.toString() })}
                            className="hover:bg-gray-100 rounded px-1"
                            title="Stoku düzenle"
                          >
                            <StockBadge stock={p.stock} />
                          </button>
                        )}
                      </td>

                      {/* Min sipariş */}
                      <td className="px-4 py-3 text-right text-gray-400 text-xs hidden sm:table-cell">
                        min {p.minOrderQty}
                      </td>

                      {/* Durum */}
                      <td className="px-4 py-3 text-center">
                        <button
                          onClick={() => toggleActive.mutate({ id: p.id, isActive: !p.isActive })}
                          className={`px-2 py-0.5 rounded-full text-xs font-medium transition-colors ${
                            p.isActive
                              ? 'bg-green-100 text-green-700 hover:bg-green-200'
                              : 'bg-gray-100 text-gray-500 hover:bg-gray-200'
                          }`}
                        >
                          {p.isActive ? 'Aktif' : 'Pasif'}
                        </button>
                      </td>

                      {/* Düzenle */}
                      <td className="px-4 py-3 text-right">
                        <button
                          onClick={() => openEdit(p)}
                          className="px-3 py-1.5 text-xs text-blue-600 hover:bg-blue-50 rounded-lg transition-colors font-medium"
                        >
                          Düzenle
                        </button>
                      </td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          )}
        </div>
      )}

      {/* Slide-over */}
      {slideOver.open && (
        <SlideOver
          product={slideOver.product}
          categories={categories}
          profileId={profileId ?? ''}
          onClose={closeSlide}
        />
      )}
    </Layout>
  )
}
