import { screen, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { languages, type Language } from '@/core/i18n/language'
import { stubGoogleSignIn } from '@/test/googleTestHelpers'
import {
  adminPerson,
  anaPerson,
  deletedPerson,
  deletingPerson,
  guestPerson,
  personWithMyId,
  suspendedPerson,
} from '@/test/ownerToolsTestData'
import {
  answerWith,
  buttonNamesIn,
  findPersonCard,
  findToolsTitle,
  renderOwnerTools,
  searchPeopleFor,
} from '@/test/ownerToolsTestHelpers'
import type { OwnerToolsPerson } from '@/core/services/ownerTools/ownerToolsService'
import { stubDeviceColorScheme } from '@/test/themeTestHelpers'
import { translated } from '@/test/translated'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const peopleGet = 'GET /api/admin/tools/people'
const badgeKeys = ['suspended', 'guest', 'admin', 'deletionPending', 'deleted'] as const

async function showCard(person: OwnerToolsPerson, language: Language = 'en'): Promise<HTMLElement> {
  await renderOwnerTools({ language, answers: { [peopleGet]: answerWith([person]) } })
  await findToolsTitle(language)
  searchPeopleFor('xx', language)
  return findPersonCard(person.displayName)
}

function badgeKeysShownIn(card: HTMLElement, language: Language = 'en'): string[] {
  return badgeKeys.filter((key) => within(card).queryByText(translated(language, `ownerTools.badge.${key}`)) !== null)
}

function actionNames(language: Language = 'en'): Record<'suspend' | 'unsuspend' | 'eraseName', string> {
  return {
    suspend: translated(language, 'ownerTools.suspend'),
    unsuspend: translated(language, 'ownerTools.unsuspend'),
    eraseName: translated(language, 'ownerTools.eraseName'),
  }
}

describe('a person card in the owner tools', () => {
  beforeEach(() => {
    window.localStorage.clear()
    vi.clearAllMocks()
    stubDeviceColorScheme('light')
    stubGoogleSignIn()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  describe('what it says', () => {
    it('shows the name and the email', async () => {
      const card = await showCard(anaPerson)

      expect(within(card).getByText(anaPerson.displayName)).toBeTruthy()
      expect(within(card).getByText(anaPerson.email as string)).toBeTruthy()
    })

    it('shows no email text for a guest, who has none', async () => {
      const card = await showCard(guestPerson)

      expect(within(card).getByText(guestPerson.displayName)).toBeTruthy()
      expect(card.textContent).not.toContain('@')
      expect(card.textContent).not.toContain('null')
    })

    it.each([
      [0, 'ownerTools.people.groupCount_other'],
      [1, 'ownerTools.people.groupCount_one'],
      [2, 'ownerTools.people.groupCount_other'],
      [3, 'ownerTools.people.groupCount_other'],
      [25, 'ownerTools.people.groupCount_other'],
    ])('writes the group count %i with the form %s', async (groupCount, key) => {
      const card = await showCard({ ...anaPerson, groupCount })

      expect(within(card).getByText(translated('en', key, { count: String(groupCount) }))).toBeTruthy()
    })

    it.each([
      [1, 'ownerTools.people.groupCount_one'],
      [2, 'ownerTools.people.groupCount_other'],
      [11, 'ownerTools.people.groupCount_other'],
      [21, 'ownerTools.people.groupCount_one'],
    ])('writes the group count %i with the form %s in Macedonian', async (groupCount, key) => {
      const card = await showCard({ ...anaPerson, groupCount }, 'mk')

      expect(within(card).getByText(translated('mk', key, { count: String(groupCount) }))).toBeTruthy()
    })
  })

  describe('the badges', () => {
    it('shows no badge for an ordinary person', async () => {
      const card = await showCard(anaPerson)

      expect(badgeKeysShownIn(card)).toEqual([])
    })

    it.each([
      ['suspended', suspendedPerson],
      ['guest', guestPerson],
      ['admin', adminPerson],
      ['deletionPending', deletingPerson],
      ['deleted', deletedPerson],
    ] as const)('shows only the %s badge for a person who is just that', async (key, person) => {
      const card = await showCard(person)

      expect(badgeKeysShownIn(card)).toEqual([key])
    })

    it('shows every badge that is true, side by side', async () => {
      const card = await showCard({
        ...anaPerson,
        isSuspended: true,
        isGuest: true,
        deletionPending: true,
      })

      expect(badgeKeysShownIn(card)).toEqual(['suspended', 'guest', 'deletionPending'])
    })

    it.each(languages)('writes the badges in the language of the person (%s)', async (language) => {
      const card = await showCard({ ...anaPerson, isSuspended: true, isAdmin: true }, language)

      expect(badgeKeysShownIn(card, language)).toEqual(['suspended', 'admin'])
    })
  })

  describe('the action buttons', () => {
    it('has Suspend and Erase name for an ordinary person, and no Unsuspend', async () => {
      const card = await showCard(anaPerson)

      expect(buttonNamesIn(card)).toEqual([actionNames().suspend, actionNames().eraseName])
    })

    it('has Unsuspend and Erase name, and no Suspend, for a suspended person', async () => {
      const card = await showCard(suspendedPerson)

      expect(buttonNamesIn(card)).toEqual([actionNames().unsuspend, actionNames().eraseName])
    })

    it('has Suspend but no Erase name for a person whose account is already deleted', async () => {
      const card = await showCard(deletedPerson)

      expect(buttonNamesIn(card)).toEqual([actionNames().suspend])
    })

    it('has Suspend and Erase name for a guest', async () => {
      const card = await showCard(guestPerson)

      expect(buttonNamesIn(card)).toEqual([actionNames().suspend, actionNames().eraseName])
    })

    it('has Suspend and Erase name for a person whose deletion is pending', async () => {
      const card = await showCard(deletingPerson)

      expect(buttonNamesIn(card)).toEqual([actionNames().suspend, actionNames().eraseName])
    })

    it('has no button at all for an owner of Kvit', async () => {
      const card = await showCard(adminPerson)

      expect(buttonNamesIn(card)).toEqual([])
    })

    it('has no button at all for an owner of Kvit who is also suspended', async () => {
      const card = await showCard({ ...adminPerson, isSuspended: true })

      expect(buttonNamesIn(card)).toEqual([])
    })

    it('has no button at all on the card of the person who is signed in', async () => {
      const card = await showCard(personWithMyId)

      expect(buttonNamesIn(card)).toEqual([])
    })

    it.each(languages)('writes the buttons in the language of the person (%s)', async (language) => {
      const card = await showCard(anaPerson, language)

      expect(buttonNamesIn(card)).toEqual([actionNames(language).suspend, actionNames(language).eraseName])
    })

    it('shows the buttons of each card on its own card when several people are found', async () => {
      await renderOwnerTools({
        answers: { [peopleGet]: answerWith([anaPerson, suspendedPerson, adminPerson, personWithMyId]) },
      })
      await findToolsTitle()
      searchPeopleFor('xx')
      await screen.findByText(anaPerson.displayName)

      const cards = [anaPerson, suspendedPerson, adminPerson, personWithMyId].map((person) =>
        buttonNamesIn(screen.getByText(person.displayName).closest('li') as HTMLElement),
      )

      expect(cards).toEqual([
        [actionNames().suspend, actionNames().eraseName],
        [actionNames().unsuspend, actionNames().eraseName],
        [],
        [],
      ])
    })
  })
})
