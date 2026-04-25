import { create } from 'zustand'
import { persist } from 'zustand/middleware'

interface WholesalerState {
  selectedWholesalerId: string | null
  selectedWholesalerName: string | null
  select: (id: string, name: string) => void
  clear: () => void
}

export const useWholesalerStore = create<WholesalerState>()(
  persist(
    set => ({
      selectedWholesalerId: null,
      selectedWholesalerName: null,
      select: (id, name) => set({ selectedWholesalerId: id, selectedWholesalerName: name }),
      clear: () => set({ selectedWholesalerId: null, selectedWholesalerName: null })
    }),
    { name: 'wholesaler-storage' }
  )
)
