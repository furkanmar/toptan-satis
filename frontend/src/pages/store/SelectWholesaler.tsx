import { useQuery } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import Layout from '../../components/Layout'
import { storeWholesalersApi } from '../../api/client'
import type { AssignedWholesaler } from '../../types'

export default function SelectWholesaler() {
  const navigate = useNavigate()

  const { data: wholesalers = [], isLoading } = useQuery<AssignedWholesaler[]>({
    queryKey: ['my-wholesalers'],
    queryFn: storeWholesalersApi.myWholesalers
  })

  return (
    <Layout>
      <div className="max-w-2xl mx-auto py-4">
        <h1 className="text-xl font-bold text-gray-800 mb-1">Toptancı Seçin</h1>
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
    </Layout>
  )
}
