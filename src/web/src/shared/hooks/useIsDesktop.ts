import { useSyncExternalStore } from 'react'

export const desktopQuery = '(min-width: 64rem)'

function subscribeToDesktopQuery(onChange: () => void): () => void {
  const queryList = window.matchMedia(desktopQuery)
  queryList.addEventListener('change', onChange)
  return () => queryList.removeEventListener('change', onChange)
}

function isDesktopNow(): boolean {
  return window.matchMedia(desktopQuery).matches
}

export function useIsDesktop(): boolean {
  return useSyncExternalStore(subscribeToDesktopQuery, isDesktopNow)
}
