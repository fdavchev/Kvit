import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { languages } from '@/core/i18n/language'
import { stubGoogleSignIn } from '@/test/googleTestHelpers'
import {
  adminPerson,
  anaPerson,
  deletedPerson,
  guestPerson,
  suspendedPerson,
} from '@/test/ownerToolsTestData'
import {
  answerWith,
  findPersonCard,
  findToolsTitle,
  renderOwnerTools,
  resultsList,
  searchPeopleFor,
  toolsUrls,
} from '@/test/ownerToolsTestHelpers'
import { neverAnswer, networkFailureAnswer, problemAnswer } from '@/test/requestTestHelpers'
import { stubDeviceColorScheme } from '@/test/themeTestHelpers'
import { translated } from '@/test/translated'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const peopleGet = 'GET /api/admin/tools/people'

function queryOfRequest(url: string): string | null {
  return new URL(url, 'http://kvit.test').searchParams.get('query')
}

async function renderPeople(people: unknown = [anaPerson], language: 'en' | 'mk' = 'en') {
  const rendered = await renderOwnerTools({ language, answers: { [peopleGet]: answerWith(people) } })
  await findToolsTitle(language)
  return rendered
}

describe('searching for people in the owner tools', () => {
  beforeEach(() => {
    window.localStorage.clear()
    vi.clearAllMocks()
    stubDeviceColorScheme('light')
    stubGoogleSignIn()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  describe('what is sent', () => {
    it('sends one GET to the people address with the typed text as the query when Search is pressed', async () => {
      const { fetchMock } = await renderPeople()

      searchPeopleFor('ana@example.com')

      await waitFor(() => {
        expect(toolsUrls(fetchMock, 'GET')).toHaveLength(1)
      })
      expect(queryOfRequest(toolsUrls(fetchMock, 'GET')[0])).toBe('ana@example.com')
      expect(toolsUrls(fetchMock, 'GET')[0].startsWith('/api/admin/tools/people?')).toBe(true)
    })

    it('does not ask anything while the text is only typed and Search is not pressed', async () => {
      const { fetchMock } = await renderPeople()

      fireEvent.change(screen.getByLabelText(translated('en', 'ownerTools.people.searchLabel')), {
        target: { value: 'ana' },
      })

      expect(toolsUrls(fetchMock, 'GET')).toEqual([])
    })

    it('sends the text without the spaces around it', async () => {
      const { fetchMock } = await renderPeople()

      searchPeopleFor('  ana  ')

      await waitFor(() => {
        expect(toolsUrls(fetchMock, 'GET')).toHaveLength(1)
      })
      expect(queryOfRequest(toolsUrls(fetchMock, 'GET')[0])).toBe('ana')
    })

    it('accepts exactly two characters', async () => {
      const { fetchMock } = await renderPeople()

      searchPeopleFor('an')

      await waitFor(() => {
        expect(toolsUrls(fetchMock, 'GET')).toHaveLength(1)
      })
      expect(queryOfRequest(toolsUrls(fetchMock, 'GET')[0])).toBe('an')
    })

    it.each(['', 'a', ' a ', '   '])('does not call the server for the text %j, because it has fewer than 2 characters', async (text) => {
      const { fetchMock } = await renderPeople()

      searchPeopleFor(text)
      await new Promise((resolve) => setTimeout(resolve, 30))

      expect(toolsUrls(fetchMock, 'GET')).toEqual([])
      expect(resultsList()).toBeNull()
    })

    it('searches again, with the new text, when the person changes the text and presses Search again', async () => {
      const { fetchMock } = await renderPeople()
      searchPeopleFor('ana')
      await findPersonCard(anaPerson.displayName)

      searchPeopleFor('petar')

      await waitFor(() => {
        expect(toolsUrls(fetchMock, 'GET')).toHaveLength(2)
      })
      expect(toolsUrls(fetchMock, 'GET').map(queryOfRequest)).toEqual(['ana', 'petar'])
    })

  })

  describe('what is shown', () => {
    it('shows one card for each person of the answer, in the order of the answer', async () => {
      await renderPeople([anaPerson, suspendedPerson, guestPerson, adminPerson, deletedPerson])
      searchPeopleFor('an')

      await findPersonCard(anaPerson.displayName)

      const names = within(resultsList() as HTMLElement)
        .getAllByRole('listitem')
        .map((card) => card.textContent ?? '')
      expect(names).toHaveLength(5)
      expect(names[0]).toContain(anaPerson.displayName)
      expect(names[1]).toContain(suspendedPerson.displayName)
      expect(names[2]).toContain(guestPerson.displayName)
      expect(names[3]).toContain(adminPerson.displayName)
      expect(names[4]).toContain(deletedPerson.displayName)
    })

    it('shows Nobody found. when the answer is an empty list', async () => {
      await renderPeople([])

      searchPeopleFor('zz')

      expect(await screen.findByText(translated('en', 'ownerTools.people.empty'))).toBeTruthy()
    })

    it('does not show Nobody found. before a search, or when people were found', async () => {
      await renderPeople([anaPerson])
      expect(screen.queryByText(translated('en', 'ownerTools.people.empty'))).toBeNull()

      searchPeopleFor('ana')
      await findPersonCard(anaPerson.displayName)

      expect(screen.queryByText(translated('en', 'ownerTools.people.empty'))).toBeNull()
    })

    it('replaces the old cards with the cards of the new search', async () => {
      const { fetchMock } = await renderOwnerTools({
        answers: {
          [peopleGet]: (request) => Response.json(request.url.endsWith('=ana') ? [anaPerson] : [suspendedPerson]),
        },
      })
      await findToolsTitle()
      searchPeopleFor('ana')
      await findPersonCard(anaPerson.displayName)

      searchPeopleFor('petar')

      expect(await screen.findByText(suspendedPerson.displayName)).toBeTruthy()
      await waitFor(() => {
        expect(screen.queryByText(anaPerson.displayName)).toBeNull()
      })
      expect(toolsUrls(fetchMock, 'GET')).toHaveLength(2)
    })

    it('names the field and the button in the language of the person', async () => {
      await renderPeople([], 'mk')

      expect(screen.getByLabelText(translated('mk', 'ownerTools.people.searchLabel'))).toBeTruthy()
      expect(screen.getByRole('button', { name: translated('mk', 'ownerTools.people.search') })).toBeTruthy()
    })

    it.each(languages)('writes Nobody found. in the language of the person (%s)', async (language) => {
      await renderPeople([], language)

      searchPeopleFor('zz', language)

      expect(await screen.findByText(translated(language, 'ownerTools.people.empty'))).toBeTruthy()
    })
  })

  describe('when the search fails', () => {
    it.each([
      ['a lost connection', networkFailureAnswer(), 'errors.network'],
      ['a refusal with a code', problemAnswer(429, 'RATE_LIMITED'), 'errors.RATE_LIMITED'],
    ])('shows the translated message in an alert after %s, and no card', async (_name, answer, textKey) => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      await renderOwnerTools({ answers: { [peopleGet]: answer } })
      await findToolsTitle()

      searchPeopleFor('ana')

      const alert = await screen.findByRole('alert')
      expect(within(alert).getByText(translated('en', textKey))).toBeTruthy()
      expect(resultsList()).toBeNull()
    })

    it('asks again when Search is pressed again with the same text after a failure', async () => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      let calls = 0
      await renderOwnerTools({
        answers: {
          [peopleGet]: () => {
            calls += 1
            return calls === 1 ? new TypeError('Failed to fetch') : Response.json([anaPerson])
          },
        },
      })
      await findToolsTitle()
      searchPeopleFor('ana')
      await screen.findByRole('alert')

      searchPeopleFor('ana')

      expect(await findPersonCard(anaPerson.displayName)).toBeTruthy()
      expect(screen.queryByRole('alert')).toBeNull()
    })

    it('shows the cards of the answer with no spinner left behind once the answer came', async () => {
      await renderPeople([anaPerson])

      searchPeopleFor('ana')
      await findPersonCard(anaPerson.displayName)

      expect(screen.queryByRole('status')).toBeNull()
    })
  })

  describe('while the answer is on its way', () => {
    it('shows a loading status and no card', async () => {
      await renderOwnerTools({ answers: { [peopleGet]: neverAnswer() } })
      await findToolsTitle()

      searchPeopleFor('ana')

      expect(await screen.findByRole('status')).toBeTruthy()
      expect(resultsList()).toBeNull()
    })
  })
})
