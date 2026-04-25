import axios from 'axios'

const API_BASE = import.meta.env.VITE_API_URL ?? '/api'

export const api = axios.create({
  baseURL: API_BASE,
  headers: { 'Content-Type': 'application/json' }
})

// JWT token her isteğe otomatik eklenir
api.interceptors.request.use(config => {
  const token = localStorage.getItem('token')
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

// 401 → login sayfasına yönlendir
api.interceptors.response.use(
  res => res,
  err => {
    if (err.response?.status === 401) {
      localStorage.removeItem('token')
      window.location.href = '/login'
    }
    return Promise.reject(err)
  }
)

// ─── Auth ────────────────────────────────────────────────────────────────────
export const authApi = {
  login: (email: string, password: string) =>
    api.post('/auth/login', { email, password }).then(r => r.data),
}

// ─── Products ────────────────────────────────────────────────────────────────
export const productsApi = {
  getAll: (params?: { wholesalerId?: string; categoryId?: string }) =>
    api.get('/products', { params }).then(r => r.data),
  getById: (id: string) =>
    api.get(`/products/${id}`).then(r => r.data),
  create: (data: unknown) =>
    api.post('/products', data).then(r => r.data),
  update: (id: string, data: unknown) =>
    api.put(`/products/${id}`, data).then(r => r.data),
  uploadImage: (id: string, file: File) => {
    const form = new FormData()
    form.append('file', file)
    return api.post(`/products/${id}/images`, form, {
      headers: { 'Content-Type': 'multipart/form-data' }
    })
  }
}

// ─── Orders ──────────────────────────────────────────────────────────────────
export const ordersApi = {
  create: (data: unknown) =>
    api.post('/orders', data).then(r => r.data),
  myOrders: () =>
    api.get('/orders/my').then(r => r.data),
  incoming: () =>
    api.get('/orders/incoming').then(r => r.data),
  confirm: (id: string, data: { wholesalerNote?: string; dueDate?: string; createCreditEntry?: boolean }) =>
    api.post(`/orders/${id}/confirm`, data).then(r => r.data),
  updateItems: (id: string, data: { items: { productId: string; quantity: number }[]; wholesalerNote?: string }) =>
    api.put(`/orders/${id}/items`, data).then(r => r.data),
  updateStatus: (id: string, status: string) =>
    api.patch(`/orders/${id}/status`, { status }).then(r => r.data),
  getAll: () =>
    api.get('/orders').then(r => r.data),
}

// ─── Store-Wholesaler ─────────────────────────────────────────────────────────
export const storeWholesalersApi = {
  myWholesalers: () =>
    api.get('/store-wholesalers/my').then(r => r.data),
  assign: (storeId: string, wholesalerId: string) =>
    api.post('/store-wholesalers', { storeId, wholesalerId }).then(r => r.data),
  remove: (storeId: string, wholesalerId: string) =>
    api.delete('/store-wholesalers', { data: { storeId, wholesalerId } }).then(r => r.data),
  getForStore: (storeId: string) =>
    api.get(`/store-wholesalers/store/${storeId}`).then(r => r.data),
}

// ─── Credit ──────────────────────────────────────────────────────────────────
export const creditApi = {
  getStoreCredit: (storeId: string) =>
    api.get(`/credit/store/${storeId}`).then(r => r.data),
  getWholesalerCredit: (wholesalerId: string) =>
    api.get(`/credit/wholesaler/${wholesalerId}`).then(r => r.data),
  getAllStores: () =>
    api.get('/credit/all-stores').then(r => r.data),
  addTransaction: (data: unknown) =>
    api.post('/credit', data).then(r => r.data),
}

// ─── Wholesalers ─────────────────────────────────────────────────────────────
export const wholesalersApi = {
  getAll: () =>
    api.get('/wholesalers').then(r => r.data),
  getById: (id: string) =>
    api.get(`/wholesalers/${id}`).then(r => r.data),
}

// ─── Categories ──────────────────────────────────────────────────────────────
export const categoriesApi = {
  getAll: (wholesalerId?: string) =>
    api.get('/categories', { params: wholesalerId ? { wholesalerId } : undefined }).then(r => r.data),
  create: (name: string) =>
    api.post('/categories', { name }).then(r => r.data),
  delete: (id: string) =>
    api.delete(`/categories/${id}`).then(r => r.data),
}
