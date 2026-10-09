import { useId } from 'react'
import { useTranslation } from 'react-i18next'
import type { Currency } from '@/shared/utils/formatMoney'
import { parseMoneyInput } from '@/shared/utils/parseMoneyInput'

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
  const isAmountInvalid = amountText !== '' && parseMoneyInput(amountText, currency) === null

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
        className="-mx-0.5 w-full min-w-0 flex-1 rounded-[14px] border-2 border-transparent bg-transparent py-0.5 text-right text-5xl font-extrabold tracking-[-0.02em] text-foreground placeholder:text-muted-foreground/60 focus-visible:outline-offset-0 aria-invalid:border-destructive"
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
