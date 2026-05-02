import { useState, useRef, useEffect, useCallback } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import Layout from '../../components/Layout'
import StockHistoryModal from '../../components/StockHistoryModal'
import { productsApi, categoriesApi } from '../../api/client'
import { useAuthStore } from '../../store/authStore'
import type { Product, Category, ProductUnitConfig } from '../../types'


const UNIT_TYPES = ['Adet', 'Kg', 'Koli', 'Litre', 'Paket']

// ─── Types ────────────────────────────────────────────────────────────────────

const VAT_RATES = [0, 1, 10, 18, 20]

interface ProductForm {
  name: string
  description: string
  price: string
  vatRate: string
  minOrderQty: string
  stock: string
  categoryId: string
  brand: string
  manufacturer: string
  minimumStockLevel: string  // '' = alarm yok
}

const emptyForm = (): ProductForm => ({
  name: '', description: '', price: '', vatRate: '18', minOrderQty: '1',
  stock: '0', categoryId: '', brand: '', manufacturer: '', minimumStockLevel: '',
})

// ─── Helpers ──────────────────────────────────────────────────────────────────

function StockBadge({ stock, minimumStockLevel }: { stock: number; minimumStockLevel?: number | null }) {
  const isLow = minimumStockLevel != null && stock <= minimumStockLevel
  if (stock <= 0) return (
    <span className="px-2 py-0.5 rounded-full text-xs font-medium bg-red-100 text-red-700">Stok yok</span>
  )
  if (isLow) return (
    <span className="px-2 py-0.5 rounded-full text-xs font-medium bg-red-100 text-red-700">
      ⚠️ Düşük: {stock}
    </span>
  )
  if (stock <= 10) return <span className="px-2 py-0.5 rounded-full text-xs font-medium bg-amber-100 text-amber-700">{stock}</span>
  return <span className="px-2 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-700">{stock}</span>
}

// ─── Image Manager ────────────────────────────────────────────────────────────

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
                <button onClick={() => setMain.mutate(img.id)} title="Ana yap"
                  className="p-1 bg-blue-500 text-white rounded text-xs">★</button>
              )}
              <button onClick={() => deleteImg.mutate(img.id)} title="Sil"
                className="p-1 bg-red-500 text-white rounded text-xs">✕</button>
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
      <input ref={fileRef} type="file" accept="image/*" className="hidden"
        onChange={e => { const f = e.target.files?.[0]; if (f) upload.mutate(f); e.target.value = '' }} />
    </div>
  )
}

// ─── Unit Config Row ──────────────────────────────────────────────────────────

interface UnitConfigRowProps {
  productId: string
  config: ProductUnitConfig
  onChanged: () => void
}

