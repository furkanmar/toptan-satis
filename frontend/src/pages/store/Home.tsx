import { useState, useEffect, useMemo } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import Layout from '../../components/Layout'
import { productsApi, categoriesApi, creditApi } from '../../api/client'
import { useCartStore } from '../../store/cartStore'
import type { Product, Category, CreditSummary, ProductUnitConfig } from '../../types'

const NAV = (wholesalerId: string) => [
  { to: `/store/${wholesalerId}`, label: 'Ürünler' },
  { to: `/store/${wholesalerId}/orders`, label: 'Siparişlerim' },
  { to: `/store/${wholesalerId}/credit`, label: 'Veresiye' },
  { to: `/store/${wholesalerId}/cart`, label: 'Sepetim' },
  { to: '/store', label: '← Toptancı Seç' },
]

type ViewMode = 'card' | 'list'
type SortKey = 'name_asc' | 'name_desc' | 'price_asc' | 'price_desc'

// ─── Ürün kartı ───────────────────────────────────────────────────────────────
function ProductCard({ product, onAdd }: { product: Product; onAdd: (p: Product, uc: ProductUnitConfig, qty: number) => void }) {
  const configs = product.unitConfigs.filter(c => c.isActive)
  const [selectedId, setSelectedId] = useState(configs[0]?.id ?? '')
  const [qty, setQty] = useState(1)
  const selected = configs.find(c => c.id === selectedId) ?? configs[0]
  const mainImg = product.images.find(i => i.isMain) ?? product.images[0]

  // Birim değişince qty sıfırla
  const handleSelectConfig = (id: string) => { setSelectedId(id); setQty(1) }

  const handleAdd = () => {
    if (!selected) return
    onAdd(product, selected, qty)
    setQty(1)
  }

  if (configs.length === 0) return null

  return (
    <div className="bg-white rounded-xl border border-gray-200 overflow-hidden hover:shadow-md transition-shadow flex flex-col">
      <div className="aspect-square bg-gray-100 shrink-0">
        {mainImg
          ? <img src={mainImg.url} alt={product.name} className="w-full h-full object-cover" />
          : <div className="w-full h-full flex items-center justify-center text-gray-300 text-4xl">📦</div>}
      </div>
      <div className="p-3 flex flex-col flex-1">
        <p className="font-medium text-sm text-gray-900 truncate">{product.name}</p>
        {(product.brand || product.manufacturer) && (
          <p className="text-xs text-gray-400 truncate">{[product.brand, product.manufacturer].filter(Boolean).join(' · ')}</p>
        )}

        {/* Birim seçimi */}
        {configs.length > 1 && (
          <div className="flex gap-1 mt-2 flex-wrap">
            {configs.map(c => (
              <button key={c.id} onClick={() => handleSelectConfig(c.id)}
                className={`px-2 py-0.5 rounded text-xs border transition-colors ${c.id === selectedId ? 'bg-blue-600 text-white border-blue-600' : 'border-gray-300 text-gray-500 hover:border-blue-400'}`}>
                {c.unitType}{c.contentQty > 1 ? ` ×${c.contentQty}` : ''}
              </button>
            ))}
          </div>
        )}

        <div className="mt-auto pt-2">
          {selected && (
            <p className="text-blue-600 font-bold text-sm mb-0.5">
              ₺{selected.price.toFixed(2)}
              <span className="text-xs font-normal text-gray-400 ml-1">
                / {selected.unitType}{selected.contentQty > 1 ? ` (×${selected.contentQty})` : ''}
              </span>
            </p>
          )}
          <p className="text-xs text-gray-400 mb-2">Stok: {product.stock} · Min: {product.minOrderQty}</p>

          {/* Miktar + sepete ekle */}
          <div className="flex items-center gap-1.5">
            <div className="flex items-center border border-gray-200 rounded-lg overflow-hidden">
              <button onClick={() => setQty(q => Math.max(1, q - 1))}
                className="w-7 h-7 bg-gray-50 hover:bg-gray-100 text-gray-600 text-sm flex items-center justify-center transition-colors">−</button>
              <input type="number" min={1} value={qty}
                onChange={e => { const v = parseInt(e.target.value); if (!isNaN(v) && v > 0) setQty(v) }}
                className="w-9 text-center text-xs border-0 focus:outline-none py-0.5" />
              <button onClick={() => setQty(q => q + 1)}
                className="w-7 h-7 bg-gray-50 hover:bg-gray-100 text-gray-600 text-sm flex items-center justify-center transition-colors">+</button>
            </div>
            <button onClick={handleAdd}
              disabled={!selected}
              className="flex-1 py-1.5 bg-blue-600 text-white rounded-lg text-xs font-medium hover:bg-blue-700 disabled:opacity-40 transition-colors">
              + Ekle
            </button>
          </div>
        </div>
      </div>
    </div>
  )
}

