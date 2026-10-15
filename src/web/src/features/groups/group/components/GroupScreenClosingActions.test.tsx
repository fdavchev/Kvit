import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { toast } from 'sonner'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { languages, type Language } from '@/core/i18n/language'
import { routes } from '@/core/router/routes'
import { renderGroupApp, type GroupAppOptions } from '@/test/activityTestHelpers'
import { settlementsPath } from '@/test/balancesTestData'
import { findBalanceCards, rowWithText } from '@/test/balancesTestHelpers'
import {
  asMemberOf,
  closingConfirmPath,
  closingGroup,
  closingGroupAnaConfirmed,
  closingObjectPath,
  closingPath,
  closingTexts,
  finishedBarText,
  finishedGroup,
  readyToCloseGroup,
  textOf,
  unlockPath,
} from '@/test/closingTestData'
import {
  anaView,
  buttonIn,
  findBarTitled,
  findClosingBar,
  liveGroupAnswer,
  ownerView,
  type LiveGroupState,
} from '@/test/closingTestHelpers'
import { macedonianOf } from '@/test/englishTextKeys'
import { groupPath, freezeTime, unfreezeTime } from '@/test/expenseTestHelpers'
import { saveButton, waitForSheetToClose } from '@/test/expenseFormTestHelpers'
import { expectDisabledWhilePending } from '@/test/formTestHelpers'
import { testGroup } from '@/test/groupTestData'
import { anaMember, filipMember } from '@/test/memberTestData'
import {
  networkFailureAnswer,
  noContentAnswer,
  problemAnswer,
  requestCount,
  requestsOf,
  type AnswerFactory,
} from '@/test/requestTestHelpers'
import { onePaymentBalances } from '@/test/settlementActionsTestData'
import { neverAnsweringFetch, openMarkPaid, sentBodies } from '@/test/settlementActionsTestHelpers'
import { stubDeviceColorScheme } from '@/test/themeTestHelpers'
import { shownToastTexts } from '@/test/toastTestHelpers'
import { translated } from '@/test/translated'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const groupId = testGroup.id
const openGroup = testGroup
const promptParams = { name: 'Filip', group: testGroup.name }
const memberClosingGroup = asMemberOf(closingGroup)
const memberConfirmedGroup = asMemberOf(closingGroupAnaConfirmed)
const memberFinishedGroup = asMemberOf(finishedGroup)
const memberOpenGroup = asMemberOf(openGroup)

function changesGroupTo(live: LiveGroupState, next: typeof openGroup): AnswerFactory {
  return () => {
    live.group = next
    return new Response(null, { status: 204 })
  }
}

function liveOptions(base: GroupAppOptions, live: LiveGroupState, answers: Record<string, AnswerFactory>): GroupAppOptions {
  return {
    ...base,
    answers: {
      ...base.answers,
      [`GET ${groupPath}`]: liveGroupAnswer(live),
      ...answers,
    },
  }
}

async function renderLive(
  base: GroupAppOptions,
  answers: Record<string, AnswerFactory>,
  path: string = routes.group(groupId),
) {
  const live: LiveGroupState = { group: base.group ?? testGroup }
  const rendered = await renderGroupApp(path, liveOptions(base, live, answers))
  return { live, ...rendered }
}

function objectDialogName(language: Language = 'en'): string {
  return closingTexts.objectTitle[language]
}

async function openObjectSheet(language: Language = 'en'): Promise<HTMLElement> {
  const bar = await findBarTitled(closingTexts.prompt, language, promptParams)
  fireEvent.click(buttonIn(bar, closingTexts.object[language]))
  return screen.findByRole('dialog', { name: objectDialogName(language) })
}

function reasonFieldIn(dialog: HTMLElement): HTMLTextAreaElement | HTMLInputElement {
  return within(dialog).getByRole<HTMLTextAreaElement | HTMLInputElement>('textbox')
}

function dialogButton(dialog: HTMLElement, name: string): HTMLElement {
  return within(dialog).getByRole('button', { name })
}

