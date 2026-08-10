import {useSettingStore} from '@/stores/setting'

export type ResolvedTheme = 'light' | 'dark'
export type ThemeMode = ResolvedTheme | 'auto'
export type ThemeStyle = 'editorial' | 'zen' | 'ink'

let systemListenerRegistered = false

function getSystemTheme(): ResolvedTheme {
  return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light'
}

export default function useTheme() {
  const settingStore = useSettingStore()

  function applyTheme() {
    const resolvedTheme = settingStore.theme === 'auto' ? getSystemTheme() : settingStore.theme
    const root = document.documentElement

    root.dataset.theme = resolvedTheme
    root.dataset.themeStyle = settingStore.themeStyle
    root.classList.toggle('dark', resolvedTheme === 'dark')
    root.classList.toggle('light', resolvedTheme === 'light')
    root.style.colorScheme = resolvedTheme
  }

  if (!systemListenerRegistered) {
    window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', () => {
      if (settingStore.theme === 'auto') applyTheme()
    })
    systemListenerRegistered = true
  }

  function setTheme(value: ThemeMode) {
    settingStore.theme = value
    applyTheme()
  }

  function setThemeStyle(value: ThemeStyle) {
    settingStore.themeStyle = value
    applyTheme()
  }

  function toggleTheme() {
    setTheme(getTheme() === 'dark' ? 'light' : 'dark')
  }

  function getTheme(): ResolvedTheme {
    return settingStore.theme === 'auto' ? getSystemTheme() : settingStore.theme
  }

  function getThemeSetting(): ThemeMode {
    return settingStore.theme
  }

  function getThemeStyle(): ThemeStyle {
    return settingStore.themeStyle
  }

  applyTheme()

  return {
    applyTheme,
    toggleTheme,
    setTheme,
    setThemeStyle,
    getTheme,
    getThemeSetting,
    getThemeStyle
  }
}
