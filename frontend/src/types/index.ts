export type UserRole = 'Admin' | 'Wholesaler' | 'Store'

export interface AuthResponse {
  token: string
  role: UserRole
  userId: string
  profileId?: string
  displayName: string
}

export interface ProductBarcode {
  id: string
  barcode: string
  note?: string
}

export interface ProductUnitConfig {
  id: string
  unitType: string         // "Adet", "Paket", "Koli", "Kg", "Litre"
  contentQty: number       // kaç temel birim içeriyor
  price: number
  sortOrder: number
  barcodes: ProductBarcode[]
}

export interface Product {
  id: string
  wholesalerId: string
  wholesalerName: string
  categoryId: string
  categoryName: string
  name: string
  description?: string
  brand?: string
  manufacturer?: string
  price: number            // referans fiyat (en küçük birim)
  vatRate: number          // KDV oranı: 0, 1, 10, 18, 20
  minOrderQty: number
  stock: number
  minimumStockLevel?: number | null
  isActive: boolean
  createdAt: string
  images: ProductImage[]
  unitConfigs: ProductUnitConfig[]
}

export interface ProductImage {
  id: string
  url: string
  isMain: boolean
}

export interface Category {
  id: string
  name: string
  slug: string
}

export interface Order {
  id: string
  storeId: string
  storeName: string
  wholesalerId: string
  wholesalerName: string
  status: OrderStatus
  totalAmount: number
  note?: string
  wholesalerNote?: string
  dueDate?: string
  createdAt: string
  updatedAt?: string
  items: OrderItem[]
}

export interface OrderItem {
  id: string
  productId: string
  productName: string
  quantity: number
  unitPrice: number
  total: number
  unitType: string
  contentQty: number
  vatRate: number
}

export interface CreditTransaction {
  id: string
  type: 'OrderDebit' | 'ManualDebit' | 'Payment'
  amount: number
  allocatedAmount: number
  remainingAmount: number
  isFullyAllocated: boolean
  description: string
  dueDate?: string
  orderId?: string
  createdAt: string
}

export interface CreditSummary {
  storeId: string
  storeName: string
  totalDebt: number
  totalPayment: number
  balance: number
  openDebtCount: number
  overdueAmount: number
  oldestOverdueDate?: string
  unallocatedPaymentAmount: number
  transactions: CreditTransaction[]
}

export interface AssignedWholesaler {
  wholesalerId: string
  companyName: string
  phone?: string
  address?: string
  description?: string
  assignedAt: string
}

export type OrderStatus = 'Pending' | 'Confirmed' | 'Rejected' | 'Delivered' | 'Cancelled'

export interface Wholesaler {
  id: string
  companyName: string
  phone?: string
  address?: string
  description?: string
}

export interface StoreWholesalerRelation {
  storeId: string
  storeName: string
  storePhone?: string
  wholesalerId: string
  wholesalerName: string
  isActive: boolean
  assignedAt: string
}

// ─── Stok hareketleri ────────────────────────────────────────────────────────

export type MovementType =
  | 'InitialBalance'
  | 'OrderConfirm'
  | 'OrderCancel'
  | 'OrderReject'
  | 'ManualAdjustment'
  | 'Return'
  | 'ForceConfirmNegative'

export interface StockMovement {
  id: string
  productId: string
  productName: string
  movementType: MovementType
  quantityChange: number
  balanceAfter: number
  orderId?: string | null
  userId: string
  reason?: string | null
  createdAt: string
}

export interface StockMovementPage {
  items: StockMovement[]
  total: number
  page: number
  pageSize: number
}

// ─── Audit log ───────────────────────────────────────────────────────────────

export interface AuditLog {
  id: string
  userId?: string | null
  userRole: string
  action: string
  entityType: string
  entityId: string
  changes?: string | null
  ipAddress?: string | null
  createdAt: string
}

export interface AuditLogPage {
  items: AuditLog[]
  total: number
  page: number
  pageSize: number
}

// ─── Bildirimler ─────────────────────────────────────────────────────────────

export type NotificationType =
  | 'OrderCreated' | 'OrderConfirmed' | 'OrderRejected' | 'OrderCancelled' | 'LowStock'

export type NotificationStatus = 'Pending' | 'Sent' | 'Failed'

export interface NotificationLog {
  id: string
  type: NotificationType
  payload: string
  channel: string
  status: NotificationStatus
  attemptCount: number
  lastAttemptAt?: string | null
  errorMessage?: string | null
  createdAt: string
}

export interface NotificationLogPage {
  items: NotificationLog[]
  total: number
  page: number
  pageSize: number
}
