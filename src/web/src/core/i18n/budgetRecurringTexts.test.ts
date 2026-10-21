import { describe, expect, it } from 'vitest'
import { collectTexts } from '@/test/localeTexts'
import { translated } from '@/test/translated'
import en from './locales/en.json'
import mk from './locales/mk.json'

interface RecurringText {
  key: string
  en: string
  placeholders: string[]
}

const latinLetters = /[A-Za-zÀ-ɏ]/
const placeholderPattern = /\{\{[^}]+\}\}/g

function text(key: string, english: string, placeholders: string[] = []): RecurringText {
  return { key, en: english, placeholders }
}

const incomeSheetTexts: RecurringText[] = [
  text('budget.income.repeatsNever', 'Repeats: Never'),
  text('budget.income.repeatsMonthly', 'Repeats: Monthly'),
  text('budget.income.repeatsWeekly', 'Repeats: Weekly'),
  text('budget.income.startsOn', 'Starts: {{day}}', ['{{day}}']),
  text('budget.income.endsNever', 'Ends: never'),
  text('budget.income.endsOn', 'Ends: {{day}}', ['{{day}}']),
  text('budget.income.endsTitle', 'When does it end?'),
  text('budget.income.endsOptionNever', 'Never'),
  text('budget.income.endsIn3Months', 'In 3 months'),
  text('budget.income.endsIn6Months', 'In 6 months'),
  text('budget.income.endsIn1Year', 'In 1 year'),
  text('budget.income.endsPickDate', 'Pick a date…'),
  text('budget.income.startDateHint', 'Pick a start date from the last 24 months, up to one year ahead.'),
  text('budget.income.endDateHint', 'Pick an end date from the start, up to 10 years later.'),
]

const weekdayTexts: RecurringText[] = [
  text('budget.recurring.weekdays.sunday', 'Sunday'),
  text('budget.recurring.weekdays.monday', 'Monday'),
  text('budget.recurring.weekdays.tuesday', 'Tuesday'),
  text('budget.recurring.weekdays.wednesday', 'Wednesday'),
  text('budget.recurring.weekdays.thursday', 'Thursday'),
  text('budget.recurring.weekdays.friday', 'Friday'),
  text('budget.recurring.weekdays.saturday', 'Saturday'),
]

const listTexts: RecurringText[] = [
  text('budget.recurring.title', 'Repeating income'),
  text('budget.recurring.rowCount', 'Repeating income · {{count}}', ['{{count}}']),
  text('budget.recurring.rowNone', 'Repeating income · none yet'),
  text('budget.recurring.more_one', '+{{count}} more', ['{{count}}']),
  text('budget.recurring.more_other', '+{{count}} more', ['{{count}}']),
  text('budget.recurring.monthly', 'Every month on the {{ordinal}}', ['{{ordinal}}']),
  text('budget.recurring.monthlyMonthEnd', 'Every month on the {{ordinal}} (or the last day of shorter months)', ['{{ordinal}}']),
  text('budget.recurring.weekly', 'Every week on {{weekday}}', ['{{weekday}}']),
  text('budget.recurring.next', 'next {{day}}', ['{{day}}']),
  text('budget.recurring.stop', 'Stop'),
  text('budget.recurring.stopped', 'Stopped'),
  text('budget.recurring.empty', 'No repeating income yet'),
  text('budget.recurring.newButton', '+ New repeating income'),
  ...weekdayTexts,
]

const newTexts = [...incomeSheetTexts, ...listTexts]

function withoutPlaceholders(value: string): string {
  return value.replace(placeholderPattern, '')
}

function keysUnder(texts: object, prefix: string): string[] {
  return collectTexts(texts)
    .map(([path]) => path)
    .filter((key) => key.startsWith(prefix))
    .sort()
}

