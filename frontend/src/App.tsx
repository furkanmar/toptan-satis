import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { useAuthStore } from './store/authStore'
import LoginPage from './pages/Login'

// Store sayfaları
import StoreHome from './pages/store/Home'
import StoreOrders from './pages/store/Orders'

// Wholesaler sayfaları
import WholesalerProducts from './pages/wholesaler/Products'
import WholesalerOrders from './pages/wholesaler/Orders'

// Admin sayfaları
import AdminDashboard from './pages/admin/Dashboard'

function RequireAuth({ children, roles }: { children: React.ReactNode; roles?: string[] }) {
  const { isAuthenticated, role } = useAuthStore()
  if (!isAuthenticated) return <Navigate to="/login" replace />
  if (roles && role && !roles.includes(role)) return <Navigate to="/" replace />
  return <>{children}</>
}

function RootRedirect() {
  const { isAuthenticated, role } = useAuthStore()
  if (!isAuthenticated) return <Navigate to="/login" replace />
  if (role === 'Admin') return <Navigate to="/admin" replace />
  if (role === 'Wholesaler') return <Navigate to="/wholesaler/products" replace />
  return <Navigate to="/store" replace />
}

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<RootRedirect />} />
        <Route path="/login" element={<LoginPage />} />

        {/* Mağaza */}
        <Route path="/store" element={<RequireAuth roles={['Store']}><StoreHome /></RequireAuth>} />
        <Route path="/store/orders" element={<RequireAuth roles={['Store']}><StoreOrders /></RequireAuth>} />

        {/* Toptancı */}
        <Route path="/wholesaler/products" element={<RequireAuth roles={['Wholesaler']}><WholesalerProducts /></RequireAuth>} />
        <Route path="/wholesaler/orders" element={<RequireAuth roles={['Wholesaler']}><WholesalerOrders /></RequireAuth>} />

        {/* Admin */}
        <Route path="/admin" element={<RequireAuth roles={['Admin']}><AdminDashboard /></RequireAuth>} />
      </Routes>
    </BrowserRouter>
  )
}
