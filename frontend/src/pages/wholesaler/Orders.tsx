import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import Layout from '../../components/Layout'
import { ordersApi } from '../../api/client'
import type { Order, OrderItem } from '../../types'

const NAV = [
  { to: '/wholesaler', label: 'Ana Sayfa' },
  { to: '/wholesaler/products', label: 'Ürünler' },
  { to: '/wholesaler/orders', label: 'Siparişler' },
  { to: '/wholesaler/credit', label: 'Veresiye' },
  { to: '/wholesaler/settings', label: 'Ayarlar' }
]

type Tab = 'pending' | 'confirmed' | 'history'

const STATUS_LABELS: Record<string, { label: string; color: string }> = {
  Pending:   { label: 'Bekliyor',   color: 'bg-yellow-100 text-yellow-700' },
  Confirmed: { label: 'Onaylandı',  color: 'bg-blue-100 text-blue-700' },
  Rejected:  { label: 'Reddedildi', color: 'bg-red-100 text-red-700' },
  Delivered: { label: 'Teslim',     color: 'bg-green-100 text-green-700' },
  Cancelled: { label: 'İptal',      color: 'bg-gray-100 text-gray-600' }
}

interface EditItem { productId: string; productName: string; quantity: number; unitPrice: number }

export default function WholesalerOrders() {
  const qc = useQueryClient()
  const [tab, setTab] = useState<Tab>('pending')
  const [confirmModal, setConfirmModal] = useState<Order | null>(null)
  const [confirmForm, setConfirmForm] = useState({ wholesalerNote: '', dueDate: '', createCreditEntry: true })
  const [expandedId, setExpandedId] = useState<string | null>(null)
  const [stockWarning, setStockWarning] = useState<{ order: Order; msg: string } | null>(null)
  const [editingOrderId, setEditingOrderId] = useState<string | null>(null)
  const [editItems, setEditItems] = useState<EditItem[]>([])
  const [editNote, setEditNote] = useState('')

  const { data: orders = [], isLoading } = useQuery<Order[]>({
    queryKey: ['incoming-orders'],
    queryFn: ordersApi.incoming,
    refetchInterval: 30_000
  })

  const pending = orders.filter(o => o.status === 'Pending')
  const confirmed = orders.filter(o => o.status === 'Confirmed')
  const history = orders.filter(o => ['Delivered', 'Rejected', 'Cancelled'].includes(o.status))
  const tabOrders: Record<Tab, Order[]> = { pending, confirmed, history }

  const confirmMutation = useMutation({
    mutationFn: ({ order, force = false }: { order: Order; force?: boolean }) =>
      ordersApi.confirm(order.id, {
        wholesalerNote: confirmForm.wholesalerNote || undefined,
        dueDate: confirmForm.dueDate || undefined,
        createCreditEntry: confirmForm.createCreditEntry,
        forceConfirm: force
      }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['incoming-orders'] })
      qc.invalidateQueries({ queryKey: ['credit-all-stores'] })
      setConfirmModal(null)
      setConfirmForm({ wholesalerNote: '', dueDate: '', createCreditEntry: true })
    },
    onError: (err: unknown, variables) => {
      const msg = (err as { response?: { data?: { error?: string } } })?.response?.data?.error ?? ''
      if (msg.includes('yeterli stok yok')) {
        setStockWarning({ order: variables.order, msg })
      }
    }
  })

  const rejectMutation = useMutation({
    mutationFn: (id: string) => ordersApi.updateStatus(id, 'Rejected'),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['incoming-orders'] })
  })

  const deliverMutation = useMutation({
    mutationFn: (id: string) => ordersApi.updateStatus(id, 'Delivered'),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['incoming-orders'] })
  })

  const updateItemsMutation = useMutation({
    mutationFn: (orderId: string) => ordersApi.updateItems(orderId, {
      items: editItems.filter(i => i.quantity > 0).map(i => ({ productId: i.productId, quantity: i.quantity })),
      wholesalerNote: editNote || undefined
    }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['incoming-orders'] })
      setEditingOrderId(null)
      setEditItems([])
      setEditNote('')
    }
  })

  const startEdit = (order: Order) => {
    setEditingOrderId(order.id)
    setEditItems(order.items.map((i: OrderItem) => ({
      productId: i.productId,
      productName: i.productName,
      quantity: i.quantity,
      unitPrice: i.unitPrice
    })))
    setEditNote(order.wholesalerNote ?? '')
  }

  const cancelEdit = () => {
    setEditingOrderId(null)
    setEditItems([])
    setEditNote('')
  }

  const tabs: { key: Tab; label: string; count?: number }[] = [
    { key: 'pending', label: 'Bekleyen', count: pending.length },
    { key: 'confirmed', label: 'Onaylı / Teslim Bekleyen', count: confirmed.length },
    { key: 'history', label: 'Geçmiş' }
  ]

  return (
    <Layout navLinks={NAV}>
      <h1 className="text-xl font-bold text-gray-900 mb-4">Siparişler</h1>

      <div className="flex gap-1 mb-4 bg-gray-100 p-1 rounded-lg w-fit">
        {tabs.map(t => (
          <button
            key={t.key}
            onClick={() => setTab(t.key)}
            className={`px-4 py-1.5 rounded-md text-sm font-medium transition-colors flex items-center gap-2 ${tab === t.key ? 'bg-white text-gray-900 shadow-sm' : 'text-gray-500 hover:text-gray-700'}`}
          >
            {t.label}
            {t.count !== undefined && t.count > 0 && (
              <span className={`text-xs px-1.5 py-0.5 rounded-full font-bold ${tab === t.key ? 'bg-blue-100 text-blue-700' : 'bg-gray-200 text-gray-600'}`}>
                {t.count}
              </span>
            )}
          </button>
        ))}
      </div>

      {isLoading ? (
        <div className="text-center py-12 text-gray-400">Yükleniyor...</div>
      ) : tabOrders[tab].length === 0 ? (
        <div className="text-center py-12 text-gray-400">Sipariş yok</div>
      ) : (
        <div className="space-y-2">
          {tabOrders[tab].map(order => {
            const { label, color } = STATUS_LABELS[order.status] ?? { label: order.status, color: 'bg-gray-100 text-gray-600' }
            const isExpanded = expandedId === order.id
            const isEditing = editingOrderId === order.id
            const isOverdue = order.dueDate && new Date(order.dueDate) < new Date()

            return (
              <div key={order.id} className="bg-white rounded-xl border border-gray-200 overflow-hidden">
                {/* Header */}
                <div
                  className="px-4 py-3 flex items-center justify-between cursor-pointer hover:bg-gray-50"
                  onClick={() => { if (!isEditing) setExpandedId(isExpanded ? null : order.id) }}
                >
                  <div className="flex items-center gap-3">
                    <span className="text-gray-400 text-xs">{isExpanded ? '▲' : '▼'}</span>
                    <div>
                      <span className="font-medium text-gray-900 text-sm">{order.storeName}</span>
                      <span className="text-xs text-gray-400 ml-2">
                        {new Date(order.createdAt).toLocaleDateString('tr-TR', { day: 'numeric', month: 'long', hour: '2-digit', minute: '2-digit' })}
                      </span>
                      {order.dueDate && (
                        <span className={`text-xs ml-2 font-medium ${isOverdue ? 'text-red-600' : 'text-gray-500'}`}>
                          Vade: {new Date(order.dueDate).toLocaleDateString('tr-TR')} {isOverdue && '⚠️'}
                        </span>
                      )}
                    </div>
                  </div>
                  <div className="flex items-center gap-3">
                    <span className="font-semibold text-gray-900 text-sm">₺{order.totalAmount.toFixed(2)}</span>
                    <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${color}`}>{label}</span>
                  </div>
                </div>

                {/* Detay */}
                {isExpanded && (
                  <div className="border-t border-gray-100 px-4 py-3">
                    {isEditing ? (
                      /* Düzenleme modu */
                      <div>
                        <p className="text-xs font-medium text-gray-500 mb-2">Ürün miktarlarını düzenle (0 → kaldır):</p>
                        <div className="space-y-2 mb-3">
                          {editItems.map((item, i) => (
                            <div key={item.productId} className="flex items-center justify-between gap-3">
                              <span className="text-sm text-gray-700 flex-1">{item.productName}</span>
                              <span className="text-xs text-gray-400">₺{item.unitPrice.toFixed(2)}/ad</span>
                              <input
                                type="number"
                                min="0"
                                value={item.quantity}
                                onChange={e => {
                                  const next = [...editItems]
                                  next[i] = { ...item, quantity: parseInt(e.target.value) || 0 }
                                  setEditItems(next)
                                }}
                                className="w-16 px-2 py-1 border border-gray-300 rounded text-sm text-center focus:outline-none focus:ring-2 focus:ring-blue-500"
                              />
                              <span className="text-xs text-gray-500 w-16 text-right">
                                ₺{(item.unitPrice * (parseInt(String(item.quantity)) || 0)).toFixed(2)}
                              </span>
                            </div>
                          ))}
                        </div>
                        <div className="mb-3">
                          <label className="block text-xs font-medium text-gray-700 mb-1">Toptancı notu</label>
                          <input
                            value={editNote}
                            onChange={e => setEditNote(e.target.value)}
                            placeholder="Değişiklik notu..."
                            className="w-full px-3 py-1.5 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                          />
                        </div>
                        <div className="text-xs text-gray-500 mb-3">
                          Yeni toplam: ₺{editItems.reduce((s, i) => s + i.unitPrice * (i.quantity || 0), 0).toFixed(2)}
                        </div>
                        <div className="flex gap-2">
                          <button
                            onClick={() => updateItemsMutation.mutate(order.id)}
                            disabled={updateItemsMutation.isPending || editItems.every(i => i.quantity <= 0)}
                            className="px-3 py-1.5 bg-blue-600 text-white rounded-lg text-xs font-medium hover:bg-blue-700 disabled:opacity-50 transition-colors"
                          >
                            {updateItemsMutation.isPending ? 'Kaydediliyor...' : 'Kaydet'}
                          </button>
                          <button
                            onClick={cancelEdit}
                            className="px-3 py-1.5 border border-gray-300 text-gray-700 rounded-lg text-xs hover:bg-gray-50 transition-colors"
                          >
                            İptal
                          </button>
                        </div>
                      </div>
                    ) : (
                      /* Normal görünüm */
                      <>
                        <div className="space-y-1 mb-3">
                          {order.items.map((item, i) => (
                            <div key={i} className="flex justify-between text-sm text-gray-600">
                              <span>{item.productName} × {item.quantity}</span>
                              <span>₺{item.total.toFixed(2)}</span>
                            </div>
                          ))}
                        </div>
                        {order.note && <p className="text-xs text-gray-500 mb-1">Mağaza notu: {order.note}</p>}
                        {order.wholesalerNote && <p className="text-xs text-blue-600 mb-1">Toptancı notu: {order.wholesalerNote}</p>}

                        <div className="flex gap-2 mt-3">
                          {order.status === 'Pending' && (
                            <>
                              <button
                                onClick={() => setConfirmModal(order)}
                                className="px-3 py-1.5 bg-blue-600 text-white rounded-lg text-xs font-medium hover:bg-blue-700 transition-colors"
                              >
                                Onayla
                              </button>
                              <button
                                onClick={() => startEdit(order)}
                                className="px-3 py-1.5 border border-blue-300 text-blue-600 rounded-lg text-xs font-medium hover:bg-blue-50 transition-colors"
                              >
                                Düzenle
                              </button>
                              <button
                                onClick={() => rejectMutation.mutate(order.id)}
                                disabled={rejectMutation.isPending}
                                className="px-3 py-1.5 border border-red-200 text-red-600 rounded-lg text-xs font-medium hover:bg-red-50 transition-colors"
                              >
                                Reddet
                              </button>
                            </>
                          )}
                          {order.status === 'Confirmed' && (
                            <button
                              onClick={() => deliverMutation.mutate(order.id)}
                              disabled={deliverMutation.isPending}
                              className="px-3 py-1.5 bg-green-600 text-white rounded-lg text-xs font-medium hover:bg-green-700 transition-colors"
                            >
                              Teslim Edildi
                            </button>
                          )}
                        </div>
                      </>
                    )}
                  </div>
                )}
              </div>
            )
          })}
        </div>
      )}

      {/* Stok uyarı modal */}
      {stockWarning && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-2xl shadow-xl w-full max-w-sm p-6">
            <div className="text-3xl mb-3 text-center">⚠️</div>
            <h2 className="font-bold text-gray-900 mb-2 text-center">Yetersiz Stok</h2>
            <p className="text-sm text-gray-600 mb-5 text-center">{stockWarning.msg}</p>
            <p className="text-sm text-gray-700 mb-5 text-center">Yine de onaylayıp stoku eksi'ye düşürmek istiyor musunuz?</p>
            <div className="flex gap-2">
              <button
                onClick={() => {
                  confirmMutation.mutate({ order: stockWarning.order, force: true })
                  setStockWarning(null)
                }}
                disabled={confirmMutation.isPending}
                className="flex-1 py-2 bg-orange-500 text-white rounded-lg text-sm font-medium hover:bg-orange-600 disabled:opacity-50 transition-colors"
              >
                Onayla (Eksi Stok)
              </button>
              <button
                onClick={() => setStockWarning(null)}
                className="px-4 py-2 border border-gray-300 text-gray-700 rounded-lg text-sm hover:bg-gray-50 transition-colors"
              >
                İptal
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Onay Modal */}
      {confirmModal && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-2xl shadow-xl w-full max-w-md p-6">
            <h2 className="font-bold text-gray-900 mb-1">Siparişi Onayla</h2>
            <p className="text-sm text-gray-500 mb-4">{confirmModal.storeName} — ₺{confirmModal.totalAmount.toFixed(2)}</p>

            <div className="space-y-3">
              <div>
                <label className="block text-xs font-medium text-gray-700 mb-1">Vade Tarihi</label>
                <input
                  type="date"
                  value={confirmForm.dueDate}
                  onChange={e => setConfirmForm(f => ({ ...f, dueDate: e.target.value }))}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
              </div>
              <div>
                <label className="block text-xs font-medium text-gray-700 mb-1">Not (isteğe bağlı)</label>
                <input
                  value={confirmForm.wholesalerNote}
                  onChange={e => setConfirmForm(f => ({ ...f, wholesalerNote: e.target.value }))}
                  placeholder="Teslimat bilgisi vb."
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
              </div>
              <label className="flex items-center gap-2 text-sm text-gray-700 cursor-pointer">
                <input
                  type="checkbox"
                  checked={confirmForm.createCreditEntry}
                  onChange={e => setConfirmForm(f => ({ ...f, createCreditEntry: e.target.checked }))}
                  className="rounded"
                />
                Veresiye kaydı oluştur
              </label>
            </div>

            <div className="flex gap-2 mt-5">
              <button
                onClick={() => confirmMutation.mutate({ order: confirmModal })}
                disabled={confirmMutation.isPending}
                className="flex-1 py-2 bg-blue-600 text-white rounded-lg text-sm font-medium hover:bg-blue-700 disabled:opacity-50 transition-colors"
              >
                {confirmMutation.isPending ? 'Onaylanıyor...' : 'Onayla'}
              </button>
              <button
                onClick={() => setConfirmModal(null)}
                className="px-4 py-2 border border-gray-300 text-gray-700 rounded-lg text-sm hover:bg-gray-50 transition-colors"
              >
                İptal
              </button>
            </div>
          </div>
        </div>
      )}
    </Layout>
  )
}
