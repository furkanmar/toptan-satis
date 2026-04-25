import { Link, useNavigate } from 'react-router-dom'
import { useAuthStore } from '../store/authStore'

interface LayoutProps {
  children: React.ReactNode
  navLinks: { to: string; label: string }[]
}

export default function Layout({ children, navLinks }: LayoutProps) {
  const { displayName, role, logout } = useAuthStore()
  const navigate = useNavigate()

  const handleLogout = () => {
    logout()
    navigate('/login')
  }

  const roleLabel = role === 'Admin' ? 'Yönetici' : role === 'Wholesaler' ? 'Toptancı' : 'Mağaza'
  const roleColor = role === 'Admin' ? 'bg-red-100 text-red-700' : role === 'Wholesaler' ? 'bg-purple-100 text-purple-700' : 'bg-green-100 text-green-700'

  return (
    <div className="min-h-screen bg-gray-50">
      <header className="bg-white border-b border-gray-200 sticky top-0 z-10">
        <div className="max-w-7xl mx-auto px-4 h-14 flex items-center justify-between">
          <div className="flex items-center gap-6">
            <span className="font-bold text-gray-900">Toptan</span>
            <nav className="flex gap-4">
              {navLinks.map(link => (
                <Link
                  key={link.to}
                  to={link.to}
                  className="text-sm text-gray-600 hover:text-gray-900 transition-colors"
                >
                  {link.label}
                </Link>
              ))}
            </nav>
          </div>
          <div className="flex items-center gap-3">
            <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${roleColor}`}>{roleLabel}</span>
            <span className="text-sm text-gray-700">{displayName}</span>
            <button
              onClick={handleLogout}
              className="text-sm text-gray-500 hover:text-gray-900 transition-colors"
            >
              Çıkış
            </button>
          </div>
        </div>
      </header>
      <main className="max-w-7xl mx-auto px-4 py-6">{children}</main>
    </div>
  )
}