describe('the actions of the closing bar', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    freezeTime()
    stubDeviceColorScheme('light')
  })

  afterEach(() => {
    unfreezeTime()
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  describe('Close the group', () => {
    it('sends one POST to the closing address with no body when the owner taps the button', async () => {
      const { fetchMock } = await renderLive(ownerView(readyToCloseGroup), {
        [`POST ${closingPath}`]: noContentAnswer(),
      })
      const bar = await findBarTitled(closingTexts.readyTitle)

      fireEvent.click(buttonIn(bar, closingTexts.start.en))

      await waitFor(() => {
        expect(requestCount(fetchMock, 'POST', closingPath)).toBe(1)
      })
      const request = requestsOf(fetchMock, 'POST', closingPath)[0]
      expect(request.body).toBeUndefined()
    })

    it('turns the bar into the waiting bar after the server answered, with the question and the button gone', async () => {
      const live: LiveGroupState = { group: readyToCloseGroup }
      await renderGroupApp(
        routes.group(groupId),
        liveOptions(ownerView(readyToCloseGroup), live, {
          [`POST ${closingPath}`]: changesGroupTo(live, closingGroup),
        }),
      )
      const bar = await findBarTitled(closingTexts.readyTitle)

      fireEvent.click(buttonIn(bar, closingTexts.start.en))

      expect(await findBarTitled(closingTexts.waitingTitle)).toBeTruthy()
      expect(screen.queryByText(closingTexts.readyTitle.en)).toBeNull()
      expect(screen.queryByRole('button', { name: closingTexts.start.en })).toBeNull()
    })

    it('turns the bar into the finished bar when the group finishes at once, because nobody else has to confirm', async () => {
      const live: LiveGroupState = { group: readyToCloseGroup }
      await renderGroupApp(
        routes.group(groupId),
        liveOptions(ownerView(readyToCloseGroup), live, {
          [`POST ${closingPath}`]: changesGroupTo(live, finishedGroup),
        }),
      )
      const bar = await findBarTitled(closingTexts.readyTitle)

      fireEvent.click(buttonIn(bar, closingTexts.start.en))

      expect(await findClosingBar(finishedBarText.en)).toBeTruthy()
      expect(screen.queryByRole('link', { name: 'Add expense' })).toBeNull()
    })

    it('asks for the group once more after the answer', async () => {
      const live: LiveGroupState = { group: readyToCloseGroup }
      const { fetchMock } = await renderGroupApp(
        routes.group(groupId),
        liveOptions(ownerView(readyToCloseGroup), live, {
          [`POST ${closingPath}`]: changesGroupTo(live, closingGroup),
        }),
      )
      const bar = await findBarTitled(closingTexts.readyTitle)

      fireEvent.click(buttonIn(bar, closingTexts.start.en))

      await findBarTitled(closingTexts.waitingTitle)
      expect(requestCount(fetchMock, 'GET', groupPath)).toBe(2)
    })

    it('disables the button while the server has not answered and sends one request when it is pressed again', async () => {
      await renderLive(ownerView(readyToCloseGroup), {})
      const bar = await findBarTitled(closingTexts.readyTitle)
      const neverAnswering = neverAnsweringFetch()
      const button = buttonIn(bar, closingTexts.start.en)

      fireEvent.click(button)
      await expectDisabledWhilePending(button)
      fireEvent.click(button)

      expect(neverAnswering).toHaveBeenCalledTimes(1)
    })

    it.each([
      [400, 'GROUP_NOT_KVIT'],
      [403, 'GROUP_NOT_OWNER'],
      [400, 'GROUP_IS_ONE_BILL'],
      [400, 'GROUP_FINISHED'],
    ])('shows the translated text of %i %s inside the bar, leaves the button usable and shows no toast', async (status, code) => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      await renderLive(ownerView(readyToCloseGroup), { [`POST ${closingPath}`]: problemAnswer(status, code) })
      const bar = await findBarTitled(closingTexts.readyTitle)

      fireEvent.click(buttonIn(bar, closingTexts.start.en))

      expect((await within(bar).findByRole('alert')).textContent).toContain(translated('en', `errors.${code}`))
      expect(buttonIn(bar, closingTexts.start.en).matches(':disabled')).toBe(false)
      expect(vi.mocked(toast.error)).not.toHaveBeenCalled()
    })

    it('shows the network text inside the bar when the server cannot be reached', async () => {
      await renderLive(ownerView(readyToCloseGroup), { [`POST ${closingPath}`]: networkFailureAnswer() })
      const bar = await findBarTitled(closingTexts.readyTitle)

      fireEvent.click(buttonIn(bar, closingTexts.start.en))

      expect((await within(bar).findByRole('alert')).textContent).toContain(translated('en', 'errors.network'))
    })

    it('shows the error in Macedonian for a person who uses Macedonian', async () => {
      await renderLive(ownerView(readyToCloseGroup, 'mk'), {
        [`POST ${closingPath}`]: problemAnswer(400, 'GROUP_NOT_KVIT'),
      })
      const bar = await findBarTitled(closingTexts.readyTitle, 'mk')

      fireEvent.click(buttonIn(bar, closingTexts.start.mk))

      expect((await within(bar).findByRole('alert')).textContent).toContain(translated('mk', 'errors.GROUP_NOT_KVIT'))
    })

    it('lets the owner try again after a refusal, and the second try works', async () => {
      const live: LiveGroupState = { group: readyToCloseGroup }
      let tries = 0
      const { fetchMock } = await renderGroupApp(
        routes.group(groupId),
        liveOptions(ownerView(readyToCloseGroup), live, {
          [`POST ${closingPath}`]: () => {
            tries += 1
            if (tries === 1) {
              return problemAnswer(400, 'GROUP_NOT_KVIT')()
            }
            live.group = closingGroup
            return new Response(null, { status: 204 })
          },
        }),
      )
      const bar = await findBarTitled(closingTexts.readyTitle)
      fireEvent.click(buttonIn(bar, closingTexts.start.en))
      await within(bar).findByRole('alert')

      fireEvent.click(buttonIn(bar, closingTexts.start.en))

      expect(await findBarTitled(closingTexts.waitingTitle)).toBeTruthy()
      expect(requestCount(fetchMock, 'POST', closingPath)).toBe(2)
    })
  })

  describe('Confirm', () => {
    function confirmButton(bar: HTMLElement, language: Language = 'en'): HTMLElement {
      return buttonIn(bar, language === 'en' ? 'Confirm' : macedonianOf('Confirm'))
    }

    it('sends one POST to the confirm address with no body when the member taps Confirm', async () => {
      const { fetchMock } = await renderLive(anaView(closingGroup), {
        [`POST ${closingConfirmPath}`]: noContentAnswer(),
      })
      const bar = await findBarTitled(closingTexts.prompt, 'en', promptParams)

      fireEvent.click(confirmButton(bar))

      await waitFor(() => {
        expect(requestCount(fetchMock, 'POST', closingConfirmPath)).toBe(1)
      })
      expect(requestsOf(fetchMock, 'POST', closingConfirmPath)[0].body).toBeUndefined()
    })

    it('turns the prompt into You confirmed after the server answered', async () => {
      const live: LiveGroupState = { group: memberClosingGroup }
      await renderGroupApp(
        routes.group(groupId),
        liveOptions(anaView(closingGroup), live, {
          [`POST ${closingConfirmPath}`]: changesGroupTo(live, memberConfirmedGroup),
        }),
      )
      const bar = await findBarTitled(closingTexts.prompt, 'en', promptParams)

      fireEvent.click(confirmButton(bar))

      expect(await findBarTitled(closingTexts.youConfirmed)).toBeTruthy()
      expect(screen.queryByRole('button', { name: 'Confirm' })).toBeNull()
    })

    it('turns the prompt into the finished bar when this was the last confirmation', async () => {
      const live: LiveGroupState = { group: memberClosingGroup }
      await renderGroupApp(
        routes.group(groupId),
        liveOptions(anaView(closingGroup), live, {
          [`POST ${closingConfirmPath}`]: changesGroupTo(live, memberFinishedGroup),
        }),
      )
      const bar = await findBarTitled(closingTexts.prompt, 'en', promptParams)

      fireEvent.click(confirmButton(bar))

      expect(await findClosingBar(finishedBarText.en)).toBeTruthy()
    })

    it('disables Confirm while the server has not answered and sends one request when it is pressed again', async () => {
      await renderLive(anaView(closingGroup), {})
      const bar = await findBarTitled(closingTexts.prompt, 'en', promptParams)
      const neverAnswering = neverAnsweringFetch()
      const button = confirmButton(bar)

      fireEvent.click(button)
      await expectDisabledWhilePending(button)
      fireEvent.click(button)

      expect(neverAnswering).toHaveBeenCalledTimes(1)
    })

    it.each([
      [400, 'CLOSING_ALREADY_ANSWERED'],
      [400, 'GROUP_NOT_CLOSING'],
      [403, 'CLOSING_OWNER_CANNOT_ANSWER'],
      [400, 'GROUP_FINISHED'],
    ])('shows the translated text of %i %s inside the bar and shows no toast', async (status, code) => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      await renderLive(anaView(closingGroup), { [`POST ${closingConfirmPath}`]: problemAnswer(status, code) })
      const bar = await findBarTitled(closingTexts.prompt, 'en', promptParams)

      fireEvent.click(confirmButton(bar))

      expect((await within(bar).findByRole('alert')).textContent).toContain(translated('en', `errors.${code}`))
      expect(vi.mocked(toast.error)).not.toHaveBeenCalled()
    })

    it('shows the error in Macedonian for a person who uses Macedonian', async () => {
      await renderLive(anaView(closingGroup, 'mk'), {
        [`POST ${closingConfirmPath}`]: problemAnswer(400, 'CLOSING_ALREADY_ANSWERED'),
      })
      const bar = await findBarTitled(closingTexts.prompt, 'mk', promptParams)

      fireEvent.click(confirmButton(bar, 'mk'))

      expect((await within(bar).findByRole('alert')).textContent).toContain(
        translated('mk', 'errors.CLOSING_ALREADY_ANSWERED'),
      )
    })
  })

  describe('the Object sheet', () => {
    it('opens from the prompt with a title, an empty reason field, Cancel and Send, and sends nothing yet', async () => {
      const { fetchMock } = await renderLive(anaView(closingGroup), {})

      const dialog = await openObjectSheet()

      expect(reasonFieldIn(dialog).value).toBe('')
      expect(dialogButton(dialog, 'Cancel')).toBeTruthy()
      expect(dialogButton(dialog, closingTexts.send.en)).toBeTruthy()
      expect(requestCount(fetchMock, 'POST', closingObjectPath)).toBe(0)
    })

    it('opens from the bar of a member who already confirmed', async () => {
      await renderLive(anaView(closingGroupAnaConfirmed), {})
      const bar = await findBarTitled(closingTexts.youConfirmed)

      fireEvent.click(buttonIn(bar, closingTexts.object.en))

      expect(await screen.findByRole('dialog', { name: objectDialogName() })).toBeTruthy()
    })

    it('limits the reason to 200 characters, by the field or by a counter', async () => {
      await renderLive(anaView(closingGroup), {})

      const dialog = await openObjectSheet()

      const hasLimitOnField = reasonFieldIn(dialog).maxLength === 200
      const hasCounter = (dialog.textContent ?? '').includes('200')
      expect(hasLimitOnField || hasCounter).toBe(true)
    })

    it('names the field Reason (optional)', async () => {
      await renderLive(anaView(closingGroup), {})

      const dialog = await openObjectSheet()

      expect(within(dialog).getByLabelText(closingTexts.reasonLabel.en)).toBe(reasonFieldIn(dialog))
    })

    it('sends the reason as typed, to the object address, as JSON', async () => {
      const { fetchMock } = await renderLive(anaView(closingGroup), {
        [`POST ${closingObjectPath}`]: noContentAnswer(),
      })
      const dialog = await openObjectSheet()
      fireEvent.change(reasonFieldIn(dialog), { target: { value: 'Marko still owes me for the taxi' } })

      fireEvent.click(dialogButton(dialog, closingTexts.send.en))

      await waitFor(() => {
        expect(requestCount(fetchMock, 'POST', closingObjectPath)).toBe(1)
      })
      const request = requestsOf(fetchMock, 'POST', closingObjectPath)[0]
      expect(request.contentType).toBe('application/json')
      expect(request.body).toEqual({ reason: 'Marko still owes me for the taxi' })
    })

    it('trims the spaces around the reason before sending it', async () => {
      const { fetchMock } = await renderLive(anaView(closingGroup), {
        [`POST ${closingObjectPath}`]: noContentAnswer(),
      })
      const dialog = await openObjectSheet()
      fireEvent.change(reasonFieldIn(dialog), { target: { value: '   Not yet  ' } })

      fireEvent.click(dialogButton(dialog, closingTexts.send.en))

      await waitFor(() => {
        expect(requestCount(fetchMock, 'POST', closingObjectPath)).toBe(1)
      })
      expect(requestsOf(fetchMock, 'POST', closingObjectPath)[0].body).toEqual({ reason: 'Not yet' })
    })

    it.each([
      ['nothing', ''],
      ['only spaces', '    '],
    ])('sends no reason when the field holds %s, because the reason is optional', async (_name, typed) => {
      const { fetchMock } = await renderLive(anaView(closingGroup), {
        [`POST ${closingObjectPath}`]: noContentAnswer(),
      })
      const dialog = await openObjectSheet()
      fireEvent.change(reasonFieldIn(dialog), { target: { value: typed } })

      fireEvent.click(dialogButton(dialog, closingTexts.send.en))

      await waitFor(() => {
        expect(requestCount(fetchMock, 'POST', closingObjectPath)).toBe(1)
      })
      const request = requestsOf(fetchMock, 'POST', closingObjectPath)[0]
      expect(request.contentType).toBe('application/json')
      expect([undefined, null, '']).toContain((request.body as { reason?: unknown }).reason)
    })

    it('sends a reason of exactly 200 characters unchanged', async () => {
      const reason = 'a'.repeat(200)
      const { fetchMock } = await renderLive(anaView(closingGroup), {
        [`POST ${closingObjectPath}`]: noContentAnswer(),
      })
      const dialog = await openObjectSheet()
      fireEvent.change(reasonFieldIn(dialog), { target: { value: reason } })

      fireEvent.click(dialogButton(dialog, closingTexts.send.en))

      await waitFor(() => {
        expect(requestCount(fetchMock, 'POST', closingObjectPath)).toBe(1)
      })
      expect(requestsOf(fetchMock, 'POST', closingObjectPath)[0].body).toEqual({ reason })
    })

    it('closes the sheet, shows a toast, and shows the group as open again for the member after the server answered', async () => {
      const live: LiveGroupState = { group: memberClosingGroup }
      await renderGroupApp(
        routes.group(groupId),
        liveOptions(anaView(closingGroup), live, {
          [`POST ${closingObjectPath}`]: changesGroupTo(live, memberOpenGroup),
        }),
      )
      const dialog = await openObjectSheet()

      fireEvent.click(dialogButton(dialog, closingTexts.send.en))

      await waitForSheetToClose()
      expect(shownToastTexts()).toContain(closingTexts.objected.en)
      await waitFor(() => {
        expect(screen.queryByText(textOf('en', closingTexts.prompt, promptParams))).toBeNull()
      })
      expect(screen.queryByRole('button', { name: 'Confirm' })).toBeNull()
    })

    it('shows the toast in Macedonian for a person who uses Macedonian', async () => {
      const live: LiveGroupState = { group: memberClosingGroup }
      await renderGroupApp(
        routes.group(groupId),
        liveOptions(anaView(closingGroup, 'mk'), live, {
          [`POST ${closingObjectPath}`]: changesGroupTo(live, memberOpenGroup),
        }),
      )
      const dialog = await openObjectSheet('mk')

      fireEvent.click(dialogButton(dialog, closingTexts.send.mk))

      await waitForSheetToClose()
      expect(shownToastTexts()).toContain(closingTexts.objected.mk)
    })

    it('closes with Cancel, sends nothing and starts empty when it is opened again', async () => {
      const { fetchMock } = await renderLive(anaView(closingGroup), {})
      const dialog = await openObjectSheet()
      fireEvent.change(reasonFieldIn(dialog), { target: { value: 'half a thought' } })

      fireEvent.click(dialogButton(dialog, 'Cancel'))
      await waitForSheetToClose()
      const reopened = await openObjectSheet()

      expect(requestCount(fetchMock, 'POST', closingObjectPath)).toBe(0)
      expect(reasonFieldIn(reopened).value).toBe('')
    })

    it('closes with Escape and sends nothing', async () => {
      const { fetchMock } = await renderLive(anaView(closingGroup), {})
      const dialog = await openObjectSheet()

      fireEvent.keyDown(dialog, { key: 'Escape' })

      await waitForSheetToClose()
      expect(requestCount(fetchMock, 'POST', closingObjectPath)).toBe(0)
    })

    it('keeps the bar with Confirm and Object on the screen after the sheet was cancelled', async () => {
      await renderLive(anaView(closingGroup), {})
      const dialog = await openObjectSheet()

      fireEvent.click(dialogButton(dialog, 'Cancel'))
      await waitForSheetToClose()

      expect(await findBarTitled(closingTexts.prompt, 'en', promptParams)).toBeTruthy()
    })

    it('disables Send while the server has not answered and sends one request when it is pressed again', async () => {
      await renderLive(anaView(closingGroup), {})
      const dialog = await openObjectSheet()
      const neverAnswering = neverAnsweringFetch()
      const send = dialogButton(dialog, closingTexts.send.en)

      fireEvent.click(send)
      await expectDisabledWhilePending(send)
      fireEvent.click(send)

      expect(sentBodies(neverAnswering)).toHaveLength(1)
    })

    it.each([
      [400, 'CLOSING_REASON_TOO_LONG'],
      [400, 'GROUP_NOT_CLOSING'],
      [403, 'CLOSING_OWNER_CANNOT_ANSWER'],
      [400, 'GROUP_FINISHED'],
    ])('shows the translated text of %i %s inside the sheet, keeps the reason that was typed and leaves the sheet open', async (status, code) => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      await renderLive(anaView(closingGroup), { [`POST ${closingObjectPath}`]: problemAnswer(status, code) })
      const dialog = await openObjectSheet()
      fireEvent.change(reasonFieldIn(dialog), { target: { value: 'Not yet' } })

      fireEvent.click(dialogButton(dialog, closingTexts.send.en))

      expect((await within(dialog).findByRole('alert')).textContent).toContain(translated('en', `errors.${code}`))
      expect(reasonFieldIn(dialog).value).toBe('Not yet')
      expect(dialogButton(dialog, closingTexts.send.en).matches(':disabled')).toBe(false)
      expect(screen.getByRole('dialog', { name: objectDialogName() })).toBe(dialog)
      expect(vi.mocked(toast.error)).not.toHaveBeenCalled()
      expect(shownToastTexts()).not.toContain(closingTexts.objected.en)
    })

    it('shows the network text inside the sheet when the server cannot be reached', async () => {
      await renderLive(anaView(closingGroup), { [`POST ${closingObjectPath}`]: networkFailureAnswer() })
      const dialog = await openObjectSheet()

      fireEvent.click(dialogButton(dialog, closingTexts.send.en))

      expect((await within(dialog).findByRole('alert')).textContent).toContain(translated('en', 'errors.network'))
    })

    it.each(languages)('writes the title, the label and the buttons in the language of the person (%s)', async (language) => {
      await renderLive(anaView(closingGroup, language), {})

      const dialog = await openObjectSheet(language)

      expect(within(dialog).getByLabelText(closingTexts.reasonLabel[language])).toBe(reasonFieldIn(dialog))
      expect(dialogButton(dialog, closingTexts.send[language])).toBeTruthy()
      expect(
        dialogButton(dialog, language === 'en' ? 'Cancel' : macedonianOf('Cancel')),
      ).toBeTruthy()
    })

    it('shows the error in Macedonian inside the sheet for a person who uses Macedonian', async () => {
      await renderLive(anaView(closingGroup, 'mk'), {
        [`POST ${closingObjectPath}`]: problemAnswer(400, 'CLOSING_REASON_TOO_LONG'),
      })
      const dialog = await openObjectSheet('mk')

      fireEvent.click(dialogButton(dialog, closingTexts.send.mk))

      expect((await within(dialog).findByRole('alert')).textContent).toContain(
        translated('mk', 'errors.CLOSING_REASON_TOO_LONG'),
      )
    })
  })

  describe('Unlock for changes', () => {
    it('sends one POST to the unlock address with no body when the owner taps the button', async () => {
      const { fetchMock } = await renderLive(ownerView(finishedGroup), {
        [`POST ${unlockPath}`]: noContentAnswer(),
      })
      const bar = await findClosingBar(finishedBarText.en)

      fireEvent.click(buttonIn(bar, closingTexts.unlock.en))

      await waitFor(() => {
        expect(requestCount(fetchMock, 'POST', unlockPath)).toBe(1)
      })
      expect(requestsOf(fetchMock, 'POST', unlockPath)[0].body).toBeUndefined()
    })

    it('takes the finished bar away, brings the add expense button back and shows a toast after the server answered', async () => {
      const live: LiveGroupState = { group: finishedGroup }
      await renderGroupApp(
        routes.group(groupId),
        liveOptions(ownerView(finishedGroup), live, {
          [`POST ${unlockPath}`]: changesGroupTo(live, openGroup),
        }),
      )
      const bar = await findClosingBar(finishedBarText.en)

      fireEvent.click(buttonIn(bar, closingTexts.unlock.en))

      expect(await screen.findByRole('link', { name: 'Add expense' })).toBeTruthy()
      expect(screen.queryByText(finishedBarText.en)).toBeNull()
      expect(shownToastTexts()).toContain(closingTexts.unlocked.en)
    })

    it('shows the toast in Macedonian for a person who uses Macedonian', async () => {
      const live: LiveGroupState = { group: finishedGroup }
      await renderGroupApp(
        routes.group(groupId),
        liveOptions(ownerView(finishedGroup, 'mk'), live, {
          [`POST ${unlockPath}`]: changesGroupTo(live, openGroup),
        }),
      )
      const bar = await findClosingBar(finishedBarText.mk, 'mk')

      fireEvent.click(buttonIn(bar, closingTexts.unlock.mk))

      await waitFor(() => {
        expect(shownToastTexts()).toContain(closingTexts.unlocked.mk)
      })
    })

    it('disables the button while the server has not answered and sends one request when it is pressed again', async () => {
      await renderLive(ownerView(finishedGroup), {})
      const bar = await findClosingBar(finishedBarText.en)
      const neverAnswering = neverAnsweringFetch()
      const button = buttonIn(bar, closingTexts.unlock.en)

      fireEvent.click(button)
      await expectDisabledWhilePending(button)
      fireEvent.click(button)

      expect(neverAnswering).toHaveBeenCalledTimes(1)
    })

    it.each([
      [400, 'GROUP_NOT_FINISHED'],
      [403, 'GROUP_NOT_OWNER'],
    ])('shows the translated text of %i %s inside the bar and shows no toast', async (status, code) => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      await renderLive(ownerView(finishedGroup), { [`POST ${unlockPath}`]: problemAnswer(status, code) })
      const bar = await findClosingBar(finishedBarText.en)

      fireEvent.click(buttonIn(bar, closingTexts.unlock.en))

      expect((await within(bar).findByRole('alert')).textContent).toContain(translated('en', `errors.${code}`))
      expect(vi.mocked(toast.error)).not.toHaveBeenCalled()
    })
  })

  describe('a screen that is out of date', () => {
    it('shows the new bar and the translated refusal when a member pays on a group that has been closed in the meantime', async () => {
      const balances = onePaymentBalances(anaMember, filipMember, 70000)
      const live: LiveGroupState = { group: memberOpenGroup }
      await renderGroupApp(routes.groupBalances(groupId), {
        ...liveOptions(anaView(memberOpenGroup), live, {
          [`POST ${settlementsPath}`]: () => {
            live.group = memberClosingGroup
            return problemAnswer(400, 'GROUP_CLOSING')()
          },
        }),
        balances,
      })
      const [card] = await findBalanceCards()
      const sheet = await openMarkPaid(rowWithText(card, 'You pay Filip'), 'I paid Filip')

      fireEvent.click(saveButton())

      expect((await within(sheet).findByRole('alert')).textContent).toContain(translated('en', 'errors.GROUP_CLOSING'))
      fireEvent.click(within(sheet).getByRole('button', { name: 'Cancel' }))
      await waitForSheetToClose()
      expect(await findBarTitled(closingTexts.prompt, 'en', promptParams)).toBeTruthy()
      expect(screen.queryAllByRole('button', { name: /^Mark paid/ })).toEqual([])
    })

    it('shows the finished bar and no Mark paid when the group has been finished in the meantime', async () => {
      const balances = onePaymentBalances(anaMember, filipMember, 70000)
      const live: LiveGroupState = { group: memberOpenGroup }
      await renderGroupApp(routes.groupBalances(groupId), {
        ...liveOptions(anaView(memberOpenGroup), live, {
          [`POST ${settlementsPath}`]: () => {
            live.group = memberFinishedGroup
            return problemAnswer(400, 'GROUP_FINISHED')()
          },
        }),
        balances,
      })
      const [card] = await findBalanceCards()
      const sheet = await openMarkPaid(rowWithText(card, 'You pay Filip'), 'I paid Filip')

      fireEvent.click(saveButton())

      expect((await within(sheet).findByRole('alert')).textContent).toContain(translated('en', 'errors.GROUP_FINISHED'))
      fireEvent.click(within(sheet).getByRole('button', { name: 'Cancel' }))
      await waitForSheetToClose()
      expect(await findClosingBar(finishedBarText.en)).toBeTruthy()
      expect(screen.queryAllByRole('button', { name: /^Mark paid/ })).toEqual([])
    })
  })
})
