export type UserRole = 'Admin' | 'Wholesaler' | 'Store'

export interface AuthResponse {
  token: string
  role: UserRole
  userId: string
  profileId?: string
  displayName: string
}

export interface Product {
  id: string
  wholesalerId: string
  wholesalerName: string
  categoryId: string
  categoryName: string
  name: string
  description?: string
  price: number
  unit: string
  minOrderQty: number
  stock: number
  isActive: boolean
  images: ProductImage[]
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

export interface CreditTransaction {
  id: string
  type: 'OrderDebit' | 'ManualDebit' | 'Payment'
  amount: number
  description: string
  dueDate?: string
  orderId?: string
  createdAt: string
}

export interface CreditSummary {
  storeId: string
  storeName: string
  totalDebt: number
  totalPaid: number
  balance: number
  overdueAmount: number
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

export interface OrderItem {
  productId: string
  productName: string
  quantity: number
  unitPrice: number
  total: number
}

export interface Wholesaler {
  id: string
  companyName: string
  phone?: string
  address?: string
  description?: string
}
