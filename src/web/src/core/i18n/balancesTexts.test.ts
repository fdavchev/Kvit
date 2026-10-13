import { describe, expect, it } from 'vitest'
import { collectTexts } from '@/test/localeTexts'
import { translated } from '@/test/translated'
import en from './locales/en.json'

const placeholders = /\{\{\w+\}\}/g

const englishSettlementSentences: [string, string][] = [
  ['activity.settlementRecorded', '{{name}} recorded a payment: {{from}} paid {{to}} {{amount}}'],
  ['activity.settlementConfirmed', '{{name}} confirmed that {{from}} paid {{to}} {{amount}}'],
  ['activity.settlementRejected', "{{name}} rejected {{from}}'s payment of {{amount}} to {{to}}"],
  ['activity.settlementCancelled', '{{name}} cancelled the payment of {{amount}} to {{to}}'],
  ['activity.settlementDeleted', '{{name}} deleted the payment of {{amount}} from {{from}} to {{to}}'],
  ['activity.settlementRestored', '{{name}} restored the payment of {{amount}} from {{from}} to {{to}}'],
]

const englishBalancesTexts: [string, string][] = [
  ['balances.title', 'Balances'],
  ['balances.empty', 'No expenses yet, so nothing to settle.'],
]

const newKeysInEnglish: string[] = collectTexts(en)
  .map(([path]) => path)
  .filter((path) => path.startsWith('balances.') || path.startsWith('activity.settlement'))

const newKeysWithEnglishWords: string[] = newKeysInEnglish.filter((key) =>
  /[A-Za-z]/.test(translated('en', key).replace(placeholders, '')),
)

describe('the texts of the Balances tab and of the payment sentences in the activity', () => {
  it.each([...englishSettlementSentences.map(([key]) => key), ...englishBalancesTexts.map(([key]) => key)])(
    'has a text for %s in English and in Macedonian',
    (key) => {
      expect(translated('en', key)).not.toBe('')
      expect(translated('mk', key)).not.toBe('')
    },
  )

  it.each([...englishSettlementSentences, ...englishBalancesTexts])(
    'has the approved English wording for %s',
    (key, english) => {
      expect(translated('en', key)).toBe(english)
    },
  )

  it('has more texts under balances than the title and the empty message', () => {
    const balancesKeys = newKeysInEnglish.filter((key) => key.startsWith('balances.'))

    expect(balancesKeys.length).toBeGreaterThan(englishBalancesTexts.length)
  })

  it.each(newKeysInEnglish)('has a Macedonian text for %s', (key) => {
    expect(translated('mk', key)).not.toBe('')
  })

  it.each(newKeysInEnglish)('uses the same placeholders in the English and the Macedonian text of %s', (key) => {
    const english = translated('en', key).match(placeholders) ?? []
    const macedonian = translated('mk', key).match(placeholders) ?? []

    expect(macedonian.sort()).toEqual(english.sort())
  })

  it.each(newKeysWithEnglishWords)('has no Macedonian text under %s that is still the English one', (key) => {
    expect(translated('mk', key)).not.toBe(translated('en', key))
  })
})
