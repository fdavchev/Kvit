import { describe, expect, it } from 'vitest'
import { createI18n } from '@/core/i18n/i18n'
import { languages, type Language } from '@/core/i18n/language'
import type { ActivityEvent } from '@/core/services/activity/activityService'
import { activityEventOf } from '@/test/activityTestData'
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
  it.each(languages)('says the person joined when the name they took is their own name, as for a guest who tapped a free name (%s)', async (language) => {
    const sentences = await sentencesOf(claimedEvent('Cveta', 'Cveta'), language)

    expect(sentences).toEqual([translated(language, 'activity.memberJoined', { member: 'Cveta' })])
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
