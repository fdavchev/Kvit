import { useSyncExternalStore } from 'react'
import { readOfferedWakeUrl, subscribeToWakeOffer } from './serverWake'

export function useOfferedWakeUrl(): string | null {
  return useSyncExternalStore(subscribeToWakeOffer, readOfferedWakeUrl)
}
