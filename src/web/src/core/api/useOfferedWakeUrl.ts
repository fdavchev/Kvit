import { useSyncExternalStore } from 'react'
import { readOfferedWakeUrl, subscribeToServerWake } from './serverWake'

export function useOfferedWakeUrl(): string | null {
  return useSyncExternalStore(subscribeToServerWake, readOfferedWakeUrl)
}
