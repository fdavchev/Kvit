import type { RefObject } from 'react'
import { useElementHeightOnPage } from './useElementHeightOnPage'

const toastLiftProperty = '--toast-lift'
export const openSheetHeightProperty = '--open-sheet-height'
const gapAboveSheetPx = 12

export function useToastsAboveElement(ref: RefObject<HTMLElement | null>, gapPx: number = 12): void {
  useElementHeightOnPage(ref, toastLiftProperty, gapPx)
}

export function useToastsAboveSheet(sheetRef: RefObject<HTMLElement | null>): void {
  useElementHeightOnPage(sheetRef, openSheetHeightProperty, gapAboveSheetPx)
}
