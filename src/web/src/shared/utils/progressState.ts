export type ProgressState = 'ok' | 'near' | 'over'

const nearShareNumerator = 4
const nearShareDenominator = 5

export function progressStateOf(spentMinor: number, limitMinor: number): ProgressState {
  if (spentMinor > limitMinor) {
    return 'over'
  }
  return spentMinor * nearShareDenominator >= limitMinor * nearShareNumerator ? 'near' : 'ok'
}

export function progressFillRatio(spentMinor: number, limitMinor: number): number {
  if (limitMinor <= 0) {
    throw new Error(`Expected a limit above 0 for a progress bar, got ${limitMinor}`)
  }
  return Math.min(spentMinor, limitMinor) / limitMinor
}
