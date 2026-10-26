import { act, fireEvent, screen, waitFor } from '@testing-library/react'
import type { QueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { languages, type Language } from '@/core/i18n/language'
import { routes } from '@/core/router/routes'
import type { Group } from '@/core/services/groups/groupsService'
import type { Me } from '@/core/services/me/meService'
import { isChecked } from '@/test/checkedStateTestHelpers'
import { asMemberOf, closingGroup, finishedGroup } from '@/test/closingTestData'
import { fieldLabelled, typeInto } from '@/test/formTestHelpers'
import { groupOf, testGroup } from '@/test/groupTestData'
import { renderRoutesWithProviders } from '@/test/renderWithProviders'
import {
  jsonAnswer,
  neverAnswer,
  networkFailureAnswer,
  noContentAnswer,
  problemAnswer,
  requestCount,
  requestsOf,
  stubFetchByRequest,
  type AnswerFactory,
} from '@/test/requestTestHelpers'
import { seedMe, testGuestMe, testMe } from '@/test/testMe'
import { translated } from '@/test/translated'
import { GroupSettingsScreen } from './GroupSettingsScreen'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const groupPath = `/api/groups/${testGroup.id}`
const switchPath = `${groupPath}/members/me/counts-in-budget`
const switchPut = `PUT ${switchPath}`

const openOwnerGroup = groupOf({ memberCount: 3 })
const openMemberGroup = groupOf({ isOwner: false, memberCount: 3 })

interface RenderOptions {
  me?: Me
  language?: Language
  answers?: Record<string, AnswerFactory>
}

async function renderSettings(group: Group, options: RenderOptions = {}) {
  const language = options.language ?? 'en'
  const fetchMock = stubFetchByRequest({
    [`GET ${groupPath}`]: jsonAnswer(group),
    ...options.answers,
  })
  const rendered = await renderRoutesWithProviders(
    [
      { path: '/groups/:groupId/settings', element: <GroupSettingsScreen /> },
      { path: '/groups/:groupId', element: <p>group page</p> },
    ],
    routes.groupSettings(group.id),
    { language, seedCache: seedMe({ ...(options.me ?? testMe), language }) },
  )
  await screen.findByRole('heading', { level: 1, name: translated(language, 'groupSettings.title') })
  return { fetchMock, ...rendered }
}

function countSwitch(language: Language = 'en'): HTMLElement {
  return screen.getByRole('switch', { name: translated(language, 'groupSettings.countInBudget') })
}

function findCountSwitch(language: Language = 'en'): HTMLElement | null {
  return screen.queryByRole('switch', { name: translated(language, 'groupSettings.countInBudget') })
}

async function letRequestsSettle(): Promise<void> {
  await act(async () => {
    await new Promise((resolve) => setTimeout(resolve, 0))
  })
}

function cachedCountsInBudget(queryClient: QueryClient): unknown {
  return queryClient.getQueryData<Group>(['groups', testGroup.id])?.countsInBudget
}

describe('GroupSettingsScreen switch "Count my shares in my budget"', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  describe('what is shown', () => {
    it.each(languages)('shows a switch with the label and the helper line to the owner of an open group (%s)', async (language) => {
      await renderSettings(openOwnerGroup, { language })

      expect(countSwitch(language)).toBeTruthy()
      expect(screen.getByText(translated(language, 'groupSettings.countInBudgetHint'))).toBeTruthy()
    })

    it.each(languages)('shows the switch to a member who is not the owner of an open group (%s)', async (language) => {
      await renderSettings(openMemberGroup, { language })

      expect(countSwitch(language)).toBeTruthy()
      expect(screen.getByText(translated(language, 'groupSettings.countInBudgetHint'))).toBeTruthy()
    })

    it.each([true, false])('shows the switch as checked %j when the group says countsInBudget is %j', async (countsInBudget) => {
      await renderSettings(groupOf({ memberCount: 3, countsInBudget }))

      expect(isChecked(countSwitch())).toBe(countsInBudget)
    })

    it.each([true, false])('shows the switch as checked %j to a member who is not the owner', async (countsInBudget) => {
      await renderSettings(groupOf({ isOwner: false, memberCount: 3, countsInBudget }))

      expect(isChecked(countSwitch())).toBe(countsInBudget)
    })

    it('shows the switch to the owner of a Closing group', async () => {
      await renderSettings(closingGroup)

      expect(countSwitch()).toBeTruthy()
    })

    it('keeps the form with the name, Save and Delete group next to the switch for the owner', async () => {
      await renderSettings(openOwnerGroup)

      expect(fieldLabelled(translated('en', 'groupFields.name')).value).toBe(openOwnerGroup.name)
      expect(screen.getByRole('button', { name: translated('en', 'groupSettings.save') })).toBeTruthy()
      expect(screen.getByRole('button', { name: translated('en', 'groupSettings.delete') })).toBeTruthy()
    })
  })

  describe('what is hidden', () => {
    it.each([
      ['the owner of a Finished group', finishedGroup],
      ['a member of a Finished group', asMemberOf(finishedGroup)],
      ['a member of a Closing group', asMemberOf(closingGroup)],
    ])('shows no switch and no helper line to %s', async (_who, group) => {
      await renderSettings(group)

      expect(findCountSwitch()).toBeNull()
      expect(screen.queryByText(translated('en', 'groupSettings.countInBudgetHint'))).toBeNull()
    })

    it.each(languages)('shows no switch and no helper line to a guest in an open group (%s)', async (language) => {
      await renderSettings(openMemberGroup, { me: testGuestMe, language })

      expect(findCountSwitch(language)).toBeNull()
      expect(screen.queryByRole('switch')).toBeNull()
      expect(screen.queryByText(translated(language, 'groupSettings.countInBudgetHint'))).toBeNull()
    })
  })

  describe('turning it off and on', () => {
    it('sends PUT counts-in-budget with false after one tap on a switch that is on, and nothing else', async () => {
      const { fetchMock } = await renderSettings(openOwnerGroup, { answers: { [switchPut]: noContentAnswer() } })

      fireEvent.click(countSwitch())

      await waitFor(() => {
        expect(requestCount(fetchMock, 'PUT', switchPath)).toBe(1)
      })
      expect(requestsOf(fetchMock, 'PUT', switchPath)[0].body).toEqual({ countsInBudget: false })
      expect(requestCount(fetchMock, 'PUT', groupPath)).toBe(0)
    })

    it('sends PUT counts-in-budget with true after one tap on a switch that is off', async () => {
      const { fetchMock } = await renderSettings(groupOf({ memberCount: 3, countsInBudget: false }), {
        answers: { [switchPut]: noContentAnswer() },
      })

      fireEvent.click(countSwitch())

      await waitFor(() => {
        expect(requestCount(fetchMock, 'PUT', switchPath)).toBe(1)
      })
      expect(requestsOf(fetchMock, 'PUT', switchPath)[0].body).toEqual({ countsInBudget: true })
    })

    it('works the same for a member who is not the owner', async () => {
      const { fetchMock } = await renderSettings(openMemberGroup, { answers: { [switchPut]: noContentAnswer() } })

      fireEvent.click(countSwitch())

      await waitFor(() => {
        expect(requestCount(fetchMock, 'PUT', switchPath)).toBe(1)
      })
      expect(requestsOf(fetchMock, 'PUT', switchPath)[0].body).toEqual({ countsInBudget: false })
    })

    it('works for the owner of a Closing group', async () => {
      const { fetchMock } = await renderSettings(closingGroup, { answers: { [switchPut]: noContentAnswer() } })

      fireEvent.click(countSwitch())

      await waitFor(() => {
        expect(requestCount(fetchMock, 'PUT', switchPath)).toBe(1)
      })
    })

    it.each([true, false])('shows the switch as checked %j after the server accepted the change, and puts it in the cached group', async (startsOn) => {
      const { queryClient } = await renderSettings(groupOf({ memberCount: 3, countsInBudget: startsOn }), {
        answers: { [switchPut]: noContentAnswer() },
      })

      fireEvent.click(countSwitch())

      await waitFor(() => {
        expect(isChecked(countSwitch())).toBe(!startsOn)
      })
      expect(cachedCountsInBudget(queryClient)).toBe(!startsOn)
    })

    it('shows no success toast and no error toast, because the switch itself is the feedback', async () => {
      await renderSettings(openOwnerGroup, { answers: { [switchPut]: noContentAnswer() } })

      fireEvent.click(countSwitch())
      await waitFor(() => {
        expect(isChecked(countSwitch())).toBe(false)
      })

      expect(toast.success).not.toHaveBeenCalled()
      expect(toast.error).not.toHaveBeenCalled()
    })

    it('leaves what was typed in the name field alone and sends no group update', async () => {
      const { fetchMock } = await renderSettings(openOwnerGroup, { answers: { [switchPut]: noContentAnswer() } })
      typeInto(translated('en', 'groupFields.name'), 'Flat 4B')

      fireEvent.click(countSwitch())
      await waitFor(() => {
        expect(isChecked(countSwitch())).toBe(false)
      })

      expect(fieldLabelled(translated('en', 'groupFields.name')).value).toBe('Flat 4B')
      expect(requestCount(fetchMock, 'PUT', groupPath)).toBe(0)
    })

    it('is not turned on or off by pressing Save', async () => {
      const { fetchMock } = await renderSettings(openOwnerGroup, {
        answers: { [`PUT ${groupPath}`]: noContentAnswer() },
      })

      fireEvent.click(screen.getByRole('button', { name: translated('en', 'groupSettings.save') }))
      await waitFor(() => {
        expect(requestCount(fetchMock, 'PUT', groupPath)).toBe(1)
      })

      expect(requestCount(fetchMock, 'PUT', switchPath)).toBe(0)
    })
  })

  describe('when saving fails', () => {
    it.each(languages)('shows the translated GROUP_FINISHED message in an error toast and keeps the switch as it was (%s)', async (language) => {
      const { queryClient } = await renderSettings(openOwnerGroup, {
        language,
        answers: { [switchPut]: problemAnswer(400, 'GROUP_FINISHED') },
      })

      fireEvent.click(countSwitch(language))

      await waitFor(() => {
        expect(toast.error).toHaveBeenCalledWith(translated(language, 'errors.GROUP_FINISHED'))
      })
      expect(isChecked(countSwitch(language))).toBe(true)
      expect(cachedCountsInBudget(queryClient)).toBe(true)
    })

    it.each([
      ['GROUP_CLOSING', 400],
      ['GROUP_NOT_FOUND', 404],
      ['GUEST_NOT_ALLOWED', 403],
    ])('shows the translated %s message in an error toast', async (errorCode, status) => {
      await renderSettings(openMemberGroup, { answers: { [switchPut]: problemAnswer(status, errorCode) } })

      fireEvent.click(countSwitch())

      await waitFor(() => {
        expect(toast.error).toHaveBeenCalledWith(translated('en', `errors.${errorCode}`))
      })
    })

    it('shows the generic message after a server error', async () => {
      await renderSettings(openOwnerGroup, { answers: { [switchPut]: () => new Response(null, { status: 500 }) } })

      fireEvent.click(countSwitch())

      await waitFor(() => {
        expect(toast.error).toHaveBeenCalledWith(translated('en', 'errors.generic'))
      })
    })

    it('shows the network message when the request never got an answer', async () => {
      await renderSettings(openOwnerGroup, { answers: { [switchPut]: networkFailureAnswer() } })

      fireEvent.click(countSwitch())

      await waitFor(() => {
        expect(toast.error).toHaveBeenCalledWith(translated('en', 'errors.network'))
      })
    })

    it('keeps a switch that was off as off after a failure', async () => {
      await renderSettings(groupOf({ memberCount: 3, countsInBudget: false }), {
        answers: { [switchPut]: problemAnswer(400, 'GROUP_FINISHED') },
      })

      fireEvent.click(countSwitch())
      await waitFor(() => {
        expect(toast.error).toHaveBeenCalled()
      })

      expect(isChecked(countSwitch())).toBe(false)
    })

    it('lets the person try again after a failure', async () => {
      let tries = 0
      const { fetchMock } = await renderSettings(openOwnerGroup, {
        answers: {
          [switchPut]: () => {
            tries += 1
            return tries === 1 ? new Response(null, { status: 500 }) : new Response(null, { status: 204 })
          },
        },
      })
      fireEvent.click(countSwitch())
      await waitFor(() => {
        expect(toast.error).toHaveBeenCalledOnce()
      })

      fireEvent.click(countSwitch())

      await waitFor(() => {
        expect(isChecked(countSwitch())).toBe(false)
      })
      expect(requestCount(fetchMock, 'PUT', switchPath)).toBe(2)
    })
  })

  describe('while a save is running', () => {
    it('sends only one request when the switch is tapped twice before the server answered', async () => {
      const { fetchMock } = await renderSettings(openOwnerGroup, { answers: { [switchPut]: neverAnswer() } })

      fireEvent.click(countSwitch())
      fireEvent.click(countSwitch())
      await letRequestsSettle()

      expect(requestCount(fetchMock, 'PUT', switchPath)).toBe(1)
    })
  })
})
