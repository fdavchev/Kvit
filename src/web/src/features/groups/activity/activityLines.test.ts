import { describe, expect, it } from 'vitest'
import { createI18n } from '@/core/i18n/i18n'
import { languages, type Language } from '@/core/i18n/language'
import type { ActivityEvent } from '@/core/services/activity/activityService'
import type { ExpenseListRow } from '@/core/services/expenses/expensesService'
import { activityEventOf } from '@/test/activityTestData'
import { taxiRow, untitledRow } from '@/test/expenseTestData'
import { cvetaGuestMember, petarMember } from '@/test/memberTestData'
import { translated } from '@/test/translated'
import { activityLines } from './activityLines'

async function sentencesOf(event: ActivityEvent, language: Language): Promise<string[]> {
  const i18n = await createI18n(language)
  return activityLines([event], { t: i18n.t, language, today: '2026-10-16', categories: [], expenses: [] }).map(
    (line) => line.sentence,
  )
}

function claimedEvent(name: string, claimedName: string): ActivityEvent {
  return activityEventOf({
    type: 'MemberClaimed',
    actorName: name,
    memberId: cvetaGuestMember.id,
    data: { name, claimedName },
  })
}

describe('activityLines for a person who took a name', () => {
  it.each(languages)('says the person joined when the name they took is their own name, as stored for a guest who tapped a free name before the server stopped writing this event for guests (%s)', async (language) => {
    const sentences = await sentencesOf(claimedEvent('Cveta', 'Cveta'), language)

    expect(sentences).toEqual([translated(language, 'activity.memberJoined', { member: 'Cveta' })])
  })

  it('still shows both stored lines of an older guest tap, the joined event and the same-name claim, instead of hiding one', async () => {
    const i18n = await createI18n('en')
    const joined = activityEventOf({
      id: 'joined-event',
      type: 'MemberJoined',
      actorName: 'Cveta',
      memberId: cvetaGuestMember.id,
      data: { name: 'Cveta' },
    })
    const claimed = { ...claimedEvent('Cveta', 'Cveta'), id: 'claimed-event' }

    const lines = activityLines([claimed, joined], { t: i18n.t, language: 'en', today: '2026-10-16', categories: [], expenses: [] })

    expect(lines.map((line) => line.sentence)).toEqual([
      translated('en', 'activity.memberJoined', { member: 'Cveta' }),
      translated('en', 'activity.memberJoined', { member: 'Cveta' }),
    ])
  })

  it.each(languages)('keeps "took the name" when the account name differs from the name that was taken (%s)', async (language) => {
    const sentences = await sentencesOf(claimedEvent(petarMember.displayName, 'Darko'), language)

    expect(sentences).toEqual([
      translated(language, 'activity.memberClaimed', { member: 'Petar', claimedName: 'Darko' }),
    ])
  })

  it('keeps "took the name" when the two names differ only in letter case, because they are written differently', async () => {
    const sentences = await sentencesOf(claimedEvent('marko', 'Marko'), 'en')

    expect(sentences).toEqual([translated('en', 'activity.memberClaimed', { member: 'marko', claimedName: 'Marko' })])
  })
})

describe('activityLines for a change of currency', () => {
  function groupCurrencyEvent(): ActivityEvent {
    return activityEventOf({
      type: 'GroupSettingsChanged',
      changes: [{ field: 'defaultCurrency', old: 'MKD', new: 'EUR' }],
    })
  }

  function expenseCurrencyEvent(): ActivityEvent {
    return activityEventOf({
      type: 'ExpenseEdited',
      expenseId: taxiRow.id,
      changes: [{ field: 'currency', old: 'EUR', new: 'MKD' }],
      data: { currency: 'MKD' },
    })
  }

  it('writes the denar of the group currency as «ден.» in Macedonian, the way amounts write it', async () => {
    expect(await sentencesOf(groupCurrencyEvent(), 'mk')).toEqual(['Ana ја измени валутата на групата: ден. → EUR'])
  })

  it('keeps the code MKD for the group currency in English', async () => {
    expect(await sentencesOf(groupCurrencyEvent(), 'en')).toEqual(["Ana changed the group's currency: MKD → EUR"])
  })

  it('writes the denar of an expense currency change as «ден.» in Macedonian', async () => {
    expect(await sentencesOf(expenseCurrencyEvent(), 'mk')).toEqual(['Ana ја измени валутата: EUR → ден.'])
  })

  it('keeps the code MKD for an expense currency change in English', async () => {
    expect(await sentencesOf(expenseCurrencyEvent(), 'en')).toEqual(['Ana changed the currency: EUR → MKD'])
  })

  it('stops with a clear error when the group currency in the event is not one Kvit knows', async () => {
    const event = activityEventOf({
      type: 'GroupSettingsChanged',
      changes: [{ field: 'defaultCurrency', old: 'MKD', new: 'USD' }],
    })

    await expect(sentencesOf(event, 'en')).rejects.toThrow('The history has a currency Kvit does not know: "USD"')
  })
})

describe('activityLines for a restored expense', () => {
  async function restoredSentences(
    expenses: readonly ExpenseListRow[],
    language: Language,
    expenseId: string = taxiRow.id,
  ): Promise<string[]> {
    const i18n = await createI18n(language)
    const event = activityEventOf({ type: 'ExpenseRestored', expenseId })
    return activityLines([event], { t: i18n.t, language, today: '2026-10-16', categories: [], expenses }).map(
      (line) => line.sentence,
    )
  }

  it.each([
    ['en', 'Ana restored “Taxi”'],
    ['mk', 'Ana го врати „Taxi“'],
  ] as const)('names the expense by its title when the expense is in the list (%s)', async (language, sentence) => {
    expect(await restoredSentences([taxiRow], language)).toEqual([sentence])
  })

  it.each(languages)('keeps "restored an expense" when the expense in the list has no title (%s)', async (language) => {
    expect(await restoredSentences([untitledRow], language, untitledRow.id)).toEqual([
      translated(language, 'activity.expenseRestored', { name: 'Ana' }),
    ])
  })

  it.each(languages)('keeps "restored an expense" when the expense is not in the list (%s)', async (language) => {
    expect(await restoredSentences([], language)).toEqual([
      translated(language, 'activity.expenseRestored', { name: 'Ana' }),
    ])
  })
})