// ─── Ürün liste satırı ────────────────────────────────────────────────────────
function ProductRow({ product, onAdd }: { product: Product; onAdd: (p: Product, uc: ProductUnitConfig, qty: number) => void }) {
  const configs = product.unitConfigs.filter(c => c.isActive)
  const [selectedId, setSelectedId] = useState(configs[0]?.id ?? '')
  const [qty, setQty] = useState(1)
  const selected = configs.find(c => c.id === selectedId) ?? configs[0]
  const mainImg = product.images.find(i => i.isMain) ?? product.images[0]

  const handleSelectConfig = (id: string) => { setSelectedId(id); setQty(1) }

  const handleAdd = () => {
    if (!selected) return
    onAdd(product, selected, qty)
    setQty(1)
  }

  if (configs.length === 0) return null

  return (
    <div className="bg-white border border-gray-200 rounded-xl flex items-center gap-3 px-4 py-3 hover:shadow-sm transition-shadow">
      <div className="w-12 h-12 rounded-lg bg-gray-100 overflow-hidden shrink-0">
        {mainImg
          ? <img src={mainImg.url} alt={product.name} className="w-full h-full object-cover" />
          : <div className="w-full h-full flex items-center justify-center text-gray-300 text-xl">📦</div>}
      </div>
      <div className="flex-1 min-w-0">
        <p className="font-medium text-sm text-gray-900 truncate">{product.name}</p>
        <p className="text-xs text-gray-400 truncate">
          {[product.brand, product.manufacturer, product.categoryName].filter(Boolean).join(' · ')}
        </p>
      </div>
      <div className="flex items-center gap-2 shrink-0 flex-wrap justify-end">
        {/* Birim seçimi */}
        {configs.length > 1 && (
          <select value={selectedId} onChange={e => handleSelectConfig(e.target.value)}
            className="text-xs border border-gray-300 rounded px-2 py-1 focus:outline-none focus:ring-1 focus:ring-blue-500 bg-white">
            {configs.map(c => <option key={c.id} value={c.id}>{c.unitType}{c.contentQty > 1 ? ` ×${c.contentQty}` : ''}</option>)}
          </select>
        )}
        {selected && (
          <span className="text-sm font-bold text-blue-600">
            ₺{selected.price.toFixed(2)}
            <span className="text-xs font-normal text-gray-400">/{selected.unitType}</span>
          </span>
        )}
        <span className={`text-xs px-2 py-0.5 rounded-full ${product.stock <= 0 ? 'bg-red-100 text-red-600' : product.stock <= 10 ? 'bg-amber-100 text-amber-700' : 'bg-green-100 text-green-700'}`}>
          {product.stock <= 0 ? 'Yok' : product.stock}
        </span>
        {/* Miktar */}
        <div className="flex items-center border border-gray-200 rounded-lg overflow-hidden">
          <button onClick={() => setQty(q => Math.max(1, q - 1))}
            className="w-6 h-7 bg-gray-50 hover:bg-gray-100 text-gray-600 text-xs flex items-center justify-center transition-colors">−</button>
          <input type="number" min={1} value={qty}
            onChange={e => { const v = parseInt(e.target.value); if (!isNaN(v) && v > 0) setQty(v) }}
            className="w-8 text-center text-xs border-0 focus:outline-none py-0.5" />
          <button onClick={() => setQty(q => q + 1)}
            className="w-6 h-7 bg-gray-50 hover:bg-gray-100 text-gray-600 text-xs flex items-center justify-center transition-colors">+</button>
        </div>
        <button onClick={handleAdd}
          disabled={!selected}
          className="px-3 py-1.5 bg-blue-600 text-white rounded-lg text-xs font-medium hover:bg-blue-700 disabled:opacity-40 transition-colors">
          + Ekle
        </button>
      </div>
    </div>
  )
}

