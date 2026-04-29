import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import Layout from '../../components/Layout'
import { deliveryNotesApi } from '../../api/client'
import { DeliveryNoteListItem } from '../../types'

const NAV = [
  { to: '/wholesaler', label: 'Ana Sayfa' },
  { to: '/wholesaler/products', label: 'Ürünler' },
  { to: '/wholesaler/orders', label: 'Siparişler' },
  { to: '/wholesaler/delivery-notes', label: 'İrsaliyeler' },
  { to: '/wholesaler/credit', label: 'Veresiye' },
  { to: '/wholesaler/settings', label: 'Ayarlar' }
]

async function openPdfBlob(url: string) {
  const token = localStorage.getItem('token')
  const res = await fetch(url, { headers: { Authorization: `Bearer ${token}` } })
  if (!res.ok) { toast.error('PDF indirilemedi'); return }
  const blob = await res.blob()
  const blobUrl = URL.createObjectURL(blob)
  window.open(blobUrl, '_blank')
  setTimeout(() => URL.revokeObjectURL(blobUrl), 10_000)
}

function statusLabel(s: string) {
  return s === 'Issued' ? 'Düzenlendi' : s === 'Cancelled' ? 'İptal' : 'Taslak'
}

function statusColor(s: string) {
  return s === 'Issued'
    ? 'bg-green-100 text-green-800'
    : s === 'Cancelled'
    ? 'bg-red-100 text-red-800'
    : 'bg-gray-100 text-gray-700'
}

