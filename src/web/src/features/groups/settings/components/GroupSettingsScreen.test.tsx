import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { toast } from 'sonner'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { Group } from '@/core/services/groups/groupsService'
import { languages, type Language } from '@/core/i18n/language'
import { routes } from '@/core/router/routes'
import { stubFetchThatNeverAnswers } from '@/test/apiTestHelpers'
import { expectDisabledWhilePending, fieldLabelled, typeInto } from '@/test/formTestHelpers'
import { groupEmojis, groupOf, testGroup } from '@/test/groupTestData'
import { renderRoutesWithProviders } from '@/test/renderWithProviders'
import {
  jsonAnswer,
  networkFailureAnswer,
  noContentAnswer,
  problemAnswer,
  requestCount,
  requestsOf,
  stubFetchByRequest,
  type AnswerFactory,
} from '@/test/requestTestHelpers'
import { seedMe, testMe } from '@/test/testMe'
import {
  actionOf,
  pressToastAction,
  referenceDangerToastStyling,
  referenceToastStyling,
  shownToastTexts,
  stylingOf,
  toastShownWithText,
  toastsShownWithOptions,
} from '@/test/toastTestHelpers'
import { translated } from '@/test/translated'
import { GroupSettingsScreen } from './GroupSettingsScreen'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const groupPath = `/api/groups/${testGroup.id}`
const restorePath = `${groupPath}/restore`
const leavePath = `${groupPath}/leave`

const ownerGroup = groupOf({ emoji: groupEmojis[2], defaultCurrency: 'EUR', memberCount: 3 })
const memberGroup = groupOf({ isOwner: false, memberCount: 3 })

interface RenderOptions {
  language?: Language
  group?: Group
  answers?: Record<string, AnswerFactory>
}

async function renderSettings(options: RenderOptions = {}) {
  const group = options.group ?? ownerGroup
  const language = options.language ?? 'en'
  const fetchMock = stubFetchByRequest({
    [`GET ${groupPath}`]: jsonAnswer(group),
    ...options.answers,
  })
  const rendered = await renderRoutesWithProviders(
    [
      { path: '/groups/:groupId/settings', element: <GroupSettingsScreen /> },
      { path: '/groups/:groupId', element: <p>group page</p> },
      { path: routes.groups, element: <p>groups page</p> },
    ],
    routes.groupSettings(group.id),
    { language, seedCache: seedMe({ ...testMe, language }) },
  )
  return { fetchMock, ...rendered }
}

async function showsSettings(language: Language = 'en'): Promise<void> {
  await screen.findByRole('heading', {
    level: 1,
    name: translated(language, 'groupSettings.title'),
  })
}

function saveButton(language: Language = 'en'): HTMLElement {
  return screen.getByRole('button', { name: translated(language, 'groupSettings.save') })
}

function deleteButton(language: Language = 'en'): HTMLElement {
  return screen.getByRole('button', { name: translated(language, 'groupSettings.delete') })
}

function leaveButton(language: Language = 'en'): HTMLButtonElement {
  return screen.getByRole<HTMLButtonElement>('button', {
    name: translated(language, 'groupSettings.leave'),
  })
}

function nameField(language: Language = 'en'): HTMLInputElement {
  return fieldLabelled(translated(language, 'groupFields.name'))
}

function chipIn(groupKey: string, name: string): HTMLElement {
  return within(
    screen.getByRole('group', { name: translated('en', groupKey) }),
  ).getByRole('button', { name })
}

function emojiChip(emoji: string): HTMLElement {
  return chipIn('groupFields.emoji', emoji)
}

function currencyChip(currency: string): HTMLElement {
  return chipIn('groupFields.currency', currency)
}

