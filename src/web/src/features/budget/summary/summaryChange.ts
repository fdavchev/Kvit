export type SummaryChange = { direction: 'up' | 'down'; percent: number } | { direction: 'new' } | { direction: 'same' }

export type SummaryDirection = SummaryChange['direction']

export function summaryChangeOf(thisMinor: number, previousMinor: number): SummaryChange {
  if (thisMinor === previousMinor) {
    return { direction: 'same' }
  }
  if (previousMinor === 0) {
    return { direction: 'new' }
  }
  const differenceMinor = Math.abs(thisMinor - previousMinor)
  const percent = Math.floor((differenceMinor * 200 + previousMinor) / (previousMinor * 2))
  return { direction: thisMinor > previousMinor ? 'up' : 'down', percent }
}