function UnitConfigRow({ productId, config, onChanged }: UnitConfigRowProps) {
  const qc = useQueryClient()
  const [editing, setEditing] = useState(false)
  const [editForm, setEditForm] = useState({
    unitType: config.unitType,
    contentQty: config.contentQty.toString(),
    price: config.price.toString(),
    sortOrder: config.sortOrder.toString(),
  })
  const [newBarcode, setNewBarcode] = useState('')
  const [confirmDelete, setConfirmDelete] = useState(false)

  const refresh = useCallback(() => {
    qc.invalidateQueries({ queryKey: ['my-products'] })
    onChanged()
  }, [qc, onChanged])

  const updateConfig = useMutation({
    mutationFn: () => productsApi.updateUnitConfig(productId, config.id, {
      unitType: editForm.unitType,
      contentQty: parseInt(editForm.contentQty) || 1,
      price: parseFloat(editForm.price) || 0,
      sortOrder: parseInt(editForm.sortOrder) || 0,
    }),
    onSuccess: () => { setEditing(false); refresh() }
  })

  const toggleActive = useMutation({
    mutationFn: () => productsApi.updateUnitConfig(productId, config.id, { isActive: !config.isActive }),
    onSuccess: refresh
  })

  const deleteConfig = useMutation({
    mutationFn: () => productsApi.deleteUnitConfig(productId, config.id),
    onSuccess: refresh
  })

  const addBarcode = useMutation({
    mutationFn: () => productsApi.addBarcode(productId, config.id, newBarcode.trim()),
    onSuccess: () => { setNewBarcode(''); refresh() }
  })

  const deleteBarcode = useMutation({
    mutationFn: (barcodeId: string) => productsApi.deleteBarcode(productId, config.id, barcodeId),
    onSuccess: refresh
  })

  if (confirmDelete) {
    return (
      <div className="flex items-center gap-2 p-2 bg-red-50 border border-red-200 rounded-lg text-sm">
        <span className="flex-1 text-red-700">"{config.unitType}" silinsin mi?</span>
        <button onClick={() => deleteConfig.mutate()}
          className="px-2 py-1 bg-red-600 text-white rounded text-xs hover:bg-red-700">Evet, sil</button>
        <button onClick={() => setConfirmDelete(false)}
          className="px-2 py-1 border border-gray-300 rounded text-xs hover:bg-gray-50">Vazgeç</button>
      </div>
    )
  }

  return (
    <div className="border border-gray-200 rounded-lg p-3 space-y-2">
      {editing ? (
        <div className="space-y-2">
          <div className="grid grid-cols-4 gap-2">
            <div>
              <label className="text-xs text-gray-500 mb-0.5 block">Birim</label>
              <select value={editForm.unitType} onChange={e => setEditForm(f => ({ ...f, unitType: e.target.value }))}
                className="w-full px-2 py-1.5 border border-gray-300 rounded text-sm focus:outline-none focus:ring-1 focus:ring-blue-500">
                {UNIT_TYPES.map(u => <option key={u}>{u}</option>)}
              </select>
            </div>
            <div>
              <label className="text-xs text-gray-500 mb-0.5 block">İçerik</label>
              <input type="number" min="1" value={editForm.contentQty}
                onChange={e => setEditForm(f => ({ ...f, contentQty: e.target.value }))}
                className="w-full px-2 py-1.5 border border-gray-300 rounded text-sm focus:outline-none focus:ring-1 focus:ring-blue-500" />
            </div>
            <div>
              <label className="text-xs text-gray-500 mb-0.5 block">Fiyat (₺)</label>
              <input type="number" min="0" step="0.01" value={editForm.price}
                onChange={e => setEditForm(f => ({ ...f, price: e.target.value }))}
                className="w-full px-2 py-1.5 border border-gray-300 rounded text-sm focus:outline-none focus:ring-1 focus:ring-blue-500" />
            </div>
            <div>
              <label className="text-xs text-gray-500 mb-0.5 block">Sıra</label>
              <input type="number" min="0" value={editForm.sortOrder}
                onChange={e => setEditForm(f => ({ ...f, sortOrder: e.target.value }))}
                className="w-full px-2 py-1.5 border border-gray-300 rounded text-sm focus:outline-none focus:ring-1 focus:ring-blue-500" />
            </div>
          </div>
          <div className="flex gap-2">
            <button onClick={() => updateConfig.mutate()} disabled={updateConfig.isPending}
              className="px-3 py-1.5 bg-blue-600 text-white rounded text-xs hover:bg-blue-700 disabled:opacity-50">
              {updateConfig.isPending ? 'Kaydediliyor…' : 'Kaydet'}
            </button>
            <button onClick={() => setEditing(false)}
              className="px-3 py-1.5 border border-gray-300 rounded text-xs hover:bg-gray-50">İptal</button>
          </div>
        </div>
      ) : (
        <div className="flex items-center gap-3">
          <div className="flex-1 flex items-center gap-2 flex-wrap">
            <span className={`text-xs font-semibold px-2 py-0.5 rounded ${config.isActive ? 'bg-gray-100 text-gray-700' : 'bg-orange-100 text-orange-700 line-through'}`}>
              {config.unitType}
            </span>
            {!config.isActive && (
              <span className="text-xs text-orange-500 font-medium">Pasif</span>
            )}
            {config.contentQty > 1 && (
              <span className="text-xs text-gray-500">{config.contentQty} adet içerir</span>
            )}
            <span className={`text-sm font-medium ${config.isActive ? 'text-gray-900' : 'text-gray-400'}`}>
              ₺{config.price.toFixed(2)}
            </span>
          </div>
          <button
            onClick={() => toggleActive.mutate()}
            disabled={toggleActive.isPending}
            title={config.isActive ? 'Satışı durdur' : 'Satışa aç'}
            className={`text-xs px-2 py-1 rounded transition-colors disabled:opacity-50 ${
              config.isActive
                ? 'text-orange-500 hover:text-orange-700 hover:bg-orange-50'
                : 'text-green-600 hover:text-green-800 hover:bg-green-50'
            }`}
          >
            {config.isActive ? '⏸ Durdur' : '▶ Aç'}
          </button>
          <button onClick={() => setEditing(true)}
            className="text-xs text-blue-600 hover:text-blue-800 px-2 py-1 hover:bg-blue-50 rounded">Düzenle</button>
          <button onClick={() => setConfirmDelete(true)}
            className="text-xs text-red-500 hover:text-red-700 px-2 py-1 hover:bg-red-50 rounded">Sil</button>
        </div>
      )}

      {/* Barkodlar */}
      <div className="pl-1">
        {config.barcodes.length > 0 && (
          <div className="flex flex-wrap gap-1.5 mb-1.5">
            {config.barcodes.map(b => (
              <div key={b.id} className="flex items-center gap-1 bg-gray-50 border border-gray-200 rounded px-2 py-0.5">
                <span className="text-xs font-mono text-gray-600">{b.barcode}</span>
                {b.note && <span className="text-xs text-gray-400">({b.note})</span>}
                <button onClick={() => deleteBarcode.mutate(b.id)}
                  className="text-gray-300 hover:text-red-500 text-xs ml-0.5">✕</button>
              </div>
            ))}
          </div>
        )}
        <div className="flex gap-1.5">
          <input
            value={newBarcode}
            onChange={e => setNewBarcode(e.target.value)}
            onKeyDown={e => e.key === 'Enter' && newBarcode.trim() && addBarcode.mutate()}
            placeholder="Barkod ekle…"
            className="flex-1 px-2 py-1 border border-gray-200 rounded text-xs focus:outline-none focus:ring-1 focus:ring-blue-400"
          />
          <button
            onClick={() => addBarcode.mutate()}
            disabled={!newBarcode.trim() || addBarcode.isPending}
            className="px-2 py-1 bg-gray-100 hover:bg-gray-200 rounded text-xs disabled:opacity-50"
          >+ Ekle</button>
        </div>
      </div>
    </div>
  )
}

