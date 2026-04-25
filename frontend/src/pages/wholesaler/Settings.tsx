import { useState } from 'react'
import { useMutation } from '@tanstack/react-query'
import Layout from '../../components/Layout'
import { api } from '../../api/client'

const NAV = [
  { to: '/wholesaler', label: 'Ana Sayfa' },
  { to: '/wholesaler/products', label: 'Ürünler' },
  { to: '/wholesaler/orders', label: 'Siparişler' },
  { to: '/wholesaler/credit', label: 'Veresiye' },
  { to: '/wholesaler/settings', label: 'Ayarlar' }
]

export default function WholesalerSettings() {
  const [form, setForm] = useState({ companyName: '', phone: '', address: '', description: '' })
  const [saved, setSaved] = useState(false)

  const saveMutation = useMutation({
    mutationFn: () => api.put('/wholesalers/me', form),
    onSuccess: () => { setSaved(true); setTimeout(() => setSaved(false), 2000) }
  })

  return (
    <Layout navLinks={NAV}>
      <h1 className="text-xl font-bold text-gray-900 mb-4">Ayarlar</h1>
      <div className="max-w-lg bg-white rounded-xl border border-gray-200 p-6">
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
    </Layout>
  )
}