export default function WholesalerDeliveryNotes() {
  const qc = useQueryClient()
  const [filters, setFilters] = useState({ from: '', to: '', status: '' })
  const [cancelModal, setCancelModal] = useState<{ id: string; noteNumber: string } | null>(null)
  const [cancelReason, setCancelReason] = useState('')

  const { data: notes = [], isLoading } = useQuery<DeliveryNoteListItem[]>({
    queryKey: ['delivery-notes', filters],
    queryFn: () =>
      deliveryNotesApi.getList({
        from: filters.from || undefined,
        to: filters.to || undefined,
        status: filters.status || undefined,
      }),
  })

  const cancelMutation = useMutation({
    mutationFn: (noteId: string) =>
      deliveryNotesApi.cancel(noteId, { cancelReason: cancelReason.trim() || undefined }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['delivery-notes'] })
      setCancelModal(null)
      setCancelReason('')
      toast.success('İrsaliye iptal edildi')
    },
    onError: (e: unknown) => {
      const msg = (e as { response?: { data?: { message?: string } } })?.response?.data?.message
      toast.error(msg ?? 'İptal işlemi başarısız')
    },
  })

  const openPdf = (noteId: string) => openPdfBlob(deliveryNotesApi.getPdfUrl(noteId))

  return (
    <Layout navLinks={NAV}>
    <div className="max-w-6xl mx-auto">
      <h1 className="text-xl font-bold text-gray-900 mb-6">Sevk İrsaliyeleri</h1>

      {/* Filtreler */}
      <div className="bg-white rounded-xl shadow-sm border border-gray-100 p-4 mb-6 flex flex-wrap gap-3 items-end">
        <div>
          <label className="block text-xs text-gray-500 mb-1">Başlangıç</label>
          <input
            type="date"
            value={filters.from}
            onChange={e => setFilters(f => ({ ...f, from: e.target.value }))}
            className="border border-gray-300 rounded-lg px-3 py-2 text-sm focus:ring-2 focus:ring-blue-500 focus:border-transparent"
          />
        </div>
        <div>
          <label className="block text-xs text-gray-500 mb-1">Bitiş</label>
          <input
            type="date"
            value={filters.to}
            onChange={e => setFilters(f => ({ ...f, to: e.target.value }))}
            className="border border-gray-300 rounded-lg px-3 py-2 text-sm focus:ring-2 focus:ring-blue-500 focus:border-transparent"
          />
        </div>
        <div>
          <label className="block text-xs text-gray-500 mb-1">Durum</label>
          <select
            value={filters.status}
            onChange={e => setFilters(f => ({ ...f, status: e.target.value }))}
            className="border border-gray-300 rounded-lg px-3 py-2 text-sm focus:ring-2 focus:ring-blue-500 focus:border-transparent"
          >
            <option value="">Tümü</option>
            <option value="Issued">Düzenlendi</option>
            <option value="Cancelled">İptal</option>
          </select>
        </div>
        <button
          onClick={() => setFilters({ from: '', to: '', status: '' })}
          className="px-4 py-2 text-sm text-gray-600 hover:text-gray-800 border border-gray-300 rounded-lg"
        >
          Temizle
        </button>
      </div>

      {/* Tablo */}
      <div className="bg-white rounded-xl shadow-sm border border-gray-100 overflow-hidden">
        {isLoading ? (
          <div className="p-8 text-center text-gray-500">Yükleniyor...</div>
        ) : notes.length === 0 ? (
          <div className="p-8 text-center text-gray-500">İrsaliye bulunamadı</div>
        ) : (
          <table className="w-full text-sm">
            <thead className="bg-gray-50 border-b border-gray-100">
              <tr>
                <th className="text-left px-4 py-3 text-xs font-medium text-gray-500 uppercase">İrsaliye No</th>
                <th className="text-left px-4 py-3 text-xs font-medium text-gray-500 uppercase">Mağaza</th>
                <th className="text-left px-4 py-3 text-xs font-medium text-gray-500 uppercase">Tarih</th>
                <th className="text-left px-4 py-3 text-xs font-medium text-gray-500 uppercase">Durum</th>
                <th className="text-right px-4 py-3 text-xs font-medium text-gray-500 uppercase">Kalem</th>
                <th className="text-right px-4 py-3 text-xs font-medium text-gray-500 uppercase">Tutar</th>
                <th className="px-4 py-3" />
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-50">
              {notes.map(note => (
                <tr key={note.id} className="hover:bg-gray-50 transition-colors">
                  <td className="px-4 py-3 font-mono font-semibold text-blue-700">{note.noteNumber}</td>
                  <td className="px-4 py-3 text-gray-800">{note.storeName}</td>
                  <td className="px-4 py-3 text-gray-600">
                    {new Date(note.issueDate).toLocaleDateString('tr-TR')}
                  </td>
                  <td className="px-4 py-3">
                    <span className={`px-2 py-1 rounded-full text-xs font-medium ${statusColor(note.status)}`}>
                      {statusLabel(note.status)}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-right text-gray-600">{note.itemCount}</td>
                  <td className="px-4 py-3 text-right font-semibold text-gray-800">
                    ₺{note.totalAmount.toLocaleString('tr-TR', { minimumFractionDigits: 2 })}
                  </td>
                  <td className="px-4 py-3">
                    <div className="flex gap-2 justify-end">
                      <button
                        onClick={() => openPdf(note.id)}
                        className="px-3 py-1 text-xs bg-blue-50 text-blue-700 rounded-lg hover:bg-blue-100 font-medium"
                      >
                        PDF
                      </button>
                      {note.status === 'Issued' && (
                        <button
                          onClick={() => setCancelModal({ id: note.id, noteNumber: note.noteNumber })}
                          className="px-3 py-1 text-xs bg-red-50 text-red-700 rounded-lg hover:bg-red-100 font-medium"
                        >
                          İptal
                        </button>
                      )}
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {/* İptal Modal */}
      {cancelModal && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-2xl shadow-2xl w-full max-w-md p-6">
            <h2 className="text-lg font-bold text-gray-800 mb-1">İrsaliye İptal</h2>
            <p className="text-sm text-gray-600 mb-4">
              <span className="font-semibold">{cancelModal.noteNumber}</span> nolu irsaliye iptal edilecek.
              Sipariş durumu <strong>Onaylandı</strong>'ya geri dönecek.
            </p>
            <div className="mb-4">
              <label className="block text-sm font-medium text-gray-700 mb-1">İptal Nedeni</label>
              <textarea
                value={cancelReason}
                onChange={e => setCancelReason(e.target.value)}
                rows={3}
                placeholder="Opsiyonel..."
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:ring-2 focus:ring-red-500 focus:border-transparent resize-none"
              />
            </div>
            <div className="flex gap-3 justify-end">
              <button
                onClick={() => { setCancelModal(null); setCancelReason('') }}
                className="px-4 py-2 text-sm text-gray-600 border border-gray-300 rounded-lg hover:bg-gray-50"
              >
                Vazgeç
              </button>
              <button
                onClick={() => cancelMutation.mutate(cancelModal.id)}
                disabled={cancelMutation.isPending}
                className="px-4 py-2 text-sm bg-red-600 text-white rounded-lg hover:bg-red-700 disabled:opacity-50 font-medium"
              >
                {cancelMutation.isPending ? 'İptal ediliyor...' : 'İptal Et'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
    </Layout>
  )
}
