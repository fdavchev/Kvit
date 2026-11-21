import { useSyncExternalStore } from 'react'
import { readIsWaitingAfterWakeVisit, subscribeToServerWake } from './serverWake'

export function useIsWaitingAfterWakeVisit(): boolean {
  return useSyncExternalStore(subscribeToServerWake, readIsWaitingAfterWakeVisit)
}
