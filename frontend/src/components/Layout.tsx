import { Link, useNavigate, useLocation } from 'react-router-dom'
import { useAuthStore } from '../store/authStore'

interface LayoutProps {
  children: React.ReactNode
  navLinks?: { to: string; label: string }[]
}

const WHOLESALER_NAV = [
  { to: '/wholesaler',                    label: 'Ana Sayfa' },
  { to: '/wholesaler/products',           label: 'Ürünler' },
  { to: '/wholesaler/orders',             label: 'Siparişler' },
  { to: '/wholesaler/delivery-notes',     label: 'İrsaliyeler' },
  { to: '/wholesaler/stock-movements',    label: 'Stok Hareketleri' },
  { to: '/wholesaler/credit',             label: 'Veresiye' },
  { to: '/wholesaler/settings',           label: 'Ayarlar' },
  { to: '/notifications',                 label: 'Bildirimler' },
]

const ADMIN_NAV = [
  { to: '/admin',                         label: 'Dashboard' },
  { to: '/admin/users',                   label: 'Kullanıcılar' },
  { to: '/admin/store-wholesalers',       label: 'Mağaza-Toptancı' },
  { to: '/admin/categories',              label: 'Kategoriler' },
  { to: '/admin/audit-logs',              label: 'Audit Log' },
]

const STORE_DEFAULT_NAV = [
  { to: '/store',                         label: 'Toptancı Seç' },
  { to: '/notifications',                 label: 'Bildirimler' },
]

export default function Layout({ children, navLinks }: LayoutProps) {
  const { displayName, role, logout } = useAuthStore()
  const navigate = useNavigate()
  const location = useLocation()

  const handleLogout = () => {
    logout()
    navigate('/login')
  }

  const roleLabel = role === 'Admin' ? 'Yönetici' : role === 'Wholesaler' ? 'Toptancı' : 'Mağaza'
  const roleColor =
    role === 'Admin'      ? 'bg-red-500/20 text-red-300' :
    role === 'Wholesaler' ? 'bg-violet-500/20 text-violet-300' :
                            'bg-emerald-500/20 text-emerald-300'

  const links = navLinks ?? (
    role === 'Wholesaler' ? WHOLESALER_NAV :
    role === 'Admin'      ? ADMIN_NAV :
                            STORE_DEFAULT_NAV
  )

  return (
    <div className="min-h-screen flex bg-slate-100">
      {/* Sidebar */}
      <aside className="w-56 min-h-screen bg-slate-800 flex flex-col fixed top-0 left-0 z-20">
        {/* Brand */}
        <div className="px-5 h-14 flex items-center border-b border-slate-700">
          <span className="font-bold text-white text-lg tracking-tight">Toptan</span>
        </div>

        {/* Nav links */}
        <nav className="flex-1 py-3 px-2 space-y-0.5 overflow-y-auto">
          {links.map(link => {
            const isActive = location.pathname === link.to
            return (
              <Link
                key={link.to}
                to={link.to}
                className={`flex items-center px-3 py-2 rounded-lg text-sm font-medium transition-colors ${
                  isActive
                    ? 'bg-slate-600 text-white'
                    : 'text-slate-300 hover:bg-slate-700 hover:text-white'
                }`}
              >
                {link.label}
              </Link>
            )
          })}
        </nav>

        {/* User info + logout */}
        <div className="px-4 py-4 border-t border-slate-700 space-y-2">
          <span className={`inline-block text-xs px-2 py-0.5 rounded-full font-medium ${roleColor}`}>
            {roleLabel}
          </span>
          <p className="text-sm text-slate-300 truncate">{displayName}</p>
          <button
            onClick={handleLogout}
            className="text-xs text-slate-500 hover:text-slate-200 transition-colors"
          >
            Çıkış Yap
          </button>
        </div>
      </aside>

      {/* Content */}
      <div className="ml-56 flex-1 min-h-screen">
        <main className="max-w-6xl mx-auto px-6 py-6">{children}</main>
      </div>
    </div>
  )
}
