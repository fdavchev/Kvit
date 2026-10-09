import type { Language } from '@/core/i18n/language'

interface NumberSeparators {
  thousands: string
  decimal: string
}

const numberSeparators: Record<Language, NumberSeparators> = {
  en: { thousands: ',', decimal: '.' },
  mk: { thousands: '.', decimal: ',' },
}

const plainDecimalPattern = /^(-?)(\d+)(?:\.(\d+))?$/
const digitsBeforeEachThousandsGroup = /\B(?=(\d{3})+$)/g

export function formatGroupedDecimal(plainDecimal: string, language: Language): string {
  const { sign, whole, fraction } = readPlainDecimal(plainDecimal)
  const separators = numberSeparators[language]
  const groupedWhole = whole.replace(digitsBeforeEachThousandsGroup, separators.thousands)
  return `${sign}${groupedWhole}${fractionText(fraction, separators)}`
}

export function formatUngroupedDecimal(plainDecimal: string, language: Language): string {
  const { sign, whole, fraction } = readPlainDecimal(plainDecimal)
  return `${sign}${whole}${fractionText(fraction, numberSeparators[language])}`
}

interface PlainDecimal {
  sign: string
  whole: string
  fraction: string | undefined
}

function readPlainDecimal(plainDecimal: string): PlainDecimal {
  const match = plainDecimalPattern.exec(plainDecimal)
  if (match === null) {
    throw new Error(`Expected a plain decimal number like -1234.50, got "${plainDecimal}"`)
  }
  return { sign: match[1], whole: match[2], fraction: match[3] }
}

function fractionText(fraction: string | undefined, separators: NumberSeparators): string {
  return fraction === undefined ? '' : `${separators.decimal}${fraction}`
}
