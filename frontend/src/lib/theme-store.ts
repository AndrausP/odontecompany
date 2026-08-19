import { create } from 'zustand'

export type Theme = 'light' | 'dark' | 'system'

/** Mesma chave usada pelo script inline em `index.html` — precisa ser o MESMO nome dos dois lados. */
export const THEME_STORAGE_KEY = 'odonto-theme'

function isTheme(value: string | null): value is Theme {
  return value === 'light' || value === 'dark' || value === 'system'
}

function readStoredTheme(): Theme {
  if (typeof window === 'undefined') return 'system'
  const stored = window.localStorage.getItem(THEME_STORAGE_KEY)
  return isTheme(stored) ? stored : 'system'
}

function systemPrefersDark(): boolean {
  return typeof window !== 'undefined' && window.matchMedia('(prefers-color-scheme: dark)').matches
}

function resolveIsDark(theme: Theme): boolean {
  return theme === 'system' ? systemPrefersDark() : theme === 'dark'
}

function applyThemeClass(theme: Theme) {
  if (typeof document === 'undefined') return
  document.documentElement.classList.toggle('dark', resolveIsDark(theme))
}

interface ThemeState {
  theme: Theme
  setTheme: (theme: Theme) => void
}

/**
 * Estado de tema (light/dark/system) — task 025.
 *
 * Persistência é feita como STRING CRUA em `localStorage` (chave `odonto-theme`), não via
 * `zustand/persist` (que embrulha o valor em JSON `{state:{...},version:...}`). O motivo é o
 * script inline em `index.html`, que roda ANTES do bundle React carregar pra evitar FOUC (flash
 * de tema errado) — ele precisa ler o mesmo valor com o mesmo formato simples, sem duplicar a
 * lógica de (de)serialização do zustand num script <head> isolado.
 *
 * `system` acompanha `prefers-color-scheme` do SO em tempo real (critério 3 da task 025) via
 * listener de `matchMedia`, registrado uma única vez no carregamento do módulo.
 */
export const useThemeStore = create<ThemeState>((set) => ({
  theme: readStoredTheme(),
  setTheme: (theme) => {
    window.localStorage.setItem(THEME_STORAGE_KEY, theme)
    applyThemeClass(theme)
    set({ theme })
  },
}))

if (typeof window !== 'undefined') {
  // Garante que o DOM reflita o estado inicial mesmo se o script inline do index.html não tiver
  // rodado (ex.: testes, SSR futuro) — idempotente com o que o script inline já aplicou.
  applyThemeClass(useThemeStore.getState().theme)

  window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', () => {
    if (useThemeStore.getState().theme === 'system') {
      applyThemeClass('system')
    }
  })
}
