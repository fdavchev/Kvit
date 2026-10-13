import { describe, expect, it } from 'vitest'
import { collectTexts } from '@/test/localeTexts'
import { keyOfEnglishText } from '@/test/englishTextKeys'
import { translated } from '@/test/translated'
import en from './locales/en.json'

const placeholders = /\{\{\w+\}\}/g

const pinnedEnglishTexts: string[] = [
  'Mark paid',
  'I paid {{name}}',
  '{{name}} paid me',
  '{{from}} paid {{to}}',
  'Waits until {{name}} confirms. You can cancel until then.',
  'Counts right away.',
  'Record another payment',
  'I paid',
  'Someone paid me',
  'Payments',
  'Payment deleted',
  'Confirm',
  'Reject',
  'Cancel',
  'Delete',
  'Save',
  'Undo',
]

const settlementsKeys: string[] = collectTexts(en)
  .map(([path]) => path)
  .filter((path) => path.startsWith('settlements.'))

const textsWithEnglishWords: string[] = pinnedEnglishTexts.filter((text) =>
  /[A-Za-z]/.test(text.replace(placeholders, '')),
)

describe('the texts of the payment actions', () => {
  it.each(pinnedEnglishTexts)('has the English text "%s" under some key', (text) => {
    expect(keyOfEnglishText(text)).not.toBe('')
  })

  it.each(pinnedEnglishTexts)('has a Macedonian text for the key of "%s"', (text) => {
    expect(translated('mk', keyOfEnglishText(text))).not.toBe('')
  })

  it.each(pinnedEnglishTexts)('uses the same placeholders in the English and the Macedonian text of "%s"', (text) => {
    const key = keyOfEnglishText(text)

    const english = translated('en', key).match(placeholders) ?? []
    const macedonian = translated('mk', key).match(placeholders) ?? []

    expect(macedonian.sort()).toEqual(english.sort())
  })

  it.each(textsWithEnglishWords)('has a Macedonian text for "%s" that is not the English one', (text) => {
    const key = keyOfEnglishText(text)

    expect(translated('mk', key)).not.toBe(translated('en', key))
  })

  it('has a Macedonian text with the same placeholders and not the English one for every key under settlements', () => {
    const problems = settlementsKeys.filter((key) => {
      const english = translated('en', key)
      const macedonian = translated('mk', key)
      const samePlaceholders =
        JSON.stringify((macedonian.match(placeholders) ?? []).sort()) ===
        JSON.stringify((english.match(placeholders) ?? []).sort())
      const hasEnglishWords = /[A-Za-z]/.test(english.replace(placeholders, ''))
      return !samePlaceholders || (hasEnglishWords && macedonian === english)
    })

    expect(problems).toEqual([])
  })
})
