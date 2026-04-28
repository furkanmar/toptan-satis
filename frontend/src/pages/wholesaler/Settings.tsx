import { useState, useEffect } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import Layout from '../../components/Layout'
import { api, categoriesApi, usersApi } from '../../api/client'
import { useAuthStore } from '../../store/authStore'
import type { Category } from '../../types'

const NAV = [
  { to: '/wholesaler', label: 'Ana Sayfa' },
  { to: '/wholesaler/products', label: 'Ürünler' },
  { to: '/wholesaler/orders', label: 'Siparişler' },
  { to: '/wholesaler/credit', label: 'Veresiye' },
  { to: '/wholesaler/settings', label: 'Ayarlar' }
]

export default function WholesalerSettings() {
  const qc = useQueryClient()
  const profileId = useAuthStore(s => s.profileId)
  const [form, setForm] = useState({ companyName: '', phone: '', address: '', description: '' })
  const [saved, setSaved] = useState(false)
  const [newCat, setNewCat] = useState('')
  const [catError, setCatError] = useState('')
  const [telegramChatId, setTelegramChatId] = useState('')
  const [telegramSaved, setTelegramSaved] = useState(false)

  const saveMutation = useMutation({
    mutationFn: () => api.put('/wholesalers/me', form),
    onSuccess: () => { setSaved(true); setTimeout(() => setSaved(false), 2000) }
  })

  const { data: categories = [] } = useQuery<Category[]>({
    queryKey: ['categories', profileId],
    queryFn: () => categoriesApi.getAll(profileId ?? undefined),
    enabled: !!profileId
  })

  const createCatMutation = useMutation({
    mutationFn: () => categoriesApi.create(newCat),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['categories', profileId] })
      setNewCat('')
      setCatError('')
    },
    onError: (err: unknown) => {
      const msg = (err as { response?: { data?: { error?: string } } })?.response?.data?.error
      setCatError(msg ?? 'Hata oluştu')
    }
  })

  const deleteCatMutation = useMutation({
    mutationFn: (id: string) => categoriesApi.delete(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['categories', profileId] })
  })

  const { data: meData } = useQuery<{ telegramChatId?: string }>({
    queryKey: ['users', 'me'],
    queryFn: usersApi.getMe,
  })

  useEffect(() => {
    if (meData?.telegramChatId) setTelegramChatId(meData.telegramChatId)
  }, [meData?.telegramChatId])

  const telegramMutation = useMutation({
    mutationFn: () => usersApi.updateTelegram(telegramChatId || null),
    onSuccess: () => {
      setTelegramSaved(true)
      setTimeout(() => setTelegramSaved(false), 2000)
      qc.invalidateQueries({ queryKey: ['users', 'me'] })
    }
  })

  return (
    <Layout navLinks={NAV}>
      <h1 className="text-xl font-bold text-gray-900 mb-4">Ayarlar</h1>

      <div className="grid grid-cols-2 gap-6">
        {/* Firma bilgileri */}
        <div className="bg-white rounded-xl border border-gray-200 p-6">
          <h2 className="font-semibold text-gray-900 mb-4">Firma Bilgileri</h2>
          <div className="space-y-3">
            {[
              { key: 'companyName', label: 'Firma Adı' },
              { key: 'phone', label: 'Telefon' },
              { key: 'address', label: 'Adres' },
            ].map(({ key, label }) => (
              <div key={key}>
                <label className="block text-xs font-medium text-gray-700 mb-1">{label}</label>
                <input
                  value={form[key as keyof typeof form]}
                  onChange={e => setForm(f => ({ ...f, [key]: e.target.value }))}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
              </div>
            ))}
            <div>
              <label className="block text-xs font-medium text-gray-700 mb-1">Açıklama</label>
              <textarea
                value={form.description}
                onChange={e => setForm(f => ({ ...f, description: e.target.value }))}
                rows={3}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 resize-none"
              />
            </div>
          </div>
          <div className="flex items-center gap-3 mt-4">
            <button
              onClick={() => saveMutation.mutate()}
              disabled={saveMutation.isPending}
              className="px-4 py-2 bg-blue-600 text-white rounded-lg text-sm font-medium hover:bg-blue-700 disabled:opacity-50 transition-colors"
            >
              {saveMutation.isPending ? 'Kaydediliyor...' : 'Kaydet'}
            </button>
            {saved && <span className="text-xs text-green-600 font-medium">✓ Kaydedildi</span>}
          </div>
        </div>

        {/* Kategori yönetimi */}
        <div className="bg-white rounded-xl border border-gray-200 p-6">
          <h2 className="font-semibold text-gray-900 mb-4">Kategorilerim</h2>

          <div className="flex gap-2 mb-3">
            <input
              value={newCat}
              onChange={e => setNewCat(e.target.value)}
              onKeyDown={e => e.key === 'Enter' && newCat && createCatMutation.mutate()}
              placeholder="Yeni kategori adı"
              className="flex-1 px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
            <button
              onClick={() => createCatMutation.mutate()}
              disabled={!newCat || createCatMutation.isPending}
              className="px-3 py-2 bg-blue-600 text-white rounded-lg text-sm font-medium hover:bg-blue-700 disabled:opacity-50 transition-colors"
            >
              Ekle
            </button>
          </div>

          {catError && <p className="text-xs text-red-600 mb-2">{catError}</p>}

          {categories.length === 0 ? (
            <p className="text-sm text-gray-400 text-center py-4">Henüz kategori yok</p>
          ) : (
            <div className="space-y-1">
              {categories.map((cat: Category) => (
                <div key={cat.id} className="flex items-center justify-between px-3 py-2 rounded-lg hover:bg-gray-50">
                  <span className="text-sm text-gray-800">{cat.name}</span>
                  <button
                    onClick={() => deleteCatMutation.mutate(cat.id)}
                    className="text-xs text-red-400 hover:text-red-600 transition-colors"
                  >
                    Sil
                  </button>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>

      {/* Telegram bildirimleri */}
      <div className="mt-6 bg-white rounded-xl border border-gray-200 p-6">
        <h2 className="font-semibold text-gray-900 mb-1">Telegram Bildirimleri</h2>
        <p className="text-xs text-gray-500 mb-4">
          Yeni sipariş ve stok alarmlarını Telegram'a almak için:{' '}
          <a
            href="https://t.me/Furkanpibot"
            target="_blank"
            rel="noopener noreferrer"
            className="text-blue-600 hover:underline font-medium"
          >
            @Furkanpibot
          </a>
          {' '}botuna <strong>/start</strong> yazın, dönen Chat ID'yi aşağıya yapıştırın.
        </p>
        <div className="flex gap-3 max-w-sm">
          <input
            value={telegramChatId}
            onChange={e => setTelegramChatId(e.target.value)}
            placeholder="Örn: 123456789"
            className="flex-1 px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 font-mono"
          />
          <button
            onClick={() => telegramMutation.mutate()}
            disabled={telegramMutation.isPending}
            className="px-4 py-2 bg-blue-600 text-white rounded-lg text-sm font-medium hover:bg-blue-700 disabled:opacity-50 transition-colors"
          >
            {telegramMutation.isPending ? 'Kaydediliyor...' : 'Kaydet'}
          </button>
        </div>
        {telegramSaved && (
          <p className="text-xs text-green-600 font-medium mt-2">✓ Chat ID kaydedildi</p>
        )}
        {meData?.telegramChatId && !telegramSaved && (
          <p className="text-xs text-gray-400 mt-2">
            Aktif Chat ID: <span className="font-mono">{meData.telegramChatId}</span>
          </p>
        )}
      </div>
    </Layout>
  )
}
