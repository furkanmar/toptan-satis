export type UserRole = 'Admin' | 'Wholesaler' | 'Store'

export interface AuthResponse {
  token: string
  role: UserRole
  userId: string
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
  createdAt: string
  items: OrderItem[]
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