// ─── Ana bileşen ──────────────────────────────────────────────────────────────
export default function StoreHome() {
  const { wholesalerId } = useParams<{ wholesalerId: string }>()
  const navigate = useNavigate()

  const { items, ensureWholesaler, addItem, clearCart } = useCartStore()

  useEffect(() => { if (wholesalerId) ensureWholesaler(wholesalerId) }, [wholesalerId, ensureWholesaler])

  const [viewMode, setViewMode] = useState<ViewMode>(() => (sessionStorage.getItem('storeViewMode') as ViewMode) ?? 'card')
  const [sortKey, setSortKey] = useState<SortKey>(() => (sessionStorage.getItem('storeSortKey') as SortKey) ?? 'name_asc')
  const [search, setSearch] = useState('')
  const [selectedCategory, setSelectedCategory] = useState<string | undefined>()

  // maxOrderAmount uyarısı
  const [maxWarning, setMaxWarning] = useState<{ productName: string; maxAmount: number; currentTotal: number } | null>(null)

  const setView = (v: ViewMode) => { setViewMode(v); sessionStorage.setItem('storeViewMode', v) }
  const setSort = (s: SortKey) => { setSortKey(s); sessionStorage.setItem('storeSortKey', s) }

  const { data: allProducts = [], isLoading } = useQuery<Product[]>({
    queryKey: ['products', wholesalerId],
    queryFn: () => productsApi.getAll({ wholesalerId }),
    enabled: !!wholesalerId,
  })

  const { data: categories = [] } = useQuery<Category[]>({
    queryKey: ['categories', wholesalerId],
    queryFn: () => categoriesApi.getAll(wholesalerId),
    enabled: !!wholesalerId,
  })

  const { data: credit } = useQuery<CreditSummary>({
    queryKey: ['credit', wholesalerId],
    queryFn: () => creditApi.getWholesalerCredit(wholesalerId!),
    enabled: !!wholesalerId,
  })

  const products = useMemo(() => {
    let list = [...allProducts]
    if (selectedCategory) list = list.filter(p => p.categoryId === selectedCategory)
    if (search.trim()) {
      const q = search.toLowerCase()
      list = list.filter(p =>
        p.name.toLowerCase().includes(q) ||
        p.brand?.toLowerCase().includes(q) ||
        p.manufacturer?.toLowerCase().includes(q) ||
        p.unitConfigs.some(uc => uc.barcodes.some(b => b.barcode.includes(q)))
      )
    }
    list.sort((a, b) => {
      if (sortKey === 'name_asc')   return a.name.localeCompare(b.name, 'tr')
      if (sortKey === 'name_desc')  return b.name.localeCompare(a.name, 'tr')
      if (sortKey === 'price_asc')  return a.price - b.price
      if (sortKey === 'price_desc') return b.price - a.price
      return 0
    })
    return list
  }, [allProducts, selectedCategory, search, sortKey])

  // maxOrderAmount kontrolü ile sepete ekle
  const handleAdd = (product: Product, uc: ProductUnitConfig, qty: number) => {
    if (product.maxOrderAmount) {
      const existingTotal = Object.values(items)
        .filter(it => it.product.id === product.id)
        .reduce((s, it) => s + it.unitPrice * it.qty, 0)
      const addedTotal = uc.price * qty
      if (existingTotal + addedTotal > product.maxOrderAmount) {
        setMaxWarning({
          productName: product.name,
          maxAmount: product.maxOrderAmount,
          currentTotal: existingTotal + addedTotal,
        })
        return
      }
    }
    addItem(product, uc, qty)
  }

  const cartEntries = Object.entries(items)
  const cartTotal   = cartEntries.reduce((s, [, it]) => s + it.unitPrice * it.qty, 0)
  const cartCount   = cartEntries.length

  return (
    <Layout navLinks={NAV(wholesalerId ?? '')}>
      {/* maxOrderAmount uyarı popup */}
      {maxWarning && (
        <div className="fixed inset-0 bg-black/40 z-50 flex items-center justify-center p-4">
          <div className="bg-white rounded-2xl shadow-xl max-w-sm w-full p-6">
            <div className="flex items-center gap-3 mb-3">
              <span className="text-2xl">⚠️</span>
              <h2 className="font-bold text-gray-900">Sipariş Limiti Aşıldı</h2>
            </div>
            <p className="text-sm text-gray-700 mb-2">
              <span className="font-semibold">{maxWarning.productName}</span> için toptancı maksimum sipariş tutarı belirlemiş.
            </p>
            <div className="bg-orange-50 border border-orange-200 rounded-lg p-3 mb-4 space-y-1 text-sm">
              <div className="flex justify-between">
                <span className="text-gray-600">Maksimum tutar</span>
                <span className="font-semibold text-orange-700">₺{maxWarning.maxAmount.toFixed(2)}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-gray-600">Sepetinizdeki tutar</span>
                <span className="font-semibold text-red-600">₺{maxWarning.currentTotal.toFixed(2)}</span>
              </div>
            </div>
            <p className="text-xs text-gray-500 mb-4">
              Ürünü yine de sipariş vermek isterseniz toptancıyla iletişime geçin.
            </p>
            <button
              onClick={() => setMaxWarning(null)}
              className="w-full py-2 bg-gray-800 text-white rounded-lg text-sm font-semibold hover:bg-gray-700 transition-colors"
            >
              Tamam
            </button>
          </div>
        </div>
      )}

      {/* Veresiye şeridi */}
      {credit && credit.balance > 0 && (
        <div
          onClick={() => navigate(`/store/${wholesalerId}/credit`)}
          className={`mb-4 rounded-xl border px-4 py-3 flex items-center justify-between cursor-pointer hover:opacity-90 transition-opacity ${credit.overdueAmount > 0 ? 'bg-red-50 border-red-200' : 'bg-blue-50 border-blue-200'}`}
        >
          <div className="flex items-center gap-3">
            <span className="text-lg">{credit.overdueAmount > 0 ? '⚠️' : '💰'}</span>
            <div>
              <p className={`text-sm font-semibold ${credit.overdueAmount > 0 ? 'text-red-800' : 'text-blue-800'}`}>
                Veresiye: ₺{credit.balance.toFixed(2)}
              </p>
              {credit.overdueAmount > 0 && <p className="text-xs text-red-600">Vadesi geçmiş: ₺{credit.overdueAmount.toFixed(2)}</p>}
            </div>
          </div>
          <span className="text-xs text-gray-400">Detay →</span>
        </div>
      )}

      <div className="flex gap-6">
        {/* Ürünler */}
        <div className="flex-1 min-w-0">
          {/* Araç çubuğu */}
          <div className="flex gap-2 mb-3 flex-wrap items-center">
            <div className="relative flex-1 min-w-48">
              <span className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400 text-sm">🔍</span>
              <input value={search} onChange={e => setSearch(e.target.value)}
                placeholder="Ürün, marka veya barkod ara…"
                className="w-full pl-9 pr-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
            </div>
            <select value={sortKey} onChange={e => setSort(e.target.value as SortKey)}
              className="text-sm border border-gray-300 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500 bg-white">
              <option value="name_asc">Ad A→Z</option>
              <option value="name_desc">Ad Z→A</option>
              <option value="price_asc">Fiyat ↑</option>
              <option value="price_desc">Fiyat ↓</option>
            </select>
            <div className="flex rounded-lg border border-gray-300 overflow-hidden">
              <button onClick={() => setView('card')}
                className={`px-3 py-2 text-sm transition-colors ${viewMode === 'card' ? 'bg-blue-600 text-white' : 'text-gray-500 hover:bg-gray-50'}`}
                title="Kart">⊞</button>
              <button onClick={() => setView('list')}
                className={`px-3 py-2 text-sm border-l border-gray-300 transition-colors ${viewMode === 'list' ? 'bg-blue-600 text-white' : 'text-gray-500 hover:bg-gray-50'}`}
                title="Liste">☰</button>
            </div>
          </div>

          {/* Kategori tabları */}
          <div className="flex gap-2 mb-4 flex-wrap">
            <button onClick={() => setSelectedCategory(undefined)}
              className={`px-3 py-1 rounded-full text-sm border transition-colors ${!selectedCategory ? 'bg-blue-600 text-white border-blue-600' : 'border-gray-300 text-gray-600 hover:border-gray-400'}`}>
              Tümü <span className="text-xs opacity-60">({allProducts.length})</span>
            </button>
            {categories.map((cat: Category) => (
              <button key={cat.id} onClick={() => setSelectedCategory(cat.id)}
                className={`px-3 py-1 rounded-full text-sm border transition-colors ${selectedCategory === cat.id ? 'bg-blue-600 text-white border-blue-600' : 'border-gray-300 text-gray-600 hover:border-gray-400'}`}>
                {cat.name} <span className="text-xs opacity-60">({allProducts.filter(p => p.categoryId === cat.id).length})</span>
              </button>
            ))}
          </div>

          {/* Ürünler */}
          {isLoading ? (
            <div className="text-center py-12 text-gray-400">Yükleniyor…</div>
          ) : products.length === 0 ? (
            <div className="text-center py-12 text-gray-400">
              {search ? `"${search}" için sonuç yok` : 'Bu kategoride ürün yok'}
            </div>
          ) : viewMode === 'card' ? (
            <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4">
              {products.map(p => (
                <ProductCard key={p.id} product={p} onAdd={handleAdd} />
              ))}
            </div>
          ) : (
            <div className="flex flex-col gap-2">
              {products.map(p => (
                <ProductRow key={p.id} product={p} onAdd={handleAdd} />
              ))}
            </div>
          )}
        </div>

        {/* Sepet özeti */}
        <div className="w-64 shrink-0">
          <div className="bg-white rounded-xl border border-gray-200 p-4 sticky top-6">
            <div className="flex items-center justify-between mb-4">
              <h2 className="font-semibold text-gray-900">Sepet</h2>
              {cartCount > 0 && (
                <button onClick={clearCart} className="text-xs text-gray-400 hover:text-red-500 transition-colors">Temizle</button>
              )}
            </div>

            {cartCount === 0 ? (
              <p className="text-sm text-gray-400 text-center py-4">Sepet boş</p>
            ) : (
              <>
                <div className="space-y-2 mb-4 max-h-64 overflow-y-auto">
                  {cartEntries.slice(0, 5).map(([, it]) => (
                    <div key={it.product.id + it.unitConfigId} className="flex justify-between items-center text-xs">
                      <span className="text-gray-700 truncate flex-1 mr-2">{it.product.name}</span>
                      <span className="text-gray-500 shrink-0">{it.qty} × ₺{it.unitPrice.toFixed(2)}</span>
                    </div>
                  ))}
                  {cartEntries.length > 5 && (
                    <p className="text-xs text-gray-400 text-center">+{cartEntries.length - 5} ürün daha…</p>
                  )}
                </div>

                <div className="border-t border-gray-100 pt-3 mb-4">
                  <div className="flex justify-between items-center">
                    <span className="text-sm text-gray-600">{cartCount} kalem</span>
                    <span className="text-base font-bold text-blue-600">₺{cartTotal.toFixed(2)}</span>
                  </div>
                </div>

                <button
                  onClick={() => navigate(`/store/${wholesalerId}/cart`)}
                  className="w-full py-2.5 bg-blue-600 text-white rounded-lg text-sm font-semibold hover:bg-blue-700 transition-colors"
                >
                  Sepete Git →
                </button>
              </>
            )}
          </div>
        </div>
      </div>
    </Layout>
  )
}
