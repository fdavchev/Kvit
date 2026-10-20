import { useId } from 'react'
import { useTranslation } from 'react-i18next'
import { useLanguage } from '@/core/i18n/useLanguage'
import { cn } from '@/shared/utils/cn'
import { currencyLabel, type Currency } from '@/shared/utils/formatMoney'
import { parseMoneyInput } from '@/shared/utils/parseMoneyInput'

interface AmountFieldProps {
  amountText: string
  currency: Currency
  onAmountTextChange: (amountText: string) => void
  onCurrencyChange?: (currency: Currency) => void
  invalid?: boolean
}

const otherCurrency: Record<Currency, Currency> = { MKD: 'EUR', EUR: 'MKD' }

const longestTextAtBiggestSize = 7
const longestTextAtMiddleSize = 10

function amountTextSize(amountText: string): string {
  if (amountText.length <= longestTextAtBiggestSize) {
    return 'text-5xl'
  }
  if (amountText.length <= longestTextAtMiddleSize) {
    return 'text-4xl'
  }
  return 'text-3xl'
}

export function AmountField({ amountText, currency, onAmountTextChange, onCurrencyChange, invalid = false }: AmountFieldProps) {
  const { t } = useTranslation()
  const { language } = useLanguage()
  const amountId = useId()
  const shownCurrency = currencyLabel(currency, language)
  const isAmountInvalid = invalid || (amountText !== '' && parseMoneyInput(amountText, currency) === null)

  return (
    <div className="flex items-center justify-center gap-2 pt-2">
      <label htmlFor={amountId} className="sr-only">
        {t('expense.amount')}
      </label>
      <input
        id={amountId}
        type="text"
        inputMode={currency === 'EUR' ? 'decimal' : 'numeric'}
        autoComplete="off"
        autoFocus
        placeholder="0"
        value={amountText}
        aria-invalid={isAmountInvalid}
        onChange={(event) => onAmountTextChange(event.target.value)}
        className={cn(
          '-mx-0.5 w-full min-w-0 flex-1 rounded-[14px] border-2 border-transparent bg-transparent px-3 py-0.5 font-extrabold tracking-[-0.02em] text-foreground placeholder:text-muted-foreground focus-visible:outline-offset-0 aria-invalid:border-destructive',
          amountTextSize(amountText),
          onCurrencyChange === undefined ? 'text-left' : 'text-right',
        )}
      />
      {onCurrencyChange === undefined ? (
        <span className="flex-none px-1 text-lg font-bold text-muted-foreground">{shownCurrency}</span>
      ) : (
        <button
          type="button"
          aria-label={t('expense.currencyToggle', { currency: shownCurrency })}
          onClick={() => onCurrencyChange(otherCurrency[currency])}
          className="pressable min-h-11 flex-none rounded-full border border-field-border bg-field px-3 text-lg font-bold text-muted-foreground"
        >
          {shownCurrency}
        </button>
      )}
    </div>
  )
}
