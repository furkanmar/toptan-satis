import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { useAuthStore } from './store/authStore'
import LoginPage from './pages/Login'

// Store sayfaları
import SelectWholesaler from './pages/store/SelectWholesaler'
import StoreHome from './pages/store/Home'
import StoreOrders from './pages/store/Orders'
import StoreCredit from './pages/store/Credit'

// Wholesaler sayfaları
import WholesalerHome from './pages/wholesaler/Home'
import WholesalerProducts from './pages/wholesaler/Products'
import WholesalerOrders from './pages/wholesaler/Orders'
import WholesalerCredit from './pages/wholesaler/Credit'
import WholesalerSettings from './pages/wholesaler/Settings'
import WholesalerStockMovements from './pages/wholesaler/StockMovements'

// Admin sayfaları
import AdminDashboard from './pages/admin/Dashboard'
import AdminUsers from './pages/admin/Users'
import AdminStoreWholesalers from './pages/admin/StoreWholesalers'
import AdminAuditLogs from './pages/admin/AuditLogs'

// Ortak
import Notifications from './pages/Notifications'

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
  if (role === 'Wholesaler') return <Navigate to="/wholesaler" replace />
  return <Navigate to="/store" replace />
}

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<RootRedirect />} />
        <Route path="/login" element={<LoginPage />} />

        {/* Mağaza */}
        <Route path="/store" element={<RequireAuth roles={['Store']}><SelectWholesaler /></RequireAuth>} />
        <Route path="/store/:wholesalerId" element={<RequireAuth roles={['Store']}><StoreHome /></RequireAuth>} />
        <Route path="/store/:wholesalerId/orders" element={<RequireAuth roles={['Store']}><StoreOrders /></RequireAuth>} />
        <Route path="/store/:wholesalerId/credit" element={<RequireAuth roles={['Store']}><StoreCredit /></RequireAuth>} />

        {/* Toptancı */}
        <Route path="/wholesaler" element={<RequireAuth roles={['Wholesaler']}><WholesalerHome /></RequireAuth>} />
        <Route path="/wholesaler/products" element={<RequireAuth roles={['Wholesaler']}><WholesalerProducts /></RequireAuth>} />
        <Route path="/wholesaler/orders" element={<RequireAuth roles={['Wholesaler']}><WholesalerOrders /></RequireAuth>} />
        <Route path="/wholesaler/credit" element={<RequireAuth roles={['Wholesaler']}><WholesalerCredit /></RequireAuth>} />
        <Route path="/wholesaler/settings" element={<RequireAuth roles={['Wholesaler']}><WholesalerSettings /></RequireAuth>} />
        <Route path="/wholesaler/stock-movements" element={<RequireAuth roles={['Wholesaler']}><WholesalerStockMovements /></RequireAuth>} />

        {/* Admin */}
        <Route path="/admin" element={<RequireAuth roles={['Admin']}><AdminDashboard /></RequireAuth>} />
        <Route path="/admin/users" element={<RequireAuth roles={['Admin']}><AdminUsers /></RequireAuth>} />
        <Route path="/admin/store-wholesalers" element={<RequireAuth roles={['Admin']}><AdminStoreWholesalers /></RequireAuth>} />
        <Route path="/admin/audit-logs" element={<RequireAuth roles={['Admin']}><AdminAuditLogs /></RequireAuth>} />

        {/* Ortak */}
        <Route path="/notifications" element={<RequireAuth roles={['Wholesaler', 'Store']}><Notifications /></RequireAuth>} />
      </Routes>
    </BrowserRouter>
  )
}
