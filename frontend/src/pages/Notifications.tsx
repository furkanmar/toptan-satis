import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import Layout from '../components/Layout'
import LoadingSkeleton from '../components/ui/LoadingSkeleton'
import EmptyState from '../components/ui/EmptyState'
import ErrorState from '../components/ui/ErrorState'
import { NotificationStatusBadge } from '../components/ui/StatusBadge'
import { usersApi } from '../api/client'
import { formatDate } from '../lib/utils'
import { useAuthStore } from '../store/authStore'
import type { NotificationLogPage } from '../types'

const NOTIF_TYPE_LABELS: Record<string, string> = {
  OrderCreated:    '🛒 Yeni Sipariş',
  OrderConfirmed:  '✅ Sipariş Onaylandı',
  OrderRejected:   '❌ Sipariş Reddedildi',
  OrderCancelled:  '🚫 Sipariş İptal',
  LowStock:        '⚠️ Düşük Stok',
}

export default function Notifications() {
  const navigate = useNavigate()
  const role = useAuthStore(s => s.role)
  const [page, setPage] = useState(1)

  const navLinks =
    role === 'Wholesaler'
      ? [
          { to: '/wholesaler', label: 'Ana Sayfa' },
          { to: '/wholesaler/products', label: 'Ürünler' },
          { to: '/wholesaler/orders', label: 'Siparişler' },
          { to: '/wholesaler/credit', label: 'Veresiye' },
          { to: '/wholesaler/settings', label: 'Ayarlar' },
          { to: '/notifications', label: 'Bildirimler' },
        ]
      : [
          { to: '/store', label: 'Ana Sayfa' },
          { to: '/notifications', label: 'Bildirimler' },
        ]

  const { data, isLoading, isError, refetch } = useQuery<NotificationLogPage>({
    queryKey: ['my-notifications', page],
    queryFn: () => usersApi.getNotifications({ page, pageSize: 20 }),
  })

  const totalPages = data ? Math.ceil(data.total / data.pageSize) : 1

  return (
    <Layout navLinks={navLinks}>
      <div className="flex items-center justify-between mb-4">
        <h1 className="text-xl font-bold text-gray-900">Bildirim Geçmişi</h1>
        {data && <span className="text-xs text-gray-500">Toplam {data.total} kayıt</span>}
      </div>

      {isLoading ? (
        <LoadingSkeleton rows={6} />
      ) : isError ? (
        <ErrorState onRetry={refetch} />
      ) : !data || data.items.length === 0 ? (
        <EmptyState
          icon="🔔"
          title="Bildirim yok"
          description="Henüz hiç bildirim gönderilmemiş"
        />
      ) : (
        <>
          <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
            {data.items.map((n, i) => {
              let payloadText = n.payload
              try {
                const p = JSON.parse(n.payload)
                payloadText = p.message ?? p.text ?? JSON.stringify(p)
              } catch { /* ham metin */ }

              return (
                <div
                  key={n.id}
                  className={`px-4 py-3 flex items-start gap-4 ${i < data.items.length - 1 ? 'border-b border-gray-100' : ''}`}
                >
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2 mb-0.5">
                      <span className="text-sm font-medium text-gray-900">
                        {NOTIF_TYPE_LABELS[n.type] ?? n.type}
                      </span>
                      <NotificationStatusBadge status={n.status} />
                      <span className="text-xs text-gray-400 ml-auto">{n.channel}</span>
                    </div>
                    <p className="text-xs text-gray-500 truncate">{payloadText}</p>
                    <div className="flex items-center gap-4 mt-1">
                      <span className="text-xs text-gray-400">{formatDate(n.createdAt)}</span>
                      {n.attemptCount > 1 && (
                        <span className="text-xs text-orange-500">{n.attemptCount} deneme</span>
                      )}
                      {n.errorMessage && (
                        <span className="text-xs text-red-400 truncate max-w-48" title={n.errorMessage}>
                          {n.errorMessage}
                        </span>
                      )}
                    </div>
                  </div>
                </div>
              )
            })}
          </div>

          {totalPages > 1 && (
            <div className="flex items-center justify-between mt-4">
              <span className="text-xs text-gray-500">Sayfa {page}/{totalPages}</span>
              <div className="flex gap-2">
                <button disabled={page <= 1} onClick={() => setPage(p => p - 1)}
                  className="px-3 py-1.5 text-xs border border-gray-300 rounded-lg disabled:opacity-40 hover:bg-gray-50">
                  ← Önceki
                </button>
                <button disabled={page >= totalPages} onClick={() => setPage(p => p + 1)}
                  className="px-3 py-1.5 text-xs border border-gray-300 rounded-lg disabled:opacity-40 hover:bg-gray-50">
                  Sonraki →
                </button>
              </div>
            </div>
          )}
        </>
      )}
    </Layout>
  )
}
