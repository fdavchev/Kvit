import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { toast } from 'sonner'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { languages } from '@/core/i18n/language'
import type { Group } from '@/core/services/groups/groupsService'
import { problemResponse } from '@/test/apiTestHelpers'
import { asMemberOf, closingGroup, finishedGroup } from '@/test/closingTestData'
import { freezeTime, groupPath, unfreezeTime } from '@/test/expenseTestHelpers'
import {
  groupWithMkdPlan,
  mkdPlanOfTwoThousand,
  planDelete,
  planPath,
  planPut,
  spendingWithPlanOf,
} from '@/test/groupPlanTestData'
import { groupWithoutPlan, openExistingPlanSheet, openNewPlanSheet } from '@/test/groupPlanSheetTestHelpers'
import {
  chooseCurrency,
  findSetPlanRow,
  planAmountField,
  planCard,
  planControlOf,
  planThatChanges,
  pressedCurrencies,
  queryPlanCard,
  queryPlanSheet,
  removePlanButton,
  savePlanButton,
  setPlanRow,
  typePlanAmount,
} from '@/test/groupPlanTestHelpers'
import { mkdOnlySpending, spendingPath } from '@/test/groupSpendingTestData'
import { testGroup } from '@/test/groupTestData'
import {
  networkFailureAnswer,
  neverAnswer,
  problemAnswer,
  requestCount,
  requestsOf,
} from '@/test/requestTestHelpers'
import { stubDeviceColorScheme } from '@/test/themeTestHelpers'
import { referenceDangerToastStyling, stylingOf, toastShownWithText } from '@/test/toastTestHelpers'
import { translated } from '@/test/translated'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const groupId = testGroup.id
const spendingUrl = `${groupPath}/spending`
const serverError = () => new Response(null, { status: 500 })
const finishedGroupWithPlan: Group = { ...finishedGroup, planAmountMinor: 200000, planCurrency: 'MKD' }
const closingGroupWithPlan: Group = { ...closingGroup, planAmountMinor: 200000, planCurrency: 'MKD' }

function errorToastTexts(): unknown[] {
  return vi.mocked(toast.error).mock.calls.map(([text]) => text)
}

function successToastTexts(): unknown[] {
  return vi.mocked(toast.success).mock.calls.map(([text]) => text)
}

function removedPlanAnswers() {
  const changing = planThatChanges(spendingWithPlanOf(mkdPlanOfTwoThousand), mkdOnlySpending)
  return { [`GET ${spendingPath}`]: changing.spendingAnswer, [planDelete]: changing.acceptChange }
}

function groupThatChangesWhenRefused(after: Group, refusal: Response) {
  const live = { group: groupWithMkdPlan }
  return {
    [`GET ${groupPath}`]: () => Response.json(live.group),
    [planPut]: () => {
      live.group = after
      return refusal
    },
  }
}

async function settle(): Promise<void> {
  await new Promise((resolve) => setTimeout(resolve, 50))
}

