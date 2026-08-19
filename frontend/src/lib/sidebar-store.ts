import { create } from 'zustand'

/** Mesmo padrão de persistência do theme-store (task 025) — string crua em localStorage, sem
 * envelope JSON do zustand/persist. Aqui não há risco de FOUC (é só largura de sidebar, não cor),
 * então não precisa de script inline no index.html — o valor é lido no primeiro render do módulo. */
const SIDEBAR_STORAGE_KEY = 'odonto-sidebar-collapsed'

function readStoredCollapsed(): boolean {
  if (typeof window === 'undefined') return false
  return window.localStorage.getItem(SIDEBAR_STORAGE_KEY) === 'true'
}

interface SidebarState {
  collapsed: boolean
  toggleCollapsed: () => void
}

/** Estado de colapso da sidebar (desktop) — task 026. Persistido, sobrevive a reload. */
export const useSidebarStore = create<SidebarState>((set, get) => ({
  collapsed: readStoredCollapsed(),
  toggleCollapsed: () => {
    const next = !get().collapsed
    window.localStorage.setItem(SIDEBAR_STORAGE_KEY, String(next))
    set({ collapsed: next })
  },
}))