// ─── Product Modal ────────────────────────────────────────────────────────────

interface ProductModalProps {
  product: Product | null
  categories: Category[]
  profileId: string
  onClose: () => void
}

function ProductModal({ product, categories, profileId, onClose }: ProductModalProps) {
  const qc = useQueryClient()
  const isEdit = !!product
  const [localProduct, setLocalProduct] = useState<Product | null>(product)

  const [form, setForm] = useState<ProductForm>(() => localProduct
    ? {
        name: localProduct.name,
        description: localProduct.description ?? '',
        price: localProduct.price.toString(),
        vatRate: localProduct.vatRate.toString(),
        minOrderQty: localProduct.minOrderQty.toString(),
        stock: localProduct.stock.toString(),
        categoryId: localProduct.categoryId,
        brand: localProduct.brand ?? '',
        manufacturer: localProduct.manufacturer ?? '',
        minimumStockLevel: localProduct.minimumStockLevel != null
          ? localProduct.minimumStockLevel.toString()
          : '',
      }
    : emptyForm()
  )
  const [isActive, setIsActive] = useState(localProduct?.isActive ?? true)
  const [isDirty, setIsDirty] = useState(false)
  const [showCloseConfirm, setShowCloseConfirm] = useState(false)

  // New unit config form
  const [showNewConfig, setShowNewConfig] = useState(false)
  const [newConfig, setNewConfig] = useState({ unitType: 'Adet', contentQty: '1', price: '', sortOrder: '0', barcode: '' })

  const set = (k: keyof ProductForm) => (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>) => {
    setForm(f => ({ ...f, [k]: e.target.value }))
    setIsDirty(true)
  }

  const handleClose = () => {
    if (isDirty) setShowCloseConfirm(true)
    else onClose()
  }

  // ESC key
  useEffect(() => {
    const handler = (e: KeyboardEvent) => { if (e.key === 'Escape') handleClose() }
    document.addEventListener('keydown', handler)
    return () => document.removeEventListener('keydown', handler)
  }, [isDirty])

  const refreshProduct = useCallback(async () => {
    if (!localProduct) return
    const updated = await productsApi.getById(localProduct.id)
    setLocalProduct(updated)
  }, [localProduct])

  const saveMutation = useMutation({
    mutationFn: async (f: ProductForm) => {
      const payload = {
        name: f.name,
        description: f.description || undefined,
        price: parseFloat(f.price),
        vatRate: parseInt(f.vatRate) || 18,
        minOrderQty: parseInt(f.minOrderQty),
        stock: parseInt(f.stock),
        categoryId: f.categoryId,
        brand: f.brand || undefined,
        manufacturer: f.manufacturer || undefined,
        // -1 = sentinel: backend'de null'a çevirir (alarmı kaldırır)
        minimumStockLevel: f.minimumStockLevel !== '' ? parseInt(f.minimumStockLevel) : -1,
        isActive,
      }
      if (isEdit) return productsApi.update(localProduct!.id, payload)
      return productsApi.create({ ...payload, wholesalerId: profileId })
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['my-products'] })
      setIsDirty(false)
      onClose()
    }
  })

  const toggleActiveMutation = useMutation({
    mutationFn: (val: boolean) => productsApi.update(localProduct!.id, { isActive: val }),
    onSuccess: (_, val) => {
      setIsActive(val)
      qc.invalidateQueries({ queryKey: ['my-products'] })
    }
  })

  const addConfigMutation = useMutation({
    mutationFn: () => productsApi.addUnitConfig(localProduct!.id, {
      unitType: newConfig.unitType,
      contentQty: parseInt(newConfig.contentQty) || 1,
      price: parseFloat(newConfig.price) || 0,
      sortOrder: parseInt(newConfig.sortOrder) || 0,
      barcodes: newConfig.barcode.trim() ? [newConfig.barcode.trim()] : [],
    }),
    onSuccess: () => {
      setShowNewConfig(false)
      setNewConfig({ unitType: 'Adet', contentQty: '1', price: '', sortOrder: '0', barcode: '' })
      qc.invalidateQueries({ queryKey: ['my-products'] })
      refreshProduct()
    }
  })

  const valid = form.name.trim() && form.price && parseFloat(form.price) > 0 && form.categoryId

  return (
    <div className="fixed inset-0 z-40 flex items-center justify-center p-4">
      {/* Backdrop */}
      <div className="absolute inset-0 bg-black/40" onClick={handleClose} />

      {/* Modal */}
      <div
        className="relative bg-white rounded-2xl shadow-2xl flex flex-col overflow-hidden"
        style={{ width: '70vw', maxWidth: '900px', height: '80vh' }}
        onClick={e => e.stopPropagation()}
      >
        {/* Header */}
        <div className="flex items-center justify-between px-6 py-4 border-b border-gray-200 shrink-0">
          <div className="flex items-center gap-3">
            <h2 className="text-base font-semibold text-gray-900">
              {isEdit ? 'Ürünü Düzenle' : 'Yeni Ürün Ekle'}
            </h2>
            {isEdit && (
              <button
                onClick={() => toggleActiveMutation.mutate(!isActive)}
                disabled={toggleActiveMutation.isPending}
                className={`px-2.5 py-0.5 rounded-full text-xs font-medium transition-colors ${
                  isActive
                    ? 'bg-green-100 text-green-700 hover:bg-green-200'
                    : 'bg-gray-100 text-gray-500 hover:bg-gray-200'
                }`}
              >
                {isActive ? 'Aktif' : 'Pasif'}
              </button>
            )}
            {!isEdit && (
              <label className="flex items-center gap-1.5 cursor-pointer">
                <input type="checkbox" checked={isActive} onChange={e => setIsActive(e.target.checked)}
                  className="rounded" />
                <span className="text-xs text-gray-600">Aktif olarak başlat</span>
              </label>
            )}
          </div>
          <button onClick={handleClose} className="text-gray-400 hover:text-gray-600 text-xl font-light">✕</button>
        </div>

        {/* Body — scrollable */}
        <div className="flex-1 overflow-y-auto px-6 py-5">
          <div className="grid grid-cols-2 gap-6">
            {/* LEFT: product info */}
            <div className="space-y-5">
              {/* Ürün bilgileri */}
              <section>
                <h3 className="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-3">Ürün Bilgileri</h3>
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
                  <div className="grid grid-cols-2 gap-3">
                    <div>
                      <label className="block text-xs font-medium text-gray-600 mb-1">Marka</label>
                      <input value={form.brand} onChange={set('brand')} placeholder="Ülker, Pınar…"
                        className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
                    </div>
                    <div>
                      <label className="block text-xs font-medium text-gray-600 mb-1">Üretici</label>
                      <input value={form.manufacturer} onChange={set('manufacturer')} placeholder="Türkiye"
                        className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
                    </div>
                  </div>
                </div>
              </section>

              {/* Fiyat & Stok */}
              <section>
                <h3 className="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-3">Fiyat & Stok</h3>
                <div className="grid grid-cols-2 gap-3 mb-3">
                  <div>
                    <label className="block text-xs font-medium text-gray-600 mb-1">Ref. Fiyat (₺) *</label>
                    <input type="number" min="0" step="0.01" value={form.price} onChange={set('price')}
                      className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
                  </div>
                  <div>
                    <label className="block text-xs font-medium text-gray-600 mb-1">KDV Oranı</label>
                    <select value={form.vatRate} onChange={set('vatRate')}
                      className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500">
                      {VAT_RATES.map(r => <option key={r} value={r}>%{r}</option>)}
                    </select>
                  </div>
                </div>
                <div className="grid grid-cols-2 gap-3">
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
                <div>
                  <label className="block text-xs font-medium text-gray-600 mb-1">
                    Minimum Stok Alarmı
                    <span className="ml-1 text-gray-400 font-normal">(boş = alarm yok)</span>
                  </label>
                  <input
                    type="number"
                    min="0"
                    value={form.minimumStockLevel}
                    onChange={set('minimumStockLevel')}
                    placeholder="Örn: 50"
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-amber-500"
                  />
                  <p className="text-xs text-gray-400 mt-1">
                    Stok bu seviyenin altına düşünce Telegram'a alarm gönderilir.
                  </p>
                </div>
              </section>

              {/* Görseller (edit only) */}
              {isEdit && localProduct && (
                <section>
                  <h3 className="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-3">Görseller</h3>
                  <ImageManager product={localProduct} onRefresh={refreshProduct} />
                </section>
              )}
            </div>

            {/* RIGHT: unit configs */}
            <div className="space-y-3">
              <div className="flex items-center justify-between">
                <h3 className="text-xs font-semibold text-gray-500 uppercase tracking-wide">Birim Konfigürasyonları</h3>
                {isEdit && (
                  <button onClick={() => setShowNewConfig(s => !s)}
                    className="text-xs text-blue-600 hover:text-blue-800 font-medium">
                    {showNewConfig ? 'İptal' : '+ Birim Ekle'}
                  </button>
                )}
              </div>

              {!isEdit && (
                <div className="text-xs text-gray-400 bg-gray-50 border border-gray-200 rounded-lg p-3">
                  Ürün kaydedildikten sonra birim konfigürasyonları eklenebilir.
                </div>
              )}

              {isEdit && localProduct && (
                <>
                  {localProduct.unitConfigs.length === 0 && !showNewConfig && (
                    <div className="text-xs text-gray-400 bg-gray-50 border border-gray-200 rounded-lg p-3">
                      Henüz birim konfigürasyonu yok. "+ Birim Ekle" ile başlayın.
                    </div>
                  )}

                  {localProduct.unitConfigs
                    .slice()
                    .sort((a, b) => a.sortOrder - b.sortOrder)
                    .map(cfg => (
                      <UnitConfigRow
                        key={cfg.id}
                        productId={localProduct.id}
                        config={cfg}
                        onChanged={refreshProduct}
                      />
                    ))}

                  {showNewConfig && (
                    <div className="border border-blue-200 bg-blue-50 rounded-lg p-3 space-y-2">
                      <p className="text-xs font-medium text-blue-700">Yeni Birim</p>
                      <div className="grid grid-cols-4 gap-2">
                        <div>
                          <label className="text-xs text-gray-500 mb-0.5 block">Birim</label>
                          <select value={newConfig.unitType}
                            onChange={e => setNewConfig(f => ({ ...f, unitType: e.target.value }))}
                            className="w-full px-2 py-1.5 border border-gray-300 rounded text-sm focus:outline-none focus:ring-1 focus:ring-blue-500 bg-white">
                            {UNIT_TYPES.map(u => <option key={u}>{u}</option>)}
                          </select>
                        </div>
                        <div>
                          <label className="text-xs text-gray-500 mb-0.5 block">İçerik</label>
                          <input type="number" min="1" value={newConfig.contentQty}
                            onChange={e => setNewConfig(f => ({ ...f, contentQty: e.target.value }))}
                            className="w-full px-2 py-1.5 border border-gray-300 rounded text-sm focus:outline-none focus:ring-1 focus:ring-blue-500" />
                        </div>
                        <div>
                          <label className="text-xs text-gray-500 mb-0.5 block">Fiyat (₺)</label>
                          <input type="number" min="0" step="0.01" value={newConfig.price}
                            onChange={e => setNewConfig(f => ({ ...f, price: e.target.value }))}
                            className="w-full px-2 py-1.5 border border-gray-300 rounded text-sm focus:outline-none focus:ring-1 focus:ring-blue-500" />
                        </div>
                        <div>
                          <label className="text-xs text-gray-500 mb-0.5 block">Sıra</label>
                          <input type="number" min="0" value={newConfig.sortOrder}
                            onChange={e => setNewConfig(f => ({ ...f, sortOrder: e.target.value }))}
                            className="w-full px-2 py-1.5 border border-gray-300 rounded text-sm focus:outline-none focus:ring-1 focus:ring-blue-500" />
                        </div>
                      </div>
                      <div>
                        <label className="text-xs text-gray-500 mb-0.5 block">Barkod <span className="text-red-500">*</span></label>
                        <input
                          value={newConfig.barcode}
                          onChange={e => setNewConfig(f => ({ ...f, barcode: e.target.value }))}
                          onKeyDown={e => e.key === 'Enter' && newConfig.price && newConfig.barcode.trim() && addConfigMutation.mutate()}
                          placeholder="Zorunlu — tarayıcı ile okutun veya elle girin"
                          className={`w-full px-2 py-1.5 border rounded text-sm focus:outline-none focus:ring-1 focus:ring-blue-500 ${!newConfig.barcode.trim() ? 'border-red-300 bg-red-50' : 'border-gray-300'}`}
                        />
                        {!newConfig.barcode.trim() && (
                          <p className="text-xs text-red-500 mt-0.5">Barkod zorunludur — barkod olmadan sipariş verilirken ürün bulunamaz.</p>
                        )}
                      </div>
                      <div className="flex gap-2">
                        <button onClick={() => addConfigMutation.mutate()}
                          disabled={!newConfig.price || !newConfig.barcode.trim() || addConfigMutation.isPending}
                          className="px-3 py-1.5 bg-blue-600 text-white rounded text-xs hover:bg-blue-700 disabled:opacity-50">
                          {addConfigMutation.isPending ? 'Ekleniyor…' : 'Ekle'}
                        </button>
                        <button onClick={() => setShowNewConfig(false)}
                          className="px-3 py-1.5 border border-gray-300 rounded text-xs hover:bg-gray-50">İptal</button>
                      </div>
                    </div>
                  )}
                </>
              )}
            </div>
          </div>
        </div>

        {/* Footer */}
        <div className="px-6 py-4 border-t border-gray-200 flex gap-2 justify-end shrink-0">
          <button onClick={handleClose}
            className="px-4 py-2 border border-gray-300 text-gray-700 rounded-lg text-sm hover:bg-gray-50 transition-colors">
            {isDirty ? 'Vazgeç' : 'Kapat'}
          </button>
          <button
            onClick={() => saveMutation.mutate(form)}
            disabled={!valid || saveMutation.isPending}
            className="px-5 py-2 bg-blue-600 text-white rounded-lg text-sm font-medium hover:bg-blue-700 disabled:opacity-50 transition-colors"
          >
            {saveMutation.isPending ? 'Kaydediliyor…' : isEdit ? 'Güncelle' : 'Oluştur'}
          </button>
        </div>
      </div>

      {/* Dirty-state close confirmation */}
      {showCloseConfirm && (
        <div className="absolute inset-0 z-50 flex items-center justify-center">
          <div className="bg-white rounded-xl shadow-2xl p-6 max-w-sm w-full mx-4">
            <h3 className="text-base font-semibold text-gray-900 mb-2">Kaydedilmemiş değişiklikler</h3>
            <p className="text-sm text-gray-500 mb-4">Yaptığınız değişiklikler kaybolacak. Çıkmak istiyor musunuz?</p>
            <div className="flex gap-2 justify-end">
              <button onClick={() => setShowCloseConfirm(false)}
                className="px-4 py-2 border border-gray-300 text-gray-700 rounded-lg text-sm hover:bg-gray-50">
                Hayır, devam et
              </button>
              <button onClick={onClose}
                className="px-4 py-2 bg-red-600 text-white rounded-lg text-sm hover:bg-red-700">
                Evet, çık
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}

// ─── Main page ────────────────────────────────────────────────────────────────

export default function WholesalerProducts() {
  const qc = useQueryClient()
  const profileId = useAuthStore(s => s.profileId)

  const [selectedCategoryId, setSelectedCategoryId] = useState<string | null>(null)
  const [search, setSearch] = useState('')
  const [filterActive, setFilterActive] = useState<'all' | 'active' | 'passive'>('all')
  const [modal, setModal] = useState<{ open: boolean; product: Product | null }>({ open: false, product: null })
  const [stockHistoryProduct, setStockHistoryProduct] = useState<Product | null>(null)
  const [editingStock, setEditingStock] = useState<{ id: string; value: string } | null>(null)
  const [editingPrice, setEditingPrice] = useState<{ productId: string; configId: string; value: string } | null>(null)

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

  const updateConfigPrice = useMutation({
    mutationFn: ({ productId, configId, price }: { productId: string; configId: string; price: number }) =>
      productsApi.updateUnitConfig(productId, configId, { price }),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['my-products'] }); setEditingPrice(null) }
  })

  const countByCategory = (catId: string) => allProducts.filter(p => p.categoryId === catId).length

  const filtered = allProducts.filter(p => {
    if (selectedCategoryId && p.categoryId !== selectedCategoryId) return false
    if (filterActive === 'active' && !p.isActive) return false
    if (filterActive === 'passive' && p.isActive) return false
    if (search) {
      const q = search.toLowerCase()
      const inName = p.name.toLowerCase().includes(q)
      const inBrand = p.brand?.toLowerCase().includes(q)
      const inManufacturer = p.manufacturer?.toLowerCase().includes(q)
      const inBarcode = p.unitConfigs.some(uc => uc.barcodes.some(b => b.barcode.includes(search)))
      if (!inName && !inBrand && !inManufacturer && !inBarcode) return false
    }
    return true
  })

  return (
    <Layout>
      {/* Header */}
      <div className="flex items-center justify-between mb-4">
        <h1 className="text-xl font-bold text-gray-900">Ürünlerim</h1>
        <button
          onClick={() => setModal({ open: true, product: null })}
          className="px-4 py-2 bg-blue-600 text-white rounded-lg text-sm font-medium hover:bg-blue-700 transition-colors"
        >
          + Yeni Ürün
        </button>
      </div>

      {/* Kategori filtreleri */}
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

      {/* Arama + Durum filtresi */}
      <div className="flex gap-3 mb-4">
        <div className="relative flex-1">
          <span className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400 text-sm">🔍</span>
          <input
            value={search}
            onChange={e => setSearch(e.target.value)}
            placeholder="Ürün adı, marka, üretici veya barkod…"
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

      {/* Tablo */}
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
                  <th className="text-left px-4 py-3 text-xs font-medium text-gray-500 hidden lg:table-cell">Birimler</th>
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
                              {(p.brand || p.manufacturer) && (
                                <span className="text-xs text-gray-400">
                                  {[p.brand, p.manufacturer].filter(Boolean).join(' · ')}
                                </span>
                              )}
                              <span className="text-xs text-purple-600 bg-purple-50 border border-purple-100 rounded px-1.5 py-0.5">KDV %{p.vatRate}</span>
                            </div>
                          </div>
                        </div>
                      </td>

                      {/* Kategori */}
                      <td className="px-4 py-3 text-gray-500 hidden md:table-cell">{p.categoryName}</td>

                      {/* Birim konfigürasyonları — inline fiyat düzenleme */}
                      <td className="px-4 py-3 hidden lg:table-cell">
                        <div className="flex flex-wrap gap-1">
                          {p.unitConfigs
                            .slice()
                            .sort((a, b) => a.sortOrder - b.sortOrder)
                            .map(uc => {
                              const isEditingThis = editingPrice?.productId === p.id && editingPrice?.configId === uc.id
                              if (isEditingThis) {
                                return (
                                  <div key={uc.id} className="flex items-center gap-0.5">
                                    <span className="text-xs text-gray-500">{uc.unitType} ₺</span>
                                    <input
                                      type="number" min="0" step="0.01"
                                      value={editingPrice.value}
                                      onChange={e => setEditingPrice(s => s && ({ ...s, value: e.target.value }))}
                                      onKeyDown={e => {
                                        if (e.key === 'Enter') updateConfigPrice.mutate({ productId: p.id, configId: uc.id, price: parseFloat(editingPrice.value) || 0 })
                                        if (e.key === 'Escape') setEditingPrice(null)
                                      }}
                                      autoFocus
                                      className="w-16 px-1 py-0.5 border border-blue-400 rounded text-xs text-right focus:outline-none"
                                    />
                                    <button onClick={() => updateConfigPrice.mutate({ productId: p.id, configId: uc.id, price: parseFloat(editingPrice.value) || 0 })}
                                      className="text-green-600 text-xs hover:text-green-700">✓</button>
                                    <button onClick={() => setEditingPrice(null)}
                                      className="text-gray-400 text-xs hover:text-gray-600">✕</button>
                                  </div>
                                )
                              }
                              return (
                                <button key={uc.id}
                                  onClick={() => setEditingPrice({ productId: p.id, configId: uc.id, value: uc.price.toFixed(2) })}
                                  title="Fiyatı düzenle"
                                  className="text-xs bg-blue-50 text-blue-700 border border-blue-100 rounded px-1.5 py-0.5 hover:bg-blue-100 transition-colors">
                                  {uc.unitType} ₺{uc.price.toFixed(2)}
                                </button>
                              )
                            })}
                          {p.unitConfigs.length === 0 && (
                            <span className="text-xs text-gray-300">—</span>
                          )}
                        </div>
                      </td>

                      {/* Stok — inline edit */}
                      <td className="px-4 py-3 text-center">
                        {editingStock?.id === p.id ? (
                          <div className="flex items-center gap-1 justify-center">
                            <input
                              type="number" min="0"
                              value={editingStock.value}
                              onChange={e => setEditingStock(s => s && ({ ...s, value: e.target.value }))}
                              onKeyDown={e => {
                                if (e.key === 'Enter') updateStock.mutate({ id: p.id, stock: parseInt(editingStock.value) || 0 })
                                if (e.key === 'Escape') setEditingStock(null)
                              }}
                              autoFocus
                              className="w-16 px-2 py-1 border border-blue-400 rounded text-sm text-center focus:outline-none"
                            />
                            <button onClick={() => updateStock.mutate({ id: p.id, stock: parseInt(editingStock.value) || 0 })}
                              className="text-green-600 text-xs hover:text-green-700">✓</button>
                            <button onClick={() => setEditingStock(null)}
                              className="text-gray-400 text-xs hover:text-gray-600">✕</button>
                          </div>
                        ) : (
                          <button
                            onClick={() => setEditingStock({ id: p.id, value: p.stock.toString() })}
                            className="hover:bg-gray-100 rounded px-1" title="Stoku düzenle"
                          >
                            <StockBadge stock={p.stock} minimumStockLevel={p.minimumStockLevel} />
                          </button>
                        )}
                      </td>

                      {/* Min */}
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

                      {/* Aksiyonlar */}
                      <td className="px-4 py-3 text-right">
                        <div className="flex items-center justify-end gap-1">
                          <button
                            onClick={() => setStockHistoryProduct(p)}
                            className="px-2 py-1.5 text-xs text-gray-500 hover:bg-gray-100 rounded-lg transition-colors"
                            title="Stok Hareketleri"
                          >
                            📊
                          </button>
                          <button
                            onClick={() => setModal({ open: true, product: p })}
                            className="px-3 py-1.5 text-xs text-blue-600 hover:bg-blue-50 rounded-lg transition-colors font-medium"
                          >
                            Düzenle
                          </button>
                        </div>
                      </td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          )}
        </div>
      )}

      {/* Modal */}
      {modal.open && (
        <ProductModal
          product={modal.product}
          categories={categories}
          profileId={profileId ?? ''}
          onClose={() => setModal({ open: false, product: null })}
        />
      )}

      {/* Stok Geçmişi Modal */}
      {stockHistoryProduct && (
        <StockHistoryModal
          productId={stockHistoryProduct.id}
          productName={stockHistoryProduct.name}
          onClose={() => setStockHistoryProduct(null)}
        />
      )}
    </Layout>
  )
}
