import { useSyncExternalStore } from 'react'
import type { ColorScheme } from './colorScheme'
import type { ThemeChoice } from './theme'

const darkSchemeQuery = '(prefers-color-scheme: dark)'

function subscribeToDeviceScheme(onChange: () => void): () => void {
  const queryList = window.matchMedia(darkSchemeQuery)
  queryList.addEventListener('change', onChange)
  return () => queryList.removeEventListener('change', onChange)
}

function deviceSchemeNow(): ColorScheme {
  return window.matchMedia(darkSchemeQuery).matches ? 'dark' : 'light'
}

export function useColorScheme(choice: ThemeChoice): ColorScheme {
  const deviceScheme = useSyncExternalStore(subscribeToDeviceScheme, deviceSchemeNow)
  return choice === 'system' ? deviceScheme : choice
}
