import { useSyncExternalStore } from 'react'
import { applyTheme } from './applyTheme'
import { readSavedTheme, saveTheme } from './savedTheme'
import type { ThemeChoice } from './theme'

interface UseThemeResult {
  theme: ThemeChoice
  setTheme: (choice: ThemeChoice) => void
}

const listeners = new Set<() => void>()

function subscribe(listener: () => void): () => void {
  listeners.add(listener)
  return () => {
    listeners.delete(listener)
  }
}

function setTheme(choice: ThemeChoice): void {
  saveTheme(choice)
  applyTheme(choice)
  listeners.forEach((listener) => listener())
}

export function useTheme(): UseThemeResult {
  const theme = useSyncExternalStore(subscribe, readSavedTheme)
  return { theme, setTheme }
}
