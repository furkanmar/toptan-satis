import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import Layout from '../../components/Layout'
import { api } from '../../api/client'

const NAV = [
  { to: '/admin', label: 'Dashboard' },
  { to: '/admin/users', label: 'Kullanıcılar' },
  { to: '/admin/categories', label: 'Kategoriler' }
]

type UserRole = 'wholesaler' | 'store'

interface UserRecord {
  id: string
  email: string
  role: string
  isActive: boolean
  createdAt: string
  wholesaler?: { companyName: string }
  store?: { storeName: string }
}

const EMPTY_FORM = { email: '', password: '', companyName: '' }

export default function AdminUsers() {
  const qc = useQueryClient()
  const [role, setRole] = useState<UserRole>('wholesaler')
  const [form, setForm] = useState(EMPTY_FORM)
  const [success, setSuccess] = useState('')
  const [error, setError] = useState('')

  const { data: users = [], isLoading } = useQuery<UserRecord[]>({
    queryKey: ['admin-users'],
    queryFn: () => api.get('/admin/users').then(r => r.data)
  })

  const createMutation = useMutation({
    mutationFn: () => api.post(`/auth/register/${role}`, form).then(r => r.data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['admin-users'] })
      setForm(EMPTY_FORM)
      setError('')
      setSuccess(`${role === 'wholesaler' ? 'Toptancı' : 'Mağaza'} oluşturuldu`)
      setTimeout(() => setSuccess(''), 3000)
    },
    onError: (err: unknown) => {
      const msg = (err as { response?: { data?: { error?: string } } })?.response?.data?.error
      setError(msg ?? 'Hata oluştu')
    }
  })

  const toggleActive = useMutation({
    mutationFn: ({ id, isActive }: { id: string; isActive: boolean }) =>
      api.patch(`/admin/users/${id}`, { isActive }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['admin-users'] })
  })

  const wholesalers = users.filter(u => u.role === 'Wholesaler')
  const stores = users.filter(u => u.role === 'Store')

  return (
    <Layout navLinks={NAV}>
      <h1 className="text-xl font-bold text-gray-900 mb-4">Kullanıcı Yönetimi</h1>

      <div className="grid grid-cols-3 gap-6">
        {/* Form */}
        <div className="bg-white rounded-xl border border-gray-200 p-4 h-fit">
          <h2 className="font-semibold text-gray-900 mb-3">Yeni Hesap Oluştur</h2>

          <div className="flex gap-2 mb-4">
            {(['wholesaler', 'store'] as UserRole[]).map(r => (
              <button
                key={r}
                onClick={() => setRole(r)}
                className={`flex-1 py-1.5 rounded-lg text-sm font-medium transition-colors ${
                  role === r
                    ? r === 'wholesaler' ? 'bg-purple-600 text-white' : 'bg-green-600 text-white'
                    : 'border border-gray-300 text-gray-600 hover:bg-gray-50'
                }`}
              >
                {r === 'wholesaler' ? '🏭 Toptancı' : '🏪 Mağaza'}
              </button>
            ))}
          </div>

          <div className="space-y-3">
            <div>
              <label className="block text-xs font-medium text-gray-700 mb-1">
                {role === 'wholesaler' ? 'Firma Adı' : 'Mağaza Adı'}
              </label>
              <input
                value={form.companyName}
                onChange={e => setForm(f => ({ ...f, companyName: e.target.value }))}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                placeholder={role === 'wholesaler' ? 'ABC Gıda Ltd.' : 'Merkez Market'}
              />
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-700 mb-1">Email</label>
              <input
                type="email"
                value={form.email}
                onChange={e => setForm(f => ({ ...f, email: e.target.value }))}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-700 mb-1">Şifre</label>
              <input
                type="password"
                value={form.password}
                onChange={e => setForm(f => ({ ...f, password: e.target.value }))}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
          </div>

          {error && (
            <div className="mt-3 text-xs text-red-600 bg-red-50 border border-red-200 rounded-lg px-3 py-2">{error}</div>
          )}
          {success && (
            <div className="mt-3 text-xs text-green-600 bg-green-50 border border-green-200 rounded-lg px-3 py-2">✓ {success}</div>
          )}

          <button
            onClick={() => createMutation.mutate()}
            disabled={createMutation.isPending || !form.email || !form.password}
            className="mt-4 w-full py-2 bg-blue-600 text-white rounded-lg text-sm font-medium hover:bg-blue-700 disabled:opacity-50 transition-colors"
          >
            {createMutation.isPending ? 'Oluşturuluyor...' : 'Oluştur'}
          </button>
        </div>

        {/* Listeler */}
        <div className="col-span-2 space-y-4">
          {/* Toptancılar */}
          <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
            <div className="px-4 py-3 border-b border-gray-100 flex items-center justify-between">
              <h2 className="font-semibold text-gray-900 text-sm">🏭 Toptancılar</h2>
              <span className="text-xs text-gray-400">{wholesalers.length} kayıt</span>
            </div>
            {isLoading ? (
              <div className="text-center py-6 text-gray-400 text-sm">Yükleniyor...</div>
            ) : wholesalers.length === 0 ? (
              <div className="text-center py-6 text-gray-400 text-sm">Henüz toptancı yok</div>
            ) : (
              <table className="w-full text-sm">
                <thead>
                  <tr className="bg-gray-50 border-b border-gray-100">
                    <th className="text-left px-4 py-2 text-xs font-medium text-gray-500">Firma</th>
                    <th className="text-left px-4 py-2 text-xs font-medium text-gray-500">Email</th>
                    <th className="text-left px-4 py-2 text-xs font-medium text-gray-500">Tarih</th>
                    <th className="text-center px-4 py-2 text-xs font-medium text-gray-500">Durum</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-50">
                  {wholesalers.map(u => (
                    <tr key={u.id} className="hover:bg-gray-50">
                      <td className="px-4 py-2.5 font-medium text-gray-900">{u.wholesaler?.companyName ?? '—'}</td>
                      <td className="px-4 py-2.5 text-gray-600">{u.email}</td>
                      <td className="px-4 py-2.5 text-gray-400 text-xs">{new Date(u.createdAt).toLocaleDateString('tr-TR')}</td>
                      <td className="px-4 py-2.5 text-center">
                        <button
                          onClick={() => toggleActive.mutate({ id: u.id, isActive: !u.isActive })}
                          className={`px-2 py-0.5 rounded-full text-xs font-medium transition-colors ${u.isActive ? 'bg-green-100 text-green-700 hover:bg-green-200' : 'bg-gray-100 text-gray-500 hover:bg-gray-200'}`}
                        >
                          {u.isActive ? 'Aktif' : 'Pasif'}
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </div>

          {/* Mağazalar */}
          <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
            <div className="px-4 py-3 border-b border-gray-100 flex items-center justify-between">
              <h2 className="font-semibold text-gray-900 text-sm">🏪 Mağazalar</h2>
              <span className="text-xs text-gray-400">{stores.length} kayıt</span>
            </div>
            {isLoading ? (
              <div className="text-center py-6 text-gray-400 text-sm">Yükleniyor...</div>
            ) : stores.length === 0 ? (
              <div className="text-center py-6 text-gray-400 text-sm">Henüz mağaza yok</div>
            ) : (
              <table className="w-full text-sm">
                <thead>
                  <tr className="bg-gray-50 border-b border-gray-100">
                    <th className="text-left px-4 py-2 text-xs font-medium text-gray-500">Mağaza</th>
                    <th className="text-left px-4 py-2 text-xs font-medium text-gray-500">Email</th>
                    <th className="text-left px-4 py-2 text-xs font-medium text-gray-500">Tarih</th>
                    <th className="text-center px-4 py-2 text-xs font-medium text-gray-500">Durum</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-50">
                  {stores.map(u => (
                    <tr key={u.id} className="hover:bg-gray-50">
                      <td className="px-4 py-2.5 font-medium text-gray-900">{u.store?.storeName ?? '—'}</td>
                      <td className="px-4 py-2.5 text-gray-600">{u.email}</td>
                      <td className="px-4 py-2.5 text-gray-400 text-xs">{new Date(u.createdAt).toLocaleDateString('tr-TR')}</td>
                      <td className="px-4 py-2.5 text-center">
                        <button
                          onClick={() => toggleActive.mutate({ id: u.id, isActive: !u.isActive })}
                          className={`px-2 py-0.5 rounded-full text-xs font-medium transition-colors ${u.isActive ? 'bg-green-100 text-green-700 hover:bg-green-200' : 'bg-gray-100 text-gray-500 hover:bg-gray-200'}`}
                        >
                          {u.isActive ? 'Aktif' : 'Pasif'}
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </div>
        </div>
      </div>
    </Layout>
  )
}
