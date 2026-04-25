import { useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { useQuery, useMutation } from '@tanstack/react-query'
import Layout from '../../components/Layout'
import { productsApi, categoriesApi, ordersApi, creditApi } from '../../api/client'
import type { Product, Category, CreditSummary } from '../../types'

const NAV = (wholesalerId: string) => [
  { to: `/store/${wholesalerId}`, label: 'Ürünler' },
  { to: `/store/${wholesalerId}/orders`, label: 'Siparişlerim' },
  { to: `/store/${wholesalerId}/credit`, label: 'Veresiye' },
  { to: '/store', label: '← Toptancı Seç' }
]

export default function StoreHome() {
  const { wholesalerId } = useParams<{ wholesalerId: string }>()
  const navigate = useNavigate()
  const [cart, setCart] = useState<Map<string, { product: Product; qty: number }>>(new Map())
  const [selectedCategory, setSelectedCategory] = useState<string | undefined>()
  const [orderNote, setOrderNote] = useState('')
  const [orderSuccess, setOrderSuccess] = useState(false)

  const { data: products = [], isLoading } = useQuery<Product[]>({
    queryKey: ['products', wholesalerId, selectedCategory],
    queryFn: () => productsApi.getAll({ wholesalerId, categoryId: selectedCategory }),
    enabled: !!wholesalerId
  })

  const { data: categories = [] } = useQuery<Category[]>({
    queryKey: ['categories', wholesalerId],
    queryFn: () => categoriesApi.getAll(wholesalerId),
    enabled: !!wholesalerId
  })

  const { data: credit } = useQuery<CreditSummary>({
    queryKey: ['credit', wholesalerId],
    queryFn: () => creditApi.getWholesalerCredit(wholesalerId!),
    enabled: !!wholesalerId
  })

  const addToCart = (product: Product) => {
    setCart(prev => {
      const next = new Map(prev)
      const existing = next.get(product.id)
      next.set(product.id, { product, qty: (existing?.qty ?? 0) + 1 })
      return next
    })
  }

  const cartItems = [...cart.values()]
  const cartTotal = cartItems.reduce((sum, { product, qty }) => sum + product.price * qty, 0)

  const placeOrderMutation = useMutation({
    mutationFn: () => ordersApi.create({
      note: orderNote,
      items: cartItems.map(({ product, qty }) => ({ productId: product.id, quantity: qty }))
    }),
    onSuccess: () => {
      setCart(new Map())
      setOrderNote('')
      setOrderSuccess(true)
      setTimeout(() => setOrderSuccess(false), 3000)
    },
    onError: (err: unknown) => {
      const msg = (err as { response?: { data?: { error?: string } } })?.response?.data?.error
      alert(msg ?? 'Sipariş gönderilemedi')
    }
  })

  const nav = NAV(wholesalerId ?? '')

  return (
    <Layout navLinks={nav}>
      {/* Veresiye özet şeridi */}
      {credit && credit.balance > 0 && (
        <div
          className={`mb-4 rounded-xl border px-4 py-3 flex items-center justify-between cursor-pointer hover:opacity-90 transition-opacity ${credit.overdueAmount > 0 ? 'bg-red-50 border-red-200' : 'bg-blue-50 border-blue-200'}`}
          onClick={() => navigate(`/store/${wholesalerId}/credit`)}
        >
          <div className="flex items-center gap-3">
            <span className="text-lg">{credit.overdueAmount > 0 ? '⚠️' : '💰'}</span>
            <div>
              <p className={`text-sm font-semibold ${credit.overdueAmount > 0 ? 'text-red-800' : 'text-blue-800'}`}>
                Veresiye Bakiyesi: ₺{credit.balance.toFixed(2)}
              </p>
              {credit.overdueAmount > 0 && (
                <p className="text-xs text-red-600">Vadesi geçmiş: ₺{credit.overdueAmount.toFixed(2)}</p>
              )}
            </div>
          </div>
          <span className="text-xs text-gray-400">Detay →</span>
        </div>
      )}

      <div className="flex gap-6">
        {/* Ürün listesi */}
        <div className="flex-1">
          <div className="flex gap-2 mb-4 flex-wrap">
            <button
              onClick={() => setSelectedCategory(undefined)}
              className={`px-3 py-1 rounded-full text-sm border transition-colors ${!selectedCategory ? 'bg-blue-600 text-white border-blue-600' : 'border-gray-300 text-gray-600 hover:border-gray-400'}`}
            >
              Tümü
            </button>
            {categories.map((cat: Category) => (
              <button
                key={cat.id}
                onClick={() => setSelectedCategory(cat.id)}
                className={`px-3 py-1 rounded-full text-sm border transition-colors ${selectedCategory === cat.id ? 'bg-blue-600 text-white border-blue-600' : 'border-gray-300 text-gray-600 hover:border-gray-400'}`}
              >
                {cat.name}
              </button>
            ))}
          </div>

          {isLoading ? (
            <div className="text-center py-12 text-gray-400">Yükleniyor...</div>
          ) : (
            <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4">
              {products.map((p: Product) => {
                const mainImg = p.images.find(i => i.isMain) ?? p.images[0]
                return (
                  <div key={p.id} className="bg-white rounded-xl border border-gray-200 overflow-hidden hover:shadow-md transition-shadow">
                    <div className="aspect-square bg-gray-100">
                      {mainImg
                        ? <img src={mainImg.url} alt={p.name} className="w-full h-full object-cover" />
                        : <div className="w-full h-full flex items-center justify-center text-gray-300 text-4xl">📦</div>
                      }
                    </div>
                    <div className="p-3">
                      <p className="font-medium text-sm text-gray-900 mb-1 truncate">{p.name}</p>
                      <p className="text-blue-600 font-bold text-sm mb-1">₺{p.price.toFixed(2)} / {p.unit}</p>
                      <p className="text-xs text-gray-400 mb-2">Min: {p.minOrderQty} {p.unit}</p>
                      <button
                        onClick={() => addToCart(p)}
                        disabled={p.stock === 0}
                        className="w-full py-1.5 bg-blue-600 text-white rounded-lg text-xs font-medium hover:bg-blue-700 disabled:opacity-40 transition-colors"
                      >
                        {p.stock === 0 ? 'Stok Yok' : 'Sepete Ekle'}
                      </button>
                    </div>
                  </div>
                )
              })}
            </div>
          )}
        </div>

        {/* Sepet */}
        <div className="w-72 shrink-0">
          <div className="bg-white rounded-xl border border-gray-200 p-4 sticky top-20">
            <h2 className="font-semibold text-gray-900 mb-3">Sepet {cartItems.length > 0 && `(${cartItems.length})`}</h2>
            {cartItems.length === 0 ? (
              <p className="text-sm text-gray-400">Sepet boş</p>
            ) : (
              <>
                <div className="space-y-2 mb-3 max-h-64 overflow-y-auto">
                  {cartItems.map(({ product, qty }) => (
                    <div key={product.id} className="flex justify-between items-center text-sm">
                      <span className="text-gray-700 truncate flex-1">{product.name}</span>
                      <div className="flex items-center gap-2 ml-2">
                        <button
                          onClick={() => setCart(prev => {
                            const next = new Map(prev)
                            const item = next.get(product.id)!
                            if (item.qty <= 1) next.delete(product.id)
                            else next.set(product.id, { ...item, qty: item.qty - 1 })
                            return next
                          })}
                          className="w-5 h-5 rounded bg-gray-100 text-gray-600 hover:bg-gray-200 text-xs flex items-center justify-center"
                        >−</button>
                        <span className="w-6 text-center font-medium">{qty}</span>
                        <button onClick={() => addToCart(product)} className="w-5 h-5 rounded bg-gray-100 text-gray-600 hover:bg-gray-200 text-xs flex items-center justify-center">+</button>
                      </div>
                    </div>
                  ))}
                </div>
                <div className="border-t border-gray-100 pt-3 mb-3">
                  <div className="flex justify-between text-sm font-semibold">
                    <span>Toplam</span>
                    <span className="text-blue-600">₺{cartTotal.toFixed(2)}</span>
                  </div>
                </div>
                <textarea
                  value={orderNote}
                  onChange={e => setOrderNote(e.target.value)}
                  placeholder="Sipariş notu (isteğe bağlı)"
                  className="w-full text-sm border border-gray-200 rounded-lg px-2 py-1.5 mb-3 resize-none h-16 focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
                <button
                  onClick={() => placeOrderMutation.mutate()}
                  disabled={placeOrderMutation.isPending}
                  className="w-full py-2 bg-blue-600 text-white rounded-lg text-sm font-medium hover:bg-blue-700 disabled:opacity-50 transition-colors"
                >
                  {placeOrderMutation.isPending ? 'Gönderiliyor...' : 'Sipariş Ver'}
                </button>
                {orderSuccess && <div className="mt-2 text-center text-xs text-green-600 font-medium">✓ Sipariş gönderildi</div>}
              </>
            )}
          </div>
        </div>
      </div>
    </Layout>
  )
}
