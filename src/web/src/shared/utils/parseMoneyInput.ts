import type { Currency } from './formatMoney'

const maxAmountMinor = 999_999_999_999
const maxWholeDigits = 10
const wholeNumberPattern = /^(\d+)$/
const twoDecimalsPattern = /^(\d+)(?:[.,](\d{1,2}))?$/
const hundredthsPerWhole = 100

export function parseMoneyInput(text: string, currency: Currency): number | null {
  const minor = parseHundredths(text, currency === 'EUR')
  return minor === null || minor === 0 ? null : minor
}

export function parseHundredths(text: string, allowsDecimals: boolean): number | null {
  const match = (allowsDecimals ? twoDecimalsPattern : wholeNumberPattern).exec(text.trim())
  if (match === null) {
    return null
  }
  const wholeDigits = match[1].replace(/^0+(?=\d)/, '')
  if (wholeDigits.length > maxWholeDigits) {
    return null
  }
  const fractionDigits = (match[2] ?? '').padEnd(2, '0')
  const hundredths = Number(wholeDigits) * hundredthsPerWhole + Number(fractionDigits)
  return hundredths > maxAmountMinor ? null : hundredths
}

export function hundredthsToText(hundredths: number, keepsTwoDecimals: boolean): string {
  if (!Number.isSafeInteger(hundredths) || hundredths < 0) {
    throw new Error(`Expected a whole number of hundredths of 0 or more, got ${hundredths}`)
  }
  const digits = String(hundredths).padStart(3, '0')
  const whole = digits.slice(0, -2)
  const fraction = digits.slice(-2)
  if (fraction === '00') {
    return whole
  }
  return `${whole}.${keepsTwoDecimals ? fraction : fraction.replace(/0$/, '')}`
}

export function moneyInputText(amountMinor: number, currency: Currency): string {
  const text = hundredthsToText(amountMinor, true)
  if (currency === 'MKD' && text.includes('.')) {
    throw new Error(`MKD amount must be whole denars (a multiple of 100 deni), got ${amountMinor}`)
  }
  return text
}
