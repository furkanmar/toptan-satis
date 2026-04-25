import { useQuery } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { storeWholesalersApi } from '../../api/client'
import { useAuthStore } from '../../store/authStore'
import type { AssignedWholesaler } from '../../types'

export default function SelectWholesaler() {
  const navigate = useNavigate()
  const { displayName, logout } = useAuthStore()

  const { data: wholesalers = [], isLoading } = useQuery<AssignedWholesaler[]>({
    queryKey: ['my-wholesalers'],
    queryFn: storeWholesalersApi.myWholesalers
  })

  const handleLogout = () => { logout(); navigate('/login') }

  return (
    <div className="min-h-screen bg-gray-50">
      <header className="bg-white border-b border-gray-200">
        <div className="max-w-2xl mx-auto px-4 h-14 flex items-center justify-between">
          <span className="font-bold text-gray-900">Toptan</span>
          <div className="flex items-center gap-3">
            <span className="text-sm text-gray-600">{displayName}</span>
            <button onClick={handleLogout} className="text-sm text-gray-400 hover:text-gray-700">Çıkış</button>
          </div>
        </div>
      </header>

      <div className="max-w-2xl mx-auto px-4 py-10">
        <h1 className="text-xl font-bold text-gray-900 mb-1">Toptancı Seçin</h1>
        <p className="text-sm text-gray-500 mb-6">Çalışmak istediğiniz toptancıyı seçin</p>

        {isLoading ? (
          <div className="text-center py-12 text-gray-400">Yükleniyor...</div>
        ) : wholesalers.length === 0 ? (
          <div className="text-center py-12">
            <p className="text-gray-400 mb-2">Henüz atanmış toptancınız yok</p>
            <p className="text-xs text-gray-400">Yöneticinizle iletişime geçin</p>
          </div>
        ) : (
          <div className="space-y-3">
            {wholesalers.map(w => (
              <button
                key={w.wholesalerId}
                onClick={() => navigate(`/store/${w.wholesalerId}`)}
                className="w-full bg-white rounded-xl border border-gray-200 p-4 text-left hover:border-blue-400 hover:shadow-md transition-all"
              >
                <div className="flex items-center justify-between">
                  <div>
                    <p className="font-semibold text-gray-900">{w.companyName}</p>
                    {w.phone && <p className="text-xs text-gray-500 mt-0.5">{w.phone}</p>}
                    {w.address && <p className="text-xs text-gray-400">{w.address}</p>}
                    {w.description && <p className="text-xs text-gray-500 mt-1 italic">{w.description}</p>}
                  </div>
                  <span className="text-gray-300 text-xl">→</span>
                </div>
              </button>
            ))}
          </div>
        )}
      </div>
    </div>
  )
}
