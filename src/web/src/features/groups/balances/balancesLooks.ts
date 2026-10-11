export const balancesSectionHeadingLook =
  'mx-1 mt-1.5 mb-2 text-[0.8125rem] font-bold tracking-[0.04em] text-muted-foreground uppercase'

export const balancesCardLook = 'rounded-[20px] bg-card px-4 py-3.5 text-card-foreground'

export function amountLookOf(amountMinor: number): string {
  if (amountMinor > 0) {
    return 'text-positive'
  }
  if (amountMinor < 0) {
    return 'text-negative'
  }
  return ''
}
