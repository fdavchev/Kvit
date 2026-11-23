import { describe, expect, it } from 'vitest'
import { collectTexts } from '@/test/localeTexts'
import { translated } from '@/test/translated'
import mk from './locales/mk.json'

const latinLetters = /[A-Za-zÀ-ɏ]/
const placeholders = /\{\{\w+\}\}/g

const currencyErrorKeys = [
  'errors.GROUP_CURRENCY_INVALID',
  'errors.EXPENSE_CURRENCY_INVALID',
  'errors.PERSONAL_ENTRY_CURRENCY_INVALID',
  'errors.RECURRING_INCOME_CURRENCY_INVALID',
  'errors.SETTLEMENT_CURRENCY_INVALID',
]

const fix21Step5Texts: [string, string, string][] = [
  ...currencyErrorKeys.map((key): [string, string, string] => [key, 'Pick MKD or EUR.', 'Избери денари или евра.']),
  [
    'errors.GROUP_PLAN_AMOUNT_INVALID',
    'Type a plan amount above 0 (whole denars for MKD).',
    'Внеси износ за планот поголем од 0 (цели денари).',
  ],
  ['expense.rate', 'Rate: 1 EUR = {{rate}} MKD · {{date}}', 'Цена на еврото: 1 EUR = {{rate}} ден. · {{date}}'],
  ['groupPlan.reached', 'Plan reached', 'Планот е достигнат'],
  ['members.nameJustTaken', 'Someone just took this name, or it was removed.', 'Некој штотуку го зеде ова име или е отстрането.'],
  ['oneBill.ownName', "You're already in this bill.", 'Ти веќе си во сметката.'],
  ['activity.expenseRestoredWithTitle', '{{name}} restored “{{title}}”', '{{name}} го врати „{{title}}“'],
  [
    'ownerTools.groups.numberInvalid',
    "That's not a group number. Copy the whole number from the report email.",
    'Ова не е број на група. Копирај го целиот број од е-поштата за пријава.',
  ],
  ['ownerTools.people.tooShort', 'Type at least 2 letters.', 'Внеси барем 2 букви.'],
  ['ownerTools.reasonTooLong', 'At most 500 characters.', 'Најмногу 500 знаци.'],
]

describe('the Fix 21 Step 5 texts', () => {
  it.each(fix21Step5Texts)('has the English and the draft Macedonian wording for %s', (key, english, macedonian) => {
    expect(translated('en', key)).toBe(english)
    expect(translated('mk', key)).toBe(macedonian)
  })

  it.each(fix21Step5Texts)('uses the same placeholders in the English and the Macedonian text of %s', (key) => {
    expect((translated('en', key).match(placeholders) ?? []).sort()).toEqual(
      (translated('mk', key).match(placeholders) ?? []).sort(),
    )
  })

  it.each(fix21Step5Texts.filter(([key]) => key !== 'expense.rate'))(
    'writes the Macedonian text of %s with Cyrillic letters only apart from the placeholders',
    (key) => {
      expect(translated('mk', key).replace(placeholders, '')).not.toMatch(latinLetters)
    },
  )

  it('never writes the code MKD in a Macedonian error text, because Macedonian amounts say «ден.»', () => {
    const errorsWithTheCode = collectTexts(mk)
      .filter(([path, text]) => path.startsWith('errors.') && typeof text === 'string' && text.includes('MKD'))
      .map(([path]) => path)

    expect(errorsWithTheCode).toEqual([])
  })

  it('writes «ден.» and not MKD after the euro rate in Macedonian, and keeps MKD in English', () => {
    expect(translated('mk', 'expense.rate', { rate: '61,5000', date: '24 сеп 2026' })).toBe(
      'Цена на еврото: 1 EUR = 61,5000 ден. · 24 сеп 2026',
    )
    expect(translated('en', 'expense.rate', { rate: '61.5000', date: '24 Sep 2026' })).toBe(
      'Rate: 1 EUR = 61.5000 MKD · 24 Sep 2026',
    )
  })

  it('names the restored expense in Macedonian quotes „…“ and in English quotes “…”', () => {
    expect(translated('mk', 'activity.expenseRestoredWithTitle', { name: 'Ана', title: 'Такси' })).toBe('Ана го врати „Такси“')
    expect(translated('en', 'activity.expenseRestoredWithTitle', { name: 'Ana', title: 'Taxi' })).toBe('Ana restored “Taxi”')
  })
})
