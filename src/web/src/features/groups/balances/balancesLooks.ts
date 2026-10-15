export const balancesSectionHeadingLook =
  'mx-1 mt-1.5 mb-2 text-[0.8125rem] font-bold tracking-[0.04em] text-muted-foreground uppercase'

export const balancesCardLook = 'rounded-[20px] bg-card px-4 py-3.5 text-card-foreground'

export const balancesRowLook = 'flex flex-wrap items-center'

export const balancesRowTextLook = 'flex min-w-0 flex-1 basis-40 flex-col'

export const balancesActionButtonLook =
  'min-h-11 w-auto min-w-28 flex-none rounded-full px-3.5 text-[0.9375rem] whitespace-nowrap'

export const balancesTextButtonLook = 'min-h-11 w-auto flex-none px-2 text-[0.9375rem]'

export function amountLookOf(amountMinor: number): string {
  if (amountMinor > 0) {
    return 'text-positive'
  }
  if (amountMinor < 0) {
    return 'text-negative'
  }
  return ''
}
