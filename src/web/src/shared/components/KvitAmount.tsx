import NumberFlow from '@number-flow/react'
import { moneyText, type MoneyParts } from '@/shared/utils/formatMoney'
import { intlLocaleWithTheSameSeparatorsAs } from '@/shared/utils/formatNumber'

export const amountRollDurationMs = 250

const rollTiming: EffectTiming = { duration: amountRollDurationMs, easing: 'ease-out' }

interface KvitAmountProps {
  parts: MoneyParts
}

export function KvitAmount({ parts }: KvitAmountProps) {
  const fractionDigits = parts.unsignedDecimal.split('.')[1]?.length ?? 0

  return (
    <>
      <span className="sr-only">{moneyText(parts)}</span>
      <NumberFlow
        aria-hidden="true"
        className="tabular-nums"
        value={parts.unsignedDecimal as `${number}`}
        locales={intlLocaleWithTheSameSeparatorsAs(parts.language)}
        format={{ minimumFractionDigits: fractionDigits, maximumFractionDigits: fractionDigits }}
        prefix={`${parts.sign}${parts.currencyBefore}`}
        suffix={parts.currencyAfter}
        transformTiming={rollTiming}
        opacityTiming={rollTiming}
      />
    </>
  )
}
