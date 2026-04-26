import { create } from 'zustand'
import { persist } from 'zustand/middleware'
import type { Product, ProductUnitConfig } from '../types'

export interface CartItem {
  product: Product
  unitConfigId: string
  unitType: string
  unitPrice: number
  contentQty: number
  qty: number
}

// key = "productId::unitConfigId"
export const cartKey = (productId: string, unitConfigId: string) =>
  `${productId}::${unitConfigId}`

interface CartState {
  wholesalerId: string | null
  items: Record<string, CartItem>
  ensureWholesaler: (wid: string) => void
  addItem: (product: Product, unitConfig: ProductUnitConfig, qty?: number) => void
  removeItem: (key: string) => void
  updateQty: (key: string, qty: number) => void
  clearCart: () => void
}

export const useCartStore = create<CartState>()(
  persist(
    (set, get) => ({
      wholesalerId: null,
      items: {},

      ensureWholesaler: (wid) => {
        if (get().wholesalerId !== wid) set({ wholesalerId: wid, items: {} })
      },

      addItem: (product, unitConfig, qty = 1) => {
        const key = cartKey(product.id, unitConfig.id)
        set(state => {
          const existing = state.items[key]
          return {
            items: {
              ...state.items,
              [key]: existing
                ? { ...existing, qty: existing.qty + qty }
                : { product, unitConfigId: unitConfig.id, unitType: unitConfig.unitType, unitPrice: unitConfig.price, contentQty: unitConfig.contentQty, qty },
            },
          }
        })
      },

      removeItem: (key) =>
        set(state => {
          const next = { ...state.items }
          delete next[key]
          return { items: next }
        }),

      updateQty: (key, qty) => {
        if (qty <= 0) {
          set(state => {
            const next = { ...state.items }
            delete next[key]
            return { items: next }
          })
        } else {
          set(state => ({ items: { ...state.items, [key]: { ...state.items[key], qty } } }))
        }
      },

      clearCart: () => set({ items: {} }),
    }),
    { name: 'cart-storage' }
  )
)
