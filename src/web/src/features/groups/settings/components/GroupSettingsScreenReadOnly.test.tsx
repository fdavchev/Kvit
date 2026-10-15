import { fireEvent, screen, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { languages, type Language } from '@/core/i18n/language'
import { routes } from '@/core/router/routes'
import type { Group } from '@/core/services/groups/groupsService'
import { closingGroup, closingTexts, finishedGroup, unlockPath } from '@/test/closingTestData'
import { liveGroupAnswer, type LiveGroupState } from '@/test/closingTestHelpers'
import { findErrorText } from '@/test/expenseTestHelpers'
import { expectDisabledWhilePending } from '@/test/formTestHelpers'
import { groupOf, testGroup } from '@/test/groupTestData'
import { renderRoutesWithProviders } from '@/test/renderWithProviders'
import {
  noContentAnswer,
  problemAnswer,
  requestCount,
  requestsOf,
  stubFetchByRequest,
  type AnswerFactory,
} from '@/test/requestTestHelpers'
import { neverAnsweringFetch } from '@/test/settlementActionsTestHelpers'
import { seedMe, testMe } from '@/test/testMe'
import { shownToastTexts } from '@/test/toastTestHelpers'
import { translated } from '@/test/translated'
import { GroupSettingsScreen } from './GroupSettingsScreen'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const groupPath = `/api/groups/${testGroup.id}`
const leavePath = `${groupPath}/leave`

const openOwnerGroup = groupOf({ memberCount: 5 })
const memberFinishedGroup: Group = { ...finishedGroup, isOwner: false }
const memberClosingGroup: Group = { ...closingGroup, isOwner: false }

interface RenderOptions {
  language?: Language
  answers?: Record<string, AnswerFactory>
}

async function renderSettings(live: LiveGroupState, options: RenderOptions = {}) {
  const language = options.language ?? 'en'
  const fetchMock = stubFetchByRequest({
    [`GET ${groupPath}`]: liveGroupAnswer(live),
    ...options.answers,
  })
  const rendered = await renderRoutesWithProviders(
    [
      { path: '/groups/:groupId/settings', element: <GroupSettingsScreen /> },
      { path: '/groups/:groupId', element: <p>group page</p> },
      { path: routes.groups, element: <p>groups page</p> },
    ],
    routes.groupSettings(live.group.id),
    { language, seedCache: seedMe({ ...testMe, language }) },
  )
  await screen.findByRole('heading', { level: 1, name: translated(language, 'groupSettings.title') })
  return { fetchMock, ...rendered }
}

function saveButton(language: Language = 'en'): HTMLElement | null {
  return screen.queryByRole('button', { name: translated(language, 'groupSettings.save') })
}

function deleteButton(language: Language = 'en'): HTMLElement | null {
  return screen.queryByRole('button', { name: translated(language, 'groupSettings.delete') })
}

function unlockButton(language: Language = 'en'): HTMLElement | null {
  return screen.queryByRole('button', { name: closingTexts.unlock[language] })
}

function leaveButton(language: Language = 'en'): HTMLButtonElement | null {
  return screen.queryByRole<HTMLButtonElement>('button', { name: translated(language, 'groupSettings.leave') })
}

describe('the group settings when the group is read-only', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  describe('for the owner', () => {
    it('has Save and Delete group and no Unlock for changes while the group is open', async () => {
      await renderSettings({ group: openOwnerGroup })

      expect(saveButton()).not.toBeNull()
      expect(deleteButton()).not.toBeNull()
      expect(unlockButton()).toBeNull()
    })

    it('has Save and Delete group and no Unlock for changes while the group is being closed, because renaming does not cancel the closing', async () => {
      await renderSettings({ group: closingGroup })

      expect(saveButton()).not.toBeNull()
      expect(deleteButton()).not.toBeNull()
      expect(unlockButton()).toBeNull()
    })

    it('has no Save, but Delete group and Unlock for changes when the group is finished', async () => {
      await renderSettings({ group: finishedGroup })

      expect(saveButton()).toBeNull()
      expect(deleteButton()).not.toBeNull()
      expect(unlockButton()).not.toBeNull()
    })

    it.each(languages)('writes the button to unlock in the language of the person (%s)', async (language) => {
      await renderSettings({ group: finishedGroup }, { language })

      expect(unlockButton(language)).not.toBeNull()
      expect(saveButton(language)).toBeNull()
    })

    it('deletes a finished group with the Delete group button', async () => {
      const { fetchMock, router } = await renderSettings(
        { group: finishedGroup },
        { answers: { [`DELETE ${groupPath}`]: noContentAnswer() } },
      )

      fireEvent.click(deleteButton() as HTMLElement)

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.groups)
      })
      expect(requestCount(fetchMock, 'DELETE', groupPath)).toBe(1)
    })
  })

  describe('Unlock for changes', () => {
    it('sends one POST to the unlock address with no body', async () => {
      const live: LiveGroupState = { group: finishedGroup }
      const { fetchMock } = await renderSettings(live, { answers: { [`POST ${unlockPath}`]: noContentAnswer() } })

      fireEvent.click(unlockButton() as HTMLElement)

      await waitFor(() => {
        expect(requestCount(fetchMock, 'POST', unlockPath)).toBe(1)
      })
      expect(requestsOf(fetchMock, 'POST', unlockPath)[0].body).toBeUndefined()
    })

    it('brings Save back and takes the button away after the server answered and the group was asked again', async () => {
      const live: LiveGroupState = { group: finishedGroup }
      await renderSettings(live, {
        answers: {
          [`POST ${unlockPath}`]: () => {
            live.group = openOwnerGroup
            return new Response(null, { status: 204 })
          },
        },
      })

      fireEvent.click(unlockButton() as HTMLElement)

      expect(await screen.findByRole('button', { name: translated('en', 'groupSettings.save') })).toBeTruthy()
      expect(unlockButton()).toBeNull()
      expect(shownToastTexts()).toContain(closingTexts.unlocked.en)
    })

    it('disables the button while the server has not answered and sends one request when it is pressed again', async () => {
      await renderSettings({ group: finishedGroup })
      const neverAnswering = neverAnsweringFetch()
      const button = unlockButton() as HTMLElement

      fireEvent.click(button)
      await expectDisabledWhilePending(button)
      fireEvent.click(button)

      expect(neverAnswering).toHaveBeenCalledTimes(1)
    })

    it.each([
      [400, 'GROUP_NOT_FINISHED'],
      [403, 'GROUP_NOT_OWNER'],
    ])('shows the translated text of %i %s', async (status, code) => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      await renderSettings({ group: finishedGroup }, { answers: { [`POST ${unlockPath}`]: problemAnswer(status, code) } })

      fireEvent.click(unlockButton() as HTMLElement)

      await findErrorText(translated('en', `errors.${code}`))
      expect(unlockButton()).not.toBeNull()
    })
  })

  describe('for a member who is not the owner', () => {
    it('has no Leave group while the group is being closed, because the server refuses it', async () => {
      await renderSettings({ group: memberClosingGroup })

      expect(leaveButton()).toBeNull()
      expect(saveButton()).toBeNull()
      expect(unlockButton()).toBeNull()
    })

    it('has neither Save nor Unlock for changes, and keeps Leave group enabled, when the group is finished', async () => {
      await renderSettings({ group: memberFinishedGroup })

      expect(saveButton()).toBeNull()
      expect(unlockButton()).toBeNull()
      expect(deleteButton()).toBeNull()
      expect(leaveButton()).not.toBeNull()
      expect(leaveButton()?.disabled).toBe(false)
    })

    it('leaves a finished group with the Leave group button', async () => {
      const { fetchMock, router } = await renderSettings(
        { group: memberFinishedGroup },
        { answers: { [`POST ${leavePath}`]: noContentAnswer() } },
      )

      fireEvent.click(leaveButton() as HTMLElement)

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.groups)
      })
      expect(requestCount(fetchMock, 'POST', leavePath)).toBe(1)
    })
  })
})
