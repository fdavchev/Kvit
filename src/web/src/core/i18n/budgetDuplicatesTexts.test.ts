import { describe, expect, it } from 'vitest'
import { ApiError } from '@/core/api/apiClient'
import { errorMessageKey } from '@/core/api/errors'
import { collectTexts } from '@/test/localeTexts'
import { translated } from '@/test/translated'
import en from './locales/en.json'
import mk from './locales/mk.json'

interface DuplicatesText {
  key: string
  en: string
  mk: string
}

const latinLetters = /[A-Za-zÀ-ɏ]/
const placeholders = /\{\{\w+\}\}/g
const duplicatesPrefix = 'budget.duplicates.'

const duplicatesTexts: DuplicatesText[] = [
  { key: 'budget.duplicates.title', en: 'Is this the same?', mk: 'Дали е ова исто?' },
  {
    key: 'budget.duplicates.body',
    en: 'Your “{{entry}}” ({{entryAmount}}, {{entryDay}}) and your share of “{{expense}}” in {{group}} ({{shareAmount}}, {{expenseDay}}).',
    mk: 'Вашиот „{{entry}}“ ({{entryAmount}}, {{entryDay}}) и вашиот дел од „{{expense}}“ во {{group}} ({{shareAmount}}, {{expenseDay}}).',
  },
  { key: 'budget.duplicates.same', en: 'Same, merge', mk: 'Исто, спој' },
  { key: 'budget.duplicates.new', en: "No, it's new", mk: 'Не, ново е' },
  { key: 'budget.duplicates.mergedWith', en: 'Merged with {{expense}} · {{group}}', mk: 'Споено со {{expense}} · {{group}}' },
  { key: 'budget.duplicates.mergedWithGroupExpense', en: 'Merged with a group expense', mk: 'Споено со групен трошок' },
  { key: 'budget.duplicates.notTheSame', en: 'Not the same', mk: 'Не е исто' },
  { key: 'budget.duplicates.countedAgain', en: 'Counted again in your budget', mk: 'Повторно се брои во вашиот буџет' },
]

const answerErrorCodes = ['DUPLICATE_ANSWER_INVALID', 'PERSONAL_ENTRY_NOT_FOUND', 'EXPENSE_NOT_FOUND', 'GUEST_NOT_ALLOWED']

function keysUnder(texts: object, prefix: string): string[] {
  return collectTexts(texts)
    .map(([path]) => path)
    .filter((key) => key.startsWith(prefix))
    .sort()
}

function placeholdersOf(text: string): string[] {
  return (text.match(placeholders) ?? []).sort()
}

function withoutPlaceholders(text: string): string {
  return text.replace(placeholders, '')
}

describe('the texts of the possible duplicates on the Budget tab', () => {
  it.each(duplicatesTexts)('has the English wording for $key', ({ key, en: english }) => {
    expect(translated('en', key)).toBe(english)
  })

  it.each(duplicatesTexts)('has the draft Macedonian wording for $key', ({ key, mk: macedonian }) => {
    expect(translated('mk', key)).toBe(macedonian)
  })

  it.each(duplicatesTexts)('writes the Macedonian text of $key with no Latin letters outside its placeholders', ({ key }) => {
    expect(withoutPlaceholders(translated('mk', key))).not.toMatch(latinLetters)
  })

  it.each(duplicatesTexts)('does not use the word «курс» in the Macedonian text of $key', ({ key }) => {
    expect(translated('mk', key).toLowerCase()).not.toContain('курс')
  })

  it.each(duplicatesTexts)('uses the same placeholders in the English and the Macedonian text of $key', ({ key }) => {
    expect(placeholdersOf(translated('mk', key))).toEqual(placeholdersOf(translated('en', key)))
  })

  it('has the same keys under budget.duplicates in English and in Macedonian', () => {
    expect(keysUnder(mk, duplicatesPrefix)).toEqual(keysUnder(en, duplicatesPrefix))
  })

  it('has exactly the pinned keys under budget.duplicates in both languages, so no unpinned text sneaks in', () => {
    const pinnedKeys = duplicatesTexts.map(({ key }) => key).sort()

    expect(keysUnder(en, duplicatesPrefix)).toEqual(pinnedKeys)
    expect(keysUnder(mk, duplicatesPrefix)).toEqual(pinnedKeys)
  })

  it('writes the sentence with the seven names and amounts it needs, in English and in Macedonian', () => {
    const wanted = ['{{entry}}', '{{entryAmount}}', '{{entryDay}}', '{{expense}}', '{{group}}', '{{shareAmount}}', '{{expenseDay}}'].sort()

    expect(placeholdersOf(translated('en', 'budget.duplicates.body'))).toEqual(wanted)
    expect(placeholdersOf(translated('mk', 'budget.duplicates.body'))).toEqual(wanted)
  })

  it('writes the English quotes around the names as “name” and the Macedonian ones as „name“', () => {
    expect(translated('en', 'budget.duplicates.body', { entry: 'Dinner', expense: 'Dinner' })).toContain('“Dinner”')
    expect(translated('mk', 'budget.duplicates.body', { entry: 'Вечера', expense: 'Вечера' })).toContain('„Вечера“')
  })

  it('keeps the texts out of budget.screen, whose keys are pinned exactly by another test', () => {
    expect(keysUnder(en, 'budget.screen.').filter((key) => /duplicate|merge/i.test(key))).toEqual([])
  })

  it('reuses the existing texts for Save and "Saved" instead of adding new ones', () => {
    for (const key of ['groupSettings.save', 'groupSettings.saved', 'expenses.today', 'expenses.yesterday']) {
      expect(translated('en', key)).not.toBe('')
      expect(translated('mk', key)).not.toBe('')
    }
    expect(keysUnder(en, duplicatesPrefix).filter((key) => /save|saved|today|yesterday/i.test(key))).toEqual([])
  })
})

describe('the error codes of the duplicate answers', () => {
  it.each(answerErrorCodes)('has a translation for errors.%s in English and in Macedonian', (code) => {
    expect(translated('en', `errors.${code}`)).not.toBe('')
    expect(translated('mk', `errors.${code}`)).not.toBe('')
  })

  it.each(answerErrorCodes)('maps %s to its own translation key', (code) => {
    const error = new ApiError('PUT /api/personal/duplicate-answers failed with status 400', {
      httpStatus: 400,
      errorCode: code,
    })

    expect(errorMessageKey(error)).toBe(`errors.${code}`)
  })
})
