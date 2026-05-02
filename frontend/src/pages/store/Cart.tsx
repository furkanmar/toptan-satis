import { useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { useMutation } from '@tanstack/react-query'
import Layout from '../../components/Layout'
import { ordersApi } from '../../api/client'
import { useCartStore } from '../../store/cartStore'

const NAV = (wholesalerId: string) => [
  { to: `/store/${wholesalerId}`, label: 'Ürünler' },
  { to: `/store/${wholesalerId}/orders`, label: 'Siparişlerim' },
  { to: `/store/${wholesalerId}/credit`, label: 'Veresiye' },
  { to: `/store/${wholesalerId}/cart`, label: 'Sepetim' },
  { to: '/store', label: '← Toptancı Seç' },
]

export default function StoreCart() {
  const { wholesalerId } = useParams<{ wholesalerId: string }>()
  const navigate = useNavigate()

  const { items, removeItem, updateQty, clearCart } = useCartStore()
  const [orderNote, setOrderNote] = useState('')
  const [orderSuccess, setOrderSuccess] = useState(false)

  const cartEntries = Object.entries(items)
  const cartTotal   = cartEntries.reduce((s, [, it]) => s + it.unitPrice * it.qty, 0)
  const vatBreakdown = cartEntries.reduce<Record<number, number>>((acc, [, it]) => {
    const rate = it.product.vatRate ?? 0
    acc[rate] = (acc[rate] ?? 0) + it.unitPrice * it.qty
    return acc
  }, {})

  const placeOrder = useMutation({
    mutationFn: () => ordersApi.create({
      note: orderNote,
      items: cartEntries.map(([, it]) => ({
        productId: it.product.id,
        unitConfigId: it.unitConfigId,
        quantity: it.qty,
      })),
    }),
    onSuccess: () => {
      clearCart()
      setOrderNote('')
      setOrderSuccess(true)
    },
    onError: (err: unknown) => {
      alert(
        (err as { response?: { data?: { error?: string } } })?.response?.data?.error ??
        'Sipariş gönderilemedi'
      )
    },
  })

  if (orderSuccess) {
    return (
      <Layout navLinks={NAV(wholesalerId ?? '')}>
        <div className="max-w-lg mx-auto py-16 text-center">
          <div className="text-5xl mb-4">✅</div>
          <h1 className="text-2xl font-bold text-gray-900 mb-2">Sipariş Gönderildi!</h1>
          <p className="text-gray-500 mb-8">Toptancı siparişinizi inceleyecek ve onaylayacak.</p>
          <div className="flex gap-3 justify-center">
            <button
              onClick={() => navigate(`/store/${wholesalerId}`)}
              className="px-5 py-2.5 bg-blue-600 text-white rounded-lg text-sm font-semibold hover:bg-blue-700 transition-colors"
            >
              Alışverişe Devam
            </button>
            <button
              onClick={() => navigate(`/store/${wholesalerId}/orders`)}
              className="px-5 py-2.5 border border-gray-300 text-gray-700 rounded-lg text-sm font-semibold hover:bg-gray-50 transition-colors"
            >
              Siparişlerim
            </button>
          </div>
        </div>
      </Layout>
    )
  }

  return (
    <Layout navLinks={NAV(wholesalerId ?? '')}>
      {/* Başlık */}
      <div className="flex items-center gap-4 mb-6">
        <button
          onClick={() => navigate(`/store/${wholesalerId}`)}
          className="text-sm text-gray-500 hover:text-gray-800 transition-colors"
        >
          ← Alışverişe Dön
        </button>
        <h1 className="text-xl font-bold text-gray-900">
          Sepetim
          {cartEntries.length > 0 && (
            <span className="ml-2 text-sm font-normal text-gray-500">({cartEntries.length} kalem)</span>
          )}
        </h1>
      </div>

      {cartEntries.length === 0 ? (
        <div className="text-center py-20">
          <div className="text-4xl mb-4">🛒</div>
          <p className="text-gray-500 mb-4">Sepetiniz boş</p>
          <button
            onClick={() => navigate(`/store/${wholesalerId}`)}
            className="px-5 py-2.5 bg-blue-600 text-white rounded-lg text-sm font-semibold hover:bg-blue-700 transition-colors"
          >
            Ürünlere Göz At
          </button>
        </div>
      ) : (
        <div className="flex gap-6 items-start">
          {/* Ürünler listesi */}
          <div className="flex-1 min-w-0">
            <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
              {/* Tablo başlığı */}
              <div className="grid grid-cols-12 gap-3 px-4 py-3 bg-gray-50 border-b border-gray-200 text-xs font-semibold text-gray-500 uppercase tracking-wide">
                <div className="col-span-5">Ürün</div>
                <div className="col-span-2 text-center">Birim Fiyat</div>
                <div className="col-span-3 text-center">Miktar</div>
                <div className="col-span-2 text-right">Tutar</div>
              </div>

              {/* Ürün satırları */}
              <div className="divide-y divide-gray-100">
                {cartEntries.map(([key, it]) => (
                  <div key={key} className="grid grid-cols-12 gap-3 px-4 py-4 items-center">
                    {/* Ürün bilgisi */}
                    <div className="col-span-5">
                      <p className="text-sm font-medium text-gray-900">{it.product.name}</p>
                      <div className="flex items-center gap-2 mt-0.5">
                        <span className="text-xs text-gray-500">{it.unitType}</span>
                        {it.contentQty > 1 && (
                          <span className="text-xs text-gray-400">×{it.contentQty} adet</span>
                        )}
                        {it.product.vatRate > 0 && (
                          <span className="text-xs text-purple-600 bg-purple-50 rounded px-1.5 py-0.5">
                            KDV %{it.product.vatRate}
                          </span>
                        )}
                      </div>
                    </div>

                    {/* Birim fiyat */}
                    <div className="col-span-2 text-center">
                      <span className="text-sm text-gray-700">₺{it.unitPrice.toFixed(2)}</span>
                    </div>

                    {/* Miktar düzenleyici */}
                    <div className="col-span-3 flex items-center justify-center gap-1">
                      <button
                        onClick={() => updateQty(key, it.qty - 1)}
                        className="w-7 h-7 rounded bg-gray-100 hover:bg-gray-200 text-gray-600 text-sm flex items-center justify-center transition-colors"
                      >−</button>
                      <input
                        type="number"
                        min={1}
                        value={it.qty}
                        onChange={e => {
                          const v = parseInt(e.target.value)
                          if (!isNaN(v) && v > 0) updateQty(key, v)
                        }}
                        className="w-14 text-center text-sm border border-gray-200 rounded-lg py-1 focus:outline-none focus:ring-2 focus:ring-blue-400"
                      />
                      <button
                        onClick={() => updateQty(key, it.qty + 1)}
                        className="w-7 h-7 rounded bg-gray-100 hover:bg-gray-200 text-gray-600 text-sm flex items-center justify-center transition-colors"
                      >+</button>
                    </div>

                    {/* Tutar + sil */}
                    <div className="col-span-2 flex items-center justify-end gap-2">
                      <span className="text-sm font-semibold text-gray-800">
                        ₺{(it.unitPrice * it.qty).toFixed(2)}
                      </span>
                      <button
                        onClick={() => removeItem(key)}
                        className="text-gray-300 hover:text-red-400 transition-colors text-xs"
                        title="Kaldır"
                      >✕</button>
                    </div>
                  </div>
                ))}
              </div>

              {/* Alt kısım — temizle */}
              <div className="px-4 py-3 bg-gray-50 border-t border-gray-100 flex justify-end">
                <button
                  onClick={clearCart}
                  className="text-xs text-gray-400 hover:text-red-500 transition-colors"
                >
                  Sepeti Temizle
                </button>
              </div>
            </div>
          </div>

          {/* Sipariş özeti + onay */}
          <div className="w-72 shrink-0">
            <div className="bg-white rounded-xl border border-gray-200 p-5 space-y-4">
              <h2 className="font-semibold text-gray-900">Sipariş Özeti</h2>

              {/* KDV dökümü */}
              {Object.entries(vatBreakdown).map(([rate, total]) => (
                <div key={rate} className="flex justify-between text-sm text-gray-600">
                  <span>KDV %{rate} matrahı</span>
                  <span>₺{total.toFixed(2)}</span>
                </div>
              ))}

              <div className="border-t border-gray-100 pt-3 flex justify-between items-center">
                <span className="font-semibold text-gray-800">Toplam</span>
                <span className="text-xl font-bold text-blue-600">₺{cartTotal.toFixed(2)}</span>
              </div>

              {/* Sipariş notu */}
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">
                  Sipariş Notu <span className="font-normal text-gray-400">(isteğe bağlı)</span>
                </label>
                <textarea
                  value={orderNote}
                  onChange={e => setOrderNote(e.target.value)}
                  placeholder="Toptancıya iletmek istediğiniz notlar…"
                  rows={3}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 resize-none"
                />
              </div>

              <button
                onClick={() => placeOrder.mutate()}
                disabled={placeOrder.isPending}
                className="w-full py-3 bg-blue-600 text-white rounded-xl text-sm font-bold hover:bg-blue-700 disabled:opacity-50 transition-colors"
              >
                {placeOrder.isPending ? 'Gönderiliyor…' : 'Siparişi Onayla'}
              </button>

              <button
                onClick={() => navigate(`/store/${wholesalerId}`)}
                className="w-full py-2 text-sm text-gray-500 hover:text-gray-800 transition-colors"
              >
                ← Alışverişe Devam Et
              </button>
            </div>
          </div>
        </div>
      )}
    </Layout>
  )
}
