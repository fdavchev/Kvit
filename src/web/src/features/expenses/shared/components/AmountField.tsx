import { useId } from 'react'
import { useTranslation } from 'react-i18next'
import type { Currency } from '@/shared/utils/formatMoney'

interface AmountFieldProps {
  amountText: string
  currency: Currency
  onAmountTextChange: (amountText: string) => void
  onCurrencyChange: (currency: Currency) => void
}

const otherCurrency: Record<Currency, Currency> = { MKD: 'EUR', EUR: 'MKD' }

export function AmountField({ amountText, currency, onAmountTextChange, onCurrencyChange }: AmountFieldProps) {
  const { t } = useTranslation()
  const amountId = useId()

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
        onChange={(event) => onAmountTextChange(event.target.value)}
        className="w-full min-w-0 flex-1 rounded-[14px] bg-transparent py-1 text-right text-5xl font-extrabold tracking-[-0.02em] text-foreground placeholder:text-muted-foreground/60 focus-visible:outline-offset-0"
      />
      <button
        type="button"
        aria-label={t('expense.currencyToggle', { currency })}
        onClick={() => onCurrencyChange(otherCurrency[currency])}
        className="pressable min-h-11 flex-none rounded-full border border-field-border bg-field px-3 text-lg font-bold text-muted-foreground"
      >
        {currency}
      </button>
    </div>
  )
}