describe('the texts of repeating income', () => {
  it.each(newTexts)('has the English wording for $key', ({ key, en: english }) => {
    expect(translated('en', key)).toBe(english)
  })

  it.each(newTexts)('has a Macedonian text for $key that is not the English one', ({ key, en: english }) => {
    expect(translated('mk', key)).not.toBe('')
    expect(translated('mk', key)).not.toBe(english)
  })

  it.each(newTexts)('writes the Macedonian text of $key with no Latin letters outside its placeholders', ({ key }) => {
    expect(withoutPlaceholders(translated('mk', key))).not.toMatch(latinLetters)
  })

  it.each(newTexts.filter(({ placeholders }) => placeholders.length > 0))(
    'keeps the placeholders $placeholders of $key in the Macedonian text',
    ({ key, placeholders }) => {
      for (const placeholder of placeholders) {
        expect(translated('mk', key)).toContain(placeholder)
      }
    },
  )

  it('shows "+1 more" for one and "+3 more" for three in English', () => {
    expect(translated('en', 'budget.recurring.more_one', { count: '1' })).toBe('+1 more')
    expect(translated('en', 'budget.recurring.more_other', { count: '3' })).toBe('+3 more')
  })

  it('writes the hints of the rhythm the way the list and the sheet show them', () => {
    expect(translated('en', 'budget.recurring.monthly', { ordinal: '20th' })).toBe('Every month on the 20th')
    expect(translated('en', 'budget.recurring.weekly', { weekday: 'Tuesday' })).toBe('Every week on Tuesday')
  })

  it('adds the month-end note to the rhythm of the 31st in English', () => {
    expect(translated('en', 'budget.recurring.monthlyMonthEnd', { ordinal: '31st' })).toBe(
      'Every month on the 31st (or the last day of shorter months)',
    )
  })

  it('keeps the Macedonian rhythm of the month end as the monthly rhythm plus a note', () => {
    const monthly = translated('mk', 'budget.recurring.monthly', { ordinal: '31-ви' })
    const monthEnd = translated('mk', 'budget.recurring.monthlyMonthEnd', { ordinal: '31-ви' })

    expect(monthEnd.startsWith(monthly)).toBe(true)
    expect(monthEnd.length).toBeGreaterThan(monthly.length)
  })

  it('names the seven weekdays of Macedonian, Sunday first', () => {
    expect(weekdayTexts.map(({ key }) => translated('mk', key))).toEqual([
      'Недела',
      'Понеделник',
      'Вторник',
      'Среда',
      'Четврток',
      'Петок',
      'Сабота',
    ])
  })

  it('has exactly the seven weekday names under budget.recurring.weekdays, in English and in Macedonian', () => {
    const pinnedKeys = weekdayTexts.map(({ key }) => key).sort()

    expect(keysUnder(en, 'budget.recurring.weekdays.')).toEqual(pinnedKeys)
    expect(keysUnder(mk, 'budget.recurring.weekdays.')).toEqual(pinnedKeys)
  })

  it('has at least the pinned texts under budget.income and budget.recurring, in English and in Macedonian', () => {
    const incomeKeys = incomeSheetTexts.map(({ key }) => key)
    const recurringKeys = listTexts.map(({ key }) => key)

    expect(keysUnder(en, 'budget.income.')).toEqual(expect.arrayContaining(incomeKeys))
    expect(keysUnder(mk, 'budget.income.')).toEqual(expect.arrayContaining(incomeKeys))
    expect(keysUnder(en, 'budget.recurring.')).toEqual(expect.arrayContaining(recurringKeys))
    expect(keysUnder(mk, 'budget.recurring.')).toEqual(expect.arrayContaining(recurringKeys))
  })

  it('has the same keys under budget.income and budget.recurring in both languages', () => {
    expect(keysUnder(mk, 'budget.income.')).toEqual(keysUnder(en, 'budget.income.'))
    expect(keysUnder(mk, 'budget.recurring.')).toEqual(keysUnder(en, 'budget.recurring.'))
  })

  it('still has the texts of adding income once under budget.income', () => {
    expect(translated('en', 'budget.income.title')).toBe('New income')
    expect(translated('en', 'budget.income.listTitle')).toBe('Income')
  })

  it('reuses the existing Edit, Undo, Saved and Restored texts instead of new ones', () => {
    expect(translated('en', 'expense.edit')).toBe('Edit')
    expect(translated('en', 'common.undo')).toBe('Undo')
    expect(translated('en', 'groupSettings.saved')).toBeTruthy()
    expect(translated('en', 'budget.entries.restored')).toBe('Restored')
  })
})