describe('GroupSettingsScreen', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  describe('loading and failing', () => {
    it('shows the loading spinner while the group is being asked', async () => {
      stubFetchThatNeverAnswers()

      await renderRoutesWithProviders(
        [{ path: '/groups/:groupId/settings', element: <GroupSettingsScreen /> }],
        routes.groupSettings(testGroup.id),
        { seedCache: seedMe(testMe) },
      )

      expect(screen.getByRole('status')).toBeTruthy()
    })

    it.each(languages)('shows the GROUP_NOT_FOUND message when the group does not exist or the person is not in it (%s)', async (language) => {
      await renderSettings({
        language,
        answers: { [`GET ${groupPath}`]: problemAnswer(404, 'GROUP_NOT_FOUND') },
      })

      expect((await screen.findByRole('alert')).textContent).toContain(
        translated(language, 'errors.GROUP_NOT_FOUND'),
      )
    })

    it('shows the generic message with a retry button after a server error', async () => {
      await renderSettings({
        answers: { [`GET ${groupPath}`]: () => new Response(null, { status: 500 }) },
      })

      expect((await screen.findByRole('alert')).textContent).toContain(
        translated('en', 'errors.generic'),
      )
      expect(screen.getByRole('button', { name: translated('en', 'common.retry') })).toBeTruthy()
    })

    it('asks for the group again and shows the settings when the retry button is pressed', async () => {
      let tries = 0
      await renderSettings({
        answers: {
          [`GET ${groupPath}`]: () => {
            tries += 1
            return tries === 1 ? new Response(null, { status: 500 }) : Response.json(ownerGroup)
          },
        },
      })

      fireEvent.click(await screen.findByRole('button', { name: translated('en', 'common.retry') }))

      await showsSettings()
      expect(screen.queryByRole('alert')).toBeNull()
    })
  })

  describe('as the owner', () => {
    it.each(languages)('shows the Group settings heading (%s)', async (language) => {
      await renderSettings({ language })

      await showsSettings(language)
    })

    it('has a back button to the group', async () => {
      await renderSettings()
      await showsSettings()

      const back = screen.getByRole('link', { name: translated('en', 'common.back') })

      expect(back.getAttribute('href')).toBe(routes.group(testGroup.id))
    })

    it('fills the name field with the name of the group', async () => {
      await renderSettings()
      await showsSettings()

      expect(nameField().value).toBe(ownerGroup.name)
    })

    it('shows the emoji of the group as the pressed one', async () => {
      await renderSettings()
      await showsSettings()

      for (const emoji of groupEmojis) {
        expect(emojiChip(emoji).getAttribute('aria-pressed')).toBe(String(emoji === groupEmojis[2]))
      }
    })

    it('shows the currency of the group as the pressed one', async () => {
      await renderSettings()
      await showsSettings()

      expect(currencyChip('EUR').getAttribute('aria-pressed')).toBe('true')
      expect(currencyChip('MKD').getAttribute('aria-pressed')).toBe('false')
    })

    it.each(languages)('has the Save, Delete group and Leave group buttons (%s)', async (language) => {
      await renderSettings({ language })
      await showsSettings(language)

      expect(saveButton(language)).toBeTruthy()
      expect(deleteButton(language)).toBeTruthy()
      expect(leaveButton(language)).toBeTruthy()
    })

    it.each(languages)('disables Leave group and says the owner must hand over first (%s)', async (language) => {
      await renderSettings({ language })
      await showsSettings(language)

      expect(leaveButton(language).disabled).toBe(true)
      expect(screen.getByText(translated(language, 'groupSettings.ownerCannotLeave'))).toBeTruthy()
    })

    it('sends no request when the disabled Leave group button is pressed', async () => {
      const { fetchMock } = await renderSettings()
      await showsSettings()

      fireEvent.click(leaveButton())

      expect(requestCount(fetchMock, 'POST', leavePath)).toBe(0)
    })

    it('sends no request besides asking for the group when nothing is pressed', async () => {
      const { fetchMock } = await renderSettings()
      await showsSettings()

      expect(fetchMock.mock.calls.map(([url]) => String(url))).toEqual([groupPath])
    })

    describe('saving', () => {
      it('sends the changed name, emoji and currency to the group', async () => {
        const { fetchMock } = await renderSettings({
          answers: { [`PUT ${groupPath}`]: noContentAnswer() },
        })
        await showsSettings()
        typeInto(translated('en', 'groupFields.name'), 'Flat 4B')
        fireEvent.click(emojiChip(groupEmojis[4]))
        fireEvent.click(currencyChip('MKD'))

        fireEvent.click(saveButton())

        await waitFor(() => {
          expect(requestCount(fetchMock, 'PUT', groupPath)).toBe(1)
        })
        expect(requestsOf(fetchMock, 'PUT', groupPath)[0]).toMatchObject({
          contentType: 'application/json',
          body: { name: 'Flat 4B', emoji: groupEmojis[4], currency: 'MKD' },
        })
      })

      it('sends the values of the group when nothing was changed', async () => {
        const { fetchMock } = await renderSettings({
          answers: { [`PUT ${groupPath}`]: noContentAnswer() },
        })
        await showsSettings()

        fireEvent.click(saveButton())

        await waitFor(() => {
          expect(requestCount(fetchMock, 'PUT', groupPath)).toBe(1)
        })
        expect(requestsOf(fetchMock, 'PUT', groupPath)[0].body).toEqual({
          name: ownerGroup.name,
          emoji: groupEmojis[2],
          currency: 'EUR',
        })
      })

      it('saves when the form is submitted with the Enter key', async () => {
        const { fetchMock } = await renderSettings({
          answers: { [`PUT ${groupPath}`]: noContentAnswer() },
        })
        await showsSettings()
        const form = nameField().form
        if (form === null) {
          throw new Error('The name field is not inside a form')
        }

        fireEvent.submit(form)

        await waitFor(() => {
          expect(requestCount(fetchMock, 'PUT', groupPath)).toBe(1)
        })
      })

      it.each(languages)('shows the Saved toast after saving (%s)', async (language) => {
        await renderSettings({
          language,
          answers: { [`PUT ${groupPath}`]: noContentAnswer() },
        })
        await showsSettings(language)

        fireEvent.click(saveButton(language))

        await waitFor(() => {
          expect(shownToastTexts()).toContain(translated(language, 'groupSettings.saved'))
        })
      })

      it('stays on the settings screen and keeps the saved name in the field after saving', async () => {
        let isSaved = false
        const { router } = await renderSettings({
          answers: {
            [`GET ${groupPath}`]: () =>
              Response.json(isSaved ? groupOf({ ...ownerGroup, name: 'Flat 4B' }) : ownerGroup),
            [`PUT ${groupPath}`]: () => {
              isSaved = true
              return new Response(null, { status: 204 })
            },
          },
        })
        await showsSettings()
        typeInto(translated('en', 'groupFields.name'), 'Flat 4B')

        fireEvent.click(saveButton())
        await waitFor(() => {
          expect(shownToastTexts()).toContain(translated('en', 'groupSettings.saved'))
        })

        expect(router.state.location.pathname).toBe(routes.groupSettings(testGroup.id))
        expect(nameField().value).toBe('Flat 4B')
      })

      it('disables the Save button while the request waits for an answer', async () => {
        await renderSettings()
        await showsSettings()
        stubFetchThatNeverAnswers()

        fireEvent.click(saveButton())

        await expectDisabledWhilePending(saveButton())
      })

      it.each(
        languages.flatMap((language) =>
          (
            [
              ['GROUP_NAME_INVALID', 400],
              ['GROUP_EMOJI_INVALID', 400],
              ['GROUP_CURRENCY_INVALID', 400],
              ['GROUP_NOT_OWNER', 403],
            ] as const
          ).map(([code, status]) => [language, code, status] as const),
        ),
      )('shows the %s text for the answer %s inline', async (language, code, status) => {
        await renderSettings({
          language,
          answers: { [`PUT ${groupPath}`]: problemAnswer(status, code) },
        })
        await showsSettings(language)

        fireEvent.click(saveButton(language))

        expect((await screen.findByRole('alert')).textContent).toContain(
          translated(language, `errors.${code}`),
        )
      })

      it('shows the name message inline when the name is emptied and the server refuses it', async () => {
        await renderSettings({
          answers: { [`PUT ${groupPath}`]: problemAnswer(400, 'GROUP_NAME_INVALID') },
        })
        await showsSettings()
        typeInto(translated('en', 'groupFields.name'), '')

        fireEvent.click(saveButton())

        expect((await screen.findByRole('alert')).textContent).toContain(
          translated('en', 'errors.GROUP_NAME_INVALID'),
        )
      })

      it('keeps the typed values, shows no Saved toast and stays on the screen after the server refuses', async () => {
        const { router } = await renderSettings({
          answers: { [`PUT ${groupPath}`]: problemAnswer(400, 'GROUP_CURRENCY_INVALID') },
        })
        await showsSettings()
        typeInto(translated('en', 'groupFields.name'), 'Flat 4B')
        fireEvent.click(emojiChip(groupEmojis[5]))
        fireEvent.click(currencyChip('MKD'))

        fireEvent.click(saveButton())
        await screen.findByRole('alert')

        expect(nameField().value).toBe('Flat 4B')
        expect(emojiChip(groupEmojis[5]).getAttribute('aria-pressed')).toBe('true')
        expect(currencyChip('MKD').getAttribute('aria-pressed')).toBe('true')
        expect(shownToastTexts()).not.toContain(translated('en', 'groupSettings.saved'))
        expect(router.state.location.pathname).toBe(routes.groupSettings(testGroup.id))
      })

      it('shows the network message inline when the server cannot be reached', async () => {
        await renderSettings({ answers: { [`PUT ${groupPath}`]: networkFailureAnswer() } })
        await showsSettings()

        fireEvent.click(saveButton())

        expect((await screen.findByRole('alert')).textContent).toContain(
          translated('en', 'errors.network'),
        )
      })
    })

    describe('deleting', () => {
      it('sends the delete request for the group with no body', async () => {
        const { fetchMock } = await renderSettings({
          answers: { [`DELETE ${groupPath}`]: noContentAnswer() },
        })
        await showsSettings()

        fireEvent.click(deleteButton())

        await waitFor(() => {
          expect(requestCount(fetchMock, 'DELETE', groupPath)).toBe(1)
        })
        expect(requestsOf(fetchMock, 'DELETE', groupPath)[0].body).toBeUndefined()
      })

      it('asks no "are you sure" question before deleting', async () => {
        await renderSettings({ answers: { [`DELETE ${groupPath}`]: noContentAnswer() } })
        await showsSettings()

        fireEvent.click(deleteButton())

        expect(screen.queryByRole('dialog')).toBeNull()
        expect(screen.queryByRole('alertdialog')).toBeNull()
      })

      it('goes to the groups list after the group is deleted', async () => {
        const { router } = await renderSettings({
          answers: { [`DELETE ${groupPath}`]: noContentAnswer() },
        })
        await showsSettings()

        fireEvent.click(deleteButton())

        await waitFor(() => {
          expect(router.state.location.pathname).toBe(routes.groups)
        })
        expect(screen.getByText('groups page')).toBeTruthy()
      })

      it.each(languages)('shows the Group deleted toast with an Undo button for 4 seconds (%s)', async (language) => {
        await renderSettings({
          language,
          answers: { [`DELETE ${groupPath}`]: noContentAnswer() },
        })
        await showsSettings(language)

        fireEvent.click(deleteButton(language))

        const shown = await waitFor(() =>
          toastShownWithText(translated(language, 'groupSettings.deleted')),
        )
        expect(shown.options.duration).toBe(4000)
        expect(actionOf(shown).label).toBe(translated(language, 'common.undo'))
      })

      it('shows the Group deleted toast in the wine-red danger style', async () => {
        const dangerStyling = referenceToastStyling(true)
        await renderSettings({ answers: { [`DELETE ${groupPath}`]: noContentAnswer() } })
        await showsSettings()

        fireEvent.click(deleteButton())

        const shown = await waitFor(() =>
          toastShownWithText(translated('en', 'groupSettings.deleted')),
        )
        expect(stylingOf(shown)).toEqual(dangerStyling)
      })

      it('restores the group when Undo is pressed on the toast', async () => {
        const { fetchMock } = await renderSettings({
          answers: {
            [`DELETE ${groupPath}`]: noContentAnswer(),
            [`POST ${restorePath}`]: noContentAnswer(),
          },
        })
        await showsSettings()
        fireEvent.click(deleteButton())
        const shown = await waitFor(() =>
          toastShownWithText(translated('en', 'groupSettings.deleted')),
        )

        pressToastAction(shown)

        await waitFor(() => {
          expect(requestCount(fetchMock, 'POST', restorePath)).toBe(1)
        })
        expect(requestsOf(fetchMock, 'POST', restorePath)[0].body).toBeUndefined()
      })

      it('does not restore the group before Undo is pressed', async () => {
        const { fetchMock } = await renderSettings({
          answers: {
            [`DELETE ${groupPath}`]: noContentAnswer(),
            [`POST ${restorePath}`]: noContentAnswer(),
          },
        })
        await showsSettings()

        fireEvent.click(deleteButton())
        await waitFor(() => {
          expect(requestCount(fetchMock, 'DELETE', groupPath)).toBe(1)
        })

        expect(requestCount(fetchMock, 'POST', restorePath)).toBe(0)
      })

      it.each(languages)('shows the GROUP_NOT_OWNER error toast, stays on the screen and shows no Group deleted toast when the server refuses (%s)', async (language) => {
        vi.spyOn(console, 'error').mockImplementation(() => {})
        const { router } = await renderSettings({
          language,
          answers: { [`DELETE ${groupPath}`]: problemAnswer(403, 'GROUP_NOT_OWNER') },
        })
        await showsSettings(language)

        fireEvent.click(deleteButton(language))

        await waitFor(() => {
          expect(toast.error).toHaveBeenCalledWith(translated(language, 'errors.GROUP_NOT_OWNER'))
        })
        expect(router.state.location.pathname).toBe(routes.groupSettings(testGroup.id))
        expect(toastsShownWithOptions()).toEqual([])
      })

      it('shows the network error toast when the delete cannot reach the server', async () => {
        vi.spyOn(console, 'error').mockImplementation(() => {})
        await renderSettings({ answers: { [`DELETE ${groupPath}`]: networkFailureAnswer() } })
        await showsSettings()

        fireEvent.click(deleteButton())

        await waitFor(() => {
          expect(toast.error).toHaveBeenCalledWith(translated('en', 'errors.network'))
        })
      })
    })
  })

  describe('as a member who is not the owner', () => {
    it.each(languages)('shows the Group settings heading (%s)', async (language) => {
      await renderSettings({ language, group: memberGroup })

      await showsSettings(language)
    })

    it('has a back button to the group', async () => {
      await renderSettings({ group: memberGroup })
      await showsSettings()

      const back = screen.getByRole('link', { name: translated('en', 'common.back') })

      expect(back.getAttribute('href')).toBe(routes.group(testGroup.id))
    })

    it.each(languages)('has an enabled Leave group button and nothing else to press (%s)', async (language) => {
      await renderSettings({ language, group: memberGroup })
      await showsSettings(language)

      expect(leaveButton(language).disabled).toBe(false)
      expect(screen.getAllByRole('button').map((button) => button.textContent)).toEqual([
        translated(language, 'groupSettings.leave'),
      ])
    })

    it('shows no name field, no emoji or currency choice, no Save and no Delete group', async () => {
      await renderSettings({ group: memberGroup })
      await showsSettings()

      expect(screen.queryByRole('textbox')).toBeNull()
      expect(screen.queryByRole('group')).toBeNull()
      expect(screen.queryByRole('button', { name: translated('en', 'groupSettings.save') })).toBeNull()
      expect(screen.queryByRole('button', { name: translated('en', 'groupSettings.delete') })).toBeNull()
    })

    it('does not show the hand-over line that only the owner needs', async () => {
      await renderSettings({ group: memberGroup })
      await showsSettings()

      expect(screen.queryByText(translated('en', 'groupSettings.ownerCannotLeave'))).toBeNull()
    })

    it('sends the leave request for the group with no body', async () => {
      const { fetchMock } = await renderSettings({
        group: memberGroup,
        answers: { [`POST ${leavePath}`]: noContentAnswer() },
      })
      await showsSettings()

      fireEvent.click(leaveButton())

      await waitFor(() => {
        expect(requestCount(fetchMock, 'POST', leavePath)).toBe(1)
      })
      expect(requestsOf(fetchMock, 'POST', leavePath)[0].body).toBeUndefined()
    })

    it('goes to the groups list after leaving', async () => {
      const { router } = await renderSettings({
        group: memberGroup,
        answers: { [`POST ${leavePath}`]: noContentAnswer() },
      })
      await showsSettings()

      fireEvent.click(leaveButton())

      await waitFor(() => {
        expect(router.state.location.pathname).toBe(routes.groups)
      })
      expect(screen.getByText('groups page')).toBeTruthy()
    })

    it.each([
      ['en', 'You left Greece trip'],
      ['mk', 'Ја напушти групата „Greece trip“'],
    ] as const)('shows the left toast with the group name and no Undo button (%s)', async (language, text) => {
      await renderSettings({
        language,
        group: memberGroup,
        answers: { [`POST ${leavePath}`]: noContentAnswer() },
      })
      await showsSettings(language)

      fireEvent.click(leaveButton(language))

      const shown = await waitFor(() => toastShownWithText(text))
      expect(shown.options.action).toBeUndefined()
      expect(shown.options.duration).toBe(4000)
    })

    it('shows the left toast in the wine-red danger style', async () => {
      const dangerStyling = referenceDangerToastStyling()
      await renderSettings({
        group: memberGroup,
        answers: { [`POST ${leavePath}`]: noContentAnswer() },
      })
      await showsSettings()

      fireEvent.click(leaveButton())

      const shown = await waitFor(() =>
        toastShownWithText(translated('en', 'groupSettings.left', { name: memberGroup.name })),
      )
      expect(stylingOf(shown)).toEqual(dangerStyling)
    })

    it('shows the error toast, stays on the screen and shows no left toast when the server refuses', async () => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      const { router } = await renderSettings({
        group: memberGroup,
        answers: { [`POST ${leavePath}`]: problemAnswer(400, 'MEMBER_OWNER_CANNOT_LEAVE') },
      })
      await showsSettings()

      fireEvent.click(leaveButton())

      await waitFor(() => {
        expect(toast.error).toHaveBeenCalledWith(
          translated('en', 'errors.MEMBER_OWNER_CANNOT_LEAVE'),
        )
      })
      expect(router.state.location.pathname).toBe(routes.groupSettings(testGroup.id))
      expect(toastsShownWithOptions()).toEqual([])
    })

    it('shows the network error toast when leaving cannot reach the server', async () => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      await renderSettings({
        group: memberGroup,
        answers: { [`POST ${leavePath}`]: networkFailureAnswer() },
      })
      await showsSettings()

      fireEvent.click(leaveButton())

      await waitFor(() => {
        expect(toast.error).toHaveBeenCalledWith(translated('en', 'errors.network'))
      })
    })
  })
})
