import type { RefObject } from 'react'
import { useElementHeightOnPage } from './useElementHeightOnPage'

const toastLiftProperty = '--toast-lift'

export function useToastsAboveElement(ref: RefObject<HTMLElement | null>, gapPx: number = 12): void {
  useElementHeightOnPage(ref, toastLiftProperty, gapPx)
}