describe('GroupScreen Totals tab, removing and refusing', () => {
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

  describe('removing the plan', () => {
    it('sends DELETE /api/groups/{id}/plan at once, with no body and no question', async () => {
      const { fetchMock, sheet } = await openExistingPlanSheet({ answers: removedPlanAnswers() })

      fireEvent.click(removePlanButton(sheet))

      expect(screen.queryByRole('alertdialog')).toBeNull()
      await waitFor(() => {
        expect(requestsOf(fetchMock, 'DELETE', planPath)).toHaveLength(1)
      })
      expect(requestsOf(fetchMock, 'DELETE', planPath)[0].body).toBeUndefined()
      expect(requestCount(fetchMock, 'PUT', planPath)).toBe(0)
    })

    it('closes the sheet and shows "Plan removed" as a red toast without Undo', async () => {
      const { sheet } = await openExistingPlanSheet({ answers: removedPlanAnswers() })

      fireEvent.click(removePlanButton(sheet))

      await waitFor(() => {
        expect(queryPlanSheet()).toBeNull()
      })
      const shown = await waitFor(() => toastShownWithText('Plan removed'))
      expect(shown.options.action).toBeUndefined()
      expect(stylingOf(shown)).toEqual(referenceDangerToastStyling())
      expect(successToastTexts()).toEqual([])
    })

    it('shows the toast in Macedonian', async () => {
      const { sheet } = await openExistingPlanSheet({ language: 'mk', answers: removedPlanAnswers() })

      fireEvent.click(removePlanButton(sheet, 'mk'))

      await waitFor(() => {
        expect(toastShownWithText('Планот е отстранет')).toBeTruthy()
      })
    })

    it('sets the plan fields of the cached group to null without asking for the group again', async () => {
      const { fetchMock, queryClient, sheet } = await openExistingPlanSheet({ answers: removedPlanAnswers() })
      const groupRequestsBeforeRemoving = requestCount(fetchMock, 'GET', groupPath)

      fireEvent.click(removePlanButton(sheet))
      await waitFor(() => {
        expect(queryPlanSheet()).toBeNull()
      })
      await waitFor(() => {
        expect(requestCount(fetchMock, 'GET', spendingUrl)).toBe(2)
      })
      await settle()

      expect(queryClient.getQueryData<Group>(['groups', groupId])).toEqual({
        ...groupWithMkdPlan,
        planAmountMinor: null,
        planCurrency: null,
      })
      expect(requestCount(fetchMock, 'GET', groupPath)).toBe(groupRequestsBeforeRemoving)
    })

    it('reads the spending again, takes the card away and puts the "Set a plan" row in its place', async () => {
      const { fetchMock, sheet } = await openExistingPlanSheet({ answers: removedPlanAnswers() })

      fireEvent.click(removePlanButton(sheet))

      await waitFor(() => {
        expect(requestCount(fetchMock, 'GET', spendingUrl)).toBe(2)
      })
      expect(await findSetPlanRow()).toBeTruthy()
      expect(queryPlanCard()).toBeNull()
    })

    it('brings the focus to the "Set a plan" row, because the card it came from is gone', async () => {
      const { sheet } = await openExistingPlanSheet({ answers: removedPlanAnswers() })

      fireEvent.click(removePlanButton(sheet))
      await waitFor(() => {
        expect(queryPlanSheet()).toBeNull()
      })

      await waitFor(() => {
        expect(document.activeElement).toBe(setPlanRow())
      })
    })

    it('sends only the DELETE, also when something was typed and not saved', async () => {
      const { fetchMock, sheet } = await openExistingPlanSheet({ answers: removedPlanAnswers() })
      typePlanAmount(sheet, '2500')

      fireEvent.click(removePlanButton(sheet))

      await waitFor(() => {
        expect(requestsOf(fetchMock, 'DELETE', planPath)).toHaveLength(1)
      })
      expect(requestCount(fetchMock, 'PUT', planPath)).toBe(0)
    })

    it('sends one request when Remove plan is tapped twice at once', async () => {
      const { fetchMock, sheet } = await openExistingPlanSheet({ answers: { [planDelete]: neverAnswer() } })

      fireEvent.click(removePlanButton(sheet))
      fireEvent.click(removePlanButton(sheet))

      await settle()
      expect(requestsOf(fetchMock, 'DELETE', planPath)).toHaveLength(1)
      expect(savePlanButton(sheet).disabled).toBe(true)
    })
  })

  describe('when the removal fails', () => {
    it.each(languages)('shows the network message as an error toast and keeps the sheet and the plan (%s)', async (language) => {
      const { queryClient, sheet } = await openExistingPlanSheet({
        language,
        answers: { [planDelete]: networkFailureAnswer() },
      })

      fireEvent.click(removePlanButton(sheet, language))

      await waitFor(() => {
        expect(errorToastTexts()).toEqual([translated(language, 'errors.network')])
      })
      expect(screen.getByRole('dialog')).toBe(sheet)
      expect(planAmountField(sheet, language).value).toBe('2000')
      expect(queryClient.getQueryData<Group>(['groups', groupId])).toEqual(groupWithMkdPlan)
      expect(() => toastShownWithText(translated(language, 'groupPlan.removed'))).toThrow()
    })

    it.each([
      ['a server error', serverError, 'errors.generic'],
      ['GROUP_NOT_OWNER', problemAnswer(403, 'GROUP_NOT_OWNER'), 'errors.GROUP_NOT_OWNER'],
    ])('shows the text for %s as an error toast and keeps the sheet', async (_name, answer, key) => {
      const { sheet } = await openExistingPlanSheet({ answers: { [planDelete]: answer } })

      fireEvent.click(removePlanButton(sheet))

      await waitFor(() => {
        expect(errorToastTexts()).toEqual([translated('en', key)])
      })
      expect(screen.getByRole('dialog')).toBe(sheet)
    })

    it('lets Remove plan be tapped again after a failure', async () => {
      const { fetchMock, sheet } = await openExistingPlanSheet({ answers: { [planDelete]: networkFailureAnswer() } })
      fireEvent.click(removePlanButton(sheet))
      await waitFor(() => {
        expect(errorToastTexts()).toHaveLength(1)
      })

      fireEvent.click(removePlanButton(sheet))

      await waitFor(() => {
        expect(requestsOf(fetchMock, 'DELETE', planPath)).toHaveLength(2)
      })
    })
  })

  describe('when saving is refused for the amount, the currency or the connection', () => {
    it.each([
      [400, 'GROUP_PLAN_AMOUNT_INVALID'],
      [400, 'GROUP_CURRENCY_INVALID'],
    ])('shows the translated text for %i %s above Save, keeps the typed amount and the chip, and keeps the sheet', async (status, code) => {
      const { sheet } = await openNewPlanSheet({ answers: { [planPut]: problemAnswer(status, code) } })
      chooseCurrency(sheet, 'EUR')
      typePlanAmount(sheet, '600')

      fireEvent.click(savePlanButton(sheet))

      const alert = await within(sheet).findByRole('alert')
      expect(alert.textContent).toBe(translated('en', `errors.${code}`))
      expect(alert.compareDocumentPosition(savePlanButton(sheet)) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy()
      expect(planAmountField(sheet).value).toBe('600')
      expect(pressedCurrencies(sheet)).toEqual(['EUR'])
      expect(screen.getByRole('dialog')).toBe(sheet)
      expect(errorToastTexts()).toEqual([])
    })

    it.each(languages)('shows the network message above Save and keeps the typed amount (%s)', async (language) => {
      const { sheet } = await openNewPlanSheet({ language, answers: { [planPut]: networkFailureAnswer() } })
      typePlanAmount(sheet, '1500', language)

      fireEvent.click(savePlanButton(sheet, language))

      expect((await within(sheet).findByRole('alert')).textContent).toBe(translated(language, 'errors.network'))
      expect(planAmountField(sheet, language).value).toBe('1500')
      expect(screen.getByRole('dialog')).toBe(sheet)
    })

    it('shows the generic message above Save after a server error', async () => {
      const { sheet } = await openNewPlanSheet({ answers: { [planPut]: serverError } })
      typePlanAmount(sheet, '1500')

      fireEvent.click(savePlanButton(sheet))

      expect((await within(sheet).findByRole('alert')).textContent).toBe(translated('en', 'errors.generic'))
    })

    it('shows the text of GROUP_NOT_FOUND above Save, like the other sheets of a group do, and keeps the sheet', async () => {
      const { sheet } = await openNewPlanSheet({ answers: { [planPut]: problemAnswer(404, 'GROUP_NOT_FOUND') } })
      typePlanAmount(sheet, '1500')

      fireEvent.click(savePlanButton(sheet))

      expect((await within(sheet).findByRole('alert')).textContent).toBe(translated('en', 'errors.GROUP_NOT_FOUND'))
      expect(screen.getByRole('dialog')).toBe(sheet)
      expect(errorToastTexts()).toEqual([])
    })

    it('enables Save again after a refusal and sends the second try', async () => {
      const { fetchMock, sheet } = await openNewPlanSheet({ answers: { [planPut]: networkFailureAnswer() } })
      typePlanAmount(sheet, '1500')
      fireEvent.click(savePlanButton(sheet))
      await within(sheet).findByRole('alert')

      expect(savePlanButton(sheet).disabled).toBe(false)
      fireEvent.click(savePlanButton(sheet))

      await waitFor(() => {
        expect(requestsOf(fetchMock, 'PUT', planPath)).toHaveLength(2)
      })
    })

    it('changes nothing in the cached group and shows no "Saved" toast', async () => {
      const { queryClient, sheet } = await openNewPlanSheet({ answers: { [planPut]: problemAnswer(400, 'GROUP_PLAN_AMOUNT_INVALID') } })
      typePlanAmount(sheet, '1500')

      fireEvent.click(savePlanButton(sheet))
      await within(sheet).findByRole('alert')

      expect(queryClient.getQueryData<Group>(['groups', groupId])).toEqual(groupWithoutPlan)
      expect(successToastTexts()).toEqual([])
    })
  })

  describe('when saving is refused because the group changed', () => {
    it.each(languages)('shows GROUP_NOT_OWNER as an error toast, reads the group and the spending again, closes the sheet and takes the button off the card (%s)', async (language) => {
      const { fetchMock, sheet } = await openExistingPlanSheet({
        language,
        answers: groupThatChangesWhenRefused(asMemberOf(groupWithMkdPlan), problemResponse(403, 'GROUP_NOT_OWNER')),
      })
      typePlanAmount(sheet, '2500', language)

      fireEvent.click(savePlanButton(sheet, language))

      await waitFor(() => {
        expect(errorToastTexts()).toEqual([translated(language, 'errors.GROUP_NOT_OWNER')])
      })
      await waitFor(() => {
        expect(queryPlanSheet(language)).toBeNull()
      })
      await waitFor(() => {
        expect(requestCount(fetchMock, 'GET', groupPath)).toBeGreaterThanOrEqual(2)
        expect(requestCount(fetchMock, 'GET', spendingUrl)).toBeGreaterThanOrEqual(2)
      })
      await waitFor(() => {
        expect(planControlOf(planCard(language))).toBeNull()
      })
    })

    it.each([
      ['GROUP_FINISHED', finishedGroupWithPlan],
      ['GROUP_CLOSING', asMemberOf(closingGroupWithPlan)],
    ])('shows %s as an error toast, reads the group and the spending again, closes the sheet and takes the button off the card', async (code, groupAfter) => {
      const { fetchMock, sheet } = await openExistingPlanSheet({
        answers: groupThatChangesWhenRefused(groupAfter, problemResponse(400, code)),
      })
      typePlanAmount(sheet, '2500')

      fireEvent.click(savePlanButton(sheet))

      await waitFor(() => {
        expect(errorToastTexts()).toEqual([translated('en', `errors.${code}`)])
      })
      await waitFor(() => {
        expect(queryPlanSheet()).toBeNull()
      })
      await waitFor(() => {
        expect(requestCount(fetchMock, 'GET', groupPath)).toBeGreaterThanOrEqual(2)
        expect(requestCount(fetchMock, 'GET', spendingUrl)).toBeGreaterThanOrEqual(2)
      })
      await waitFor(() => {
        expect(planControlOf(planCard())).toBeNull()
      })
    })

    it('shows no inline message above Save for these refusals, because the sheet closes', async () => {
      const { sheet } = await openExistingPlanSheet({
        answers: groupThatChangesWhenRefused(asMemberOf(groupWithMkdPlan), problemResponse(403, 'GROUP_NOT_OWNER')),
      })
      typePlanAmount(sheet, '2500')

      fireEvent.click(savePlanButton(sheet))

      await waitFor(() => {
        expect(queryPlanSheet()).toBeNull()
      })
      expect(screen.queryByRole('alert')).toBeNull()
      expect(successToastTexts()).toEqual([])
    })

    it('sends no second request on its own', async () => {
      const { fetchMock, sheet } = await openExistingPlanSheet({
        answers: groupThatChangesWhenRefused(asMemberOf(groupWithMkdPlan), problemResponse(403, 'GROUP_NOT_OWNER')),
      })
      typePlanAmount(sheet, '2500')

      fireEvent.click(savePlanButton(sheet))
      await waitFor(() => {
        expect(queryPlanSheet()).toBeNull()
      })
      await settle()

      expect(requestsOf(fetchMock, 'PUT', planPath)).toHaveLength(1)
    })
  })
})
