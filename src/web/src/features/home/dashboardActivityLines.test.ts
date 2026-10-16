import { describe, expect, it } from 'vitest'
import { createI18n } from '@/core/i18n/i18n'
import { languages, type Language } from '@/core/i18n/language'
import {
  dashboardActivityRowOf,
  danaJoinedRow,
  expenseEditedRow,
  flatGroupId,
  greeceGroupId,
  paymentRecordedRow,
  taxiAddedRow,
} from '@/test/dashboardTestData'
import { translated } from '@/test/translated'
import { formatMoney } from '@/shared/utils/formatMoney'
import { dashboardActivityLines } from './dashboardActivityLines'

async function contextOf(language: Language) {
  const i18n = await createI18n(language)
  return { t: i18n.t, language }
}

describe('dashboardActivityLines', () => {
  it.each(languages)('writes the sentence of an added expense with the name, title and amount in %s', async (language) => {
    const lines = dashboardActivityLines([taxiAddedRow], await contextOf(language))

    expect(lines).toHaveLength(1)
    expect(lines[0].sentence).toBe(
      translated(language, 'activity.expenseAdded', {
        name: 'Ana',
        title: 'Taxi',
        amount: formatMoney(90000, 'MKD', language),
      }),
    )
  })

  it.each(languages)('writes the sentence of a recorded payment in %s', async (language) => {
    const lines = dashboardActivityLines([paymentRecordedRow], await contextOf(language))

    expect(lines[0].sentence).toBe(
      translated(language, 'activity.settlementRecorded', {
        name: 'Marko',
        from: 'Marko',
        to: 'Filip',
        amount: formatMoney(60000, 'MKD', language),
      }),
    )
  })

  it.each(languages)('writes the sentence of a member who joined in %s', async (language) => {
    const lines = dashboardActivityLines([danaJoinedRow], await contextOf(language))

    expect(lines[0].sentence).toBe(translated(language, 'activity.memberJoined', { member: 'Dana' }))
  })

  it.each(languages)('writes an edit as one sentence without old and new values in %s', async (language) => {
    const lines = dashboardActivityLines([expenseEditedRow], await contextOf(language))

    expect(lines).toHaveLength(1)
    expect(lines[0].sentence).toBe(translated(language, 'home.activityExpenseEdited', { name: 'Ana' }))
    expect(lines[0].sentence).not.toContain('900')
    expect(lines[0].sentence).not.toContain('1,200')
  })

  it('writes one sentence for an edit that changed several things', async () => {
    const severalChanges = dashboardActivityRowOf(
      {
        id: '0f000000-0000-4000-8000-000000000090',
        type: 'ExpenseEdited',
        actorName: 'Ana',
        expenseId: 'e0000000-0000-4000-8000-000000000002',
        changes: [
          { field: 'amount', old: '90000', new: '120000' },
          { field: 'title', old: 'Taxi', new: 'Taxi to the airport' },
          { field: 'category', old: 'food', new: 'transport' },
        ],
        data: { currency: 'MKD' },
      },
      greeceGroupId,
      'Greece trip',
    )

    const lines = dashboardActivityLines([severalChanges], await contextOf('en'))

    expect(lines).toHaveLength(1)
  })

  it('writes the edit sentence for an old edit that has no data and no changes, because no expense list is needed', async () => {
    const bareEdit = dashboardActivityRowOf(
      {
        id: '0f000000-0000-4000-8000-000000000091',
        type: 'ExpenseEdited',
        actorName: 'Ana',
        expenseId: 'e0000000-0000-4000-8000-000000000003',
        changes: null,
        data: null,
      },
      greeceGroupId,
      'Greece trip',
    )

    const lines = dashboardActivityLines([bareEdit], await contextOf('en'))

    expect(lines[0].sentence).toBe(translated('en', 'home.activityExpenseEdited', { name: 'Ana' }))
  })

  it('keeps the group id, the group name, the time and the order of the rows', async () => {
    const lines = dashboardActivityLines([taxiAddedRow, danaJoinedRow, expenseEditedRow], await contextOf('en'))

    expect(lines.map((line) => line.groupId)).toEqual([greeceGroupId, flatGroupId, expenseEditedRow.groupId])
    expect(lines.map((line) => line.groupName)).toEqual(['Greece trip', 'Flat', expenseEditedRow.groupName])
    expect(lines.map((line) => line.createdAt)).toEqual([
      taxiAddedRow.createdAt,
      danaJoinedRow.createdAt,
      expenseEditedRow.createdAt,
    ])
  })

  it('gives every line its own key', async () => {
    const lines = dashboardActivityLines(
      [taxiAddedRow, paymentRecordedRow, danaJoinedRow, expenseEditedRow],
      await contextOf('en'),
    )

    expect(new Set(lines.map((line) => line.key)).size).toBe(4)
  })

  it('returns no lines for no rows', async () => {
    expect(dashboardActivityLines([], await contextOf('en'))).toEqual([])
  })

  it('throws an error naming the event type when Kvit has no sentence for it', async () => {
    const unknown = dashboardActivityRowOf(
      { id: '0f000000-0000-4000-8000-000000000092', type: 'PigsFlew' },
      greeceGroupId,
      'Greece trip',
    )
    const context = await contextOf('en')

    expect(() => dashboardActivityLines([unknown], context)).toThrow('PigsFlew')
  })

  it('throws an error naming the event when an added expense has no data', async () => {
    const broken = dashboardActivityRowOf(
      { id: '0f000000-0000-4000-8000-000000000093', type: 'ExpenseAdded', data: null },
      greeceGroupId,
      'Greece trip',
    )
    const context = await contextOf('en')

    expect(() => dashboardActivityLines([broken], context)).toThrow('0f000000-0000-4000-8000-000000000093')
  })
})
