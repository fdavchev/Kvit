import { fireEvent, screen, waitFor } from '@testing-library/react'
import { toast } from 'sonner'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { languages } from '@/core/i18n/language'
import type { Group } from '@/core/services/groups/groupsService'
import { freezeTime, groupPath, unfreezeTime } from '@/test/expenseTestHelpers'
import {
  eurPlanOfSixHundred,
  groupWithMkdPlan,
  mkdPlanOfTwoThousand,
  planPath,
  planPut,
  spendingWithPlanOf,
} from '@/test/groupPlanTestData'
import { groupWithoutPlan, openExistingPlanSheet, openNewPlanSheet } from '@/test/groupPlanSheetTestHelpers'
import {
  chooseCurrency,
  findPlanCard,
  ofAmountLine,
  planControl,
  planThatChanges,
  queryPlanSheet,
  removePlanButton,
  requireLine,
  savePlanButton,
  typePlanAmount,
} from '@/test/groupPlanTestHelpers'
import { mkdOnlySpending, spendingPath } from '@/test/groupSpendingTestData'
import { testGroup } from '@/test/groupTestData'
import { neverAnswer, requestCount, requestsOf } from '@/test/requestTestHelpers'
import { stubDeviceColorScheme } from '@/test/themeTestHelpers'
import { translated } from '@/test/translated'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const groupId = testGroup.id

function newEuroPlanAnswers() {
  const changing = planThatChanges(mkdOnlySpending, spendingWithPlanOf(eurPlanOfSixHundred))
  return { [`GET ${spendingPath}`]: changing.spendingAnswer, [planPut]: changing.acceptChange }
}

function raisedPlanAnswers() {
  const changing = planThatChanges(
    spendingWithPlanOf(mkdPlanOfTwoThousand),
    spendingWithPlanOf({ ...mkdPlanOfTwoThousand, amountMinor: 250000, perPersonMinor: 250000 }),
  )
  return { [`GET ${spendingPath}`]: changing.spendingAnswer, [planPut]: changing.acceptChange }
}

function successToastTexts(): unknown[] {
  return vi.mocked(toast.success).mock.calls.map(([text]) => text)
}

function errorToastTexts(): unknown[] {
  return vi.mocked(toast.error).mock.calls.map(([text]) => text)
}

async function settle(): Promise<void> {
  await new Promise((resolve) => setTimeout(resolve, 50))
}

describe('GroupScreen Totals tab, saving the plan', () => {
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

  describe('the request', () => {
    it('sends PUT /api/groups/{id}/plan with 1,500 denars as 150000 minor units and the currency MKD, and nothing else', async () => {
      const { fetchMock, sheet } = await openNewPlanSheet({ answers: newEuroPlanAnswers() })

      typePlanAmount(sheet, '1500')
      fireEvent.click(savePlanButton(sheet))

      await waitFor(() => {
        expect(requestsOf(fetchMock, 'PUT', planPath)).toHaveLength(1)
      })
      expect(requestsOf(fetchMock, 'PUT', planPath)[0].body).toEqual({ amountMinor: 150000, currency: 'MKD' })
      expect(requestCount(fetchMock, 'DELETE', planPath)).toBe(0)
    })

    it('sends 600 euros as 60000 minor units and the currency EUR', async () => {
      const { fetchMock, sheet } = await openNewPlanSheet({ answers: newEuroPlanAnswers() })

      chooseCurrency(sheet, 'EUR')
      typePlanAmount(sheet, '600')
      fireEvent.click(savePlanButton(sheet))

      await waitFor(() => {
        expect(requestsOf(fetchMock, 'PUT', planPath)).toHaveLength(1)
      })
      expect(requestsOf(fetchMock, 'PUT', planPath)[0].body).toEqual({ amountMinor: 60000, currency: 'EUR' })
    })

    it.each([
      ['600.50', 60050],
      ['12,5', 1250],
      ['9999999999.99', 999999999999],
    ])('sends %s euros as %i minor units', async (text, expectedMinor) => {
      const { fetchMock, sheet } = await openNewPlanSheet({ answers: newEuroPlanAnswers() })

      chooseCurrency(sheet, 'EUR')
      typePlanAmount(sheet, text)
      fireEvent.click(savePlanButton(sheet))

      await waitFor(() => {
        expect(requestsOf(fetchMock, 'PUT', planPath)).toHaveLength(1)
      })
      expect(requestsOf(fetchMock, 'PUT', planPath)[0].body).toEqual({ amountMinor: expectedMinor, currency: 'EUR' })
    })

    it('sends the biggest allowed amount of denars: 9,999,999,999 denars', async () => {
      const { fetchMock, sheet } = await openNewPlanSheet({ answers: newEuroPlanAnswers() })

      typePlanAmount(sheet, '9999999999')
      fireEvent.click(savePlanButton(sheet))

      await waitFor(() => {
        expect(requestsOf(fetchMock, 'PUT', planPath)).toHaveLength(1)
      })
      expect(requestsOf(fetchMock, 'PUT', planPath)[0].body).toEqual({ amountMinor: 999999999900, currency: 'MKD' })
    })

    it('sends the new amount of a plan that exists, in the currency it already has', async () => {
      const { fetchMock, sheet } = await openExistingPlanSheet({ answers: raisedPlanAnswers() })

      typePlanAmount(sheet, '2500')
      fireEvent.click(savePlanButton(sheet))

      await waitFor(() => {
        expect(requestsOf(fetchMock, 'PUT', planPath)).toHaveLength(1)
      })
      expect(requestsOf(fetchMock, 'PUT', planPath)[0].body).toEqual({ amountMinor: 250000, currency: 'MKD' })
    })

    it('reads the number that was already there in the new currency when only the currency is changed', async () => {
      const { fetchMock, sheet } = await openExistingPlanSheet({ answers: raisedPlanAnswers() })

      chooseCurrency(sheet, 'EUR')
      fireEvent.click(savePlanButton(sheet))

      await waitFor(() => {
        expect(requestsOf(fetchMock, 'PUT', planPath)).toHaveLength(1)
      })
      expect(requestsOf(fetchMock, 'PUT', planPath)[0].body).toEqual({ amountMinor: 200000, currency: 'EUR' })
    })

    it('sends one request when Save is tapped twice at once, and shows Save as busy', async () => {
      const { fetchMock, sheet } = await openNewPlanSheet({ answers: { [planPut]: neverAnswer() } })
      typePlanAmount(sheet, '1500')

      fireEvent.click(savePlanButton(sheet))
      fireEvent.click(savePlanButton(sheet))

      await settle()
      expect(requestsOf(fetchMock, 'PUT', planPath)).toHaveLength(1)
      expect(savePlanButton(sheet).disabled).toBe(true)
    })

    it('sends one request when the form is submitted twice at once with the keyboard', async () => {
      const { fetchMock, sheet } = await openNewPlanSheet({ answers: { [planPut]: neverAnswer() } })
      typePlanAmount(sheet, '1500')

      fireEvent.submit(savePlanButton(sheet).closest('form') ?? sheet)
      fireEvent.submit(savePlanButton(sheet).closest('form') ?? sheet)

      await settle()
      expect(requestsOf(fetchMock, 'PUT', planPath)).toHaveLength(1)
    })

    it('disables Remove plan while the request is out', async () => {
      const { sheet } = await openExistingPlanSheet({ answers: { [planPut]: neverAnswer() } })
      typePlanAmount(sheet, '2500')

      fireEvent.click(savePlanButton(sheet))

      await waitFor(() => {
        expect(removePlanButton(sheet).disabled).toBe(true)
      })
    })

    it('is two taps and one typed amount from the Totals tab: the row, then Save', async () => {
      const { fetchMock, sheet } = await openNewPlanSheet({ answers: newEuroPlanAnswers() })
      let taps = 1

      typePlanAmount(sheet, '1500')
      fireEvent.click(savePlanButton(sheet))
      taps += 1

      expect(taps).toBe(2)
      await waitFor(() => {
        expect(requestsOf(fetchMock, 'PUT', planPath)).toHaveLength(1)
      })
    })
  })

  describe('when the server accepts', () => {
    it.each(languages)('closes the sheet and shows the text "Saved" in a toast (%s)', async (language) => {
      const { sheet } = await openNewPlanSheet({ language, answers: newEuroPlanAnswers() })

      typePlanAmount(sheet, '1500', language)
      fireEvent.click(savePlanButton(sheet, language))

      await waitFor(() => {
        expect(queryPlanSheet(language)).toBeNull()
      })
      expect(successToastTexts()).toEqual([translated(language, 'groupSettings.saved')])
      expect(errorToastTexts()).toEqual([])
    })

    it('puts the new plan into the cached group without asking for the group again', async () => {
      const { fetchMock, queryClient, sheet } = await openNewPlanSheet({ answers: newEuroPlanAnswers() })
      const groupRequestsBeforeSave = requestCount(fetchMock, 'GET', groupPath)

      chooseCurrency(sheet, 'EUR')
      typePlanAmount(sheet, '600')
      fireEvent.click(savePlanButton(sheet))
      await waitFor(() => {
        expect(queryPlanSheet()).toBeNull()
      })
      await waitFor(() => {
        expect(requestCount(fetchMock, 'GET', `${groupPath}/spending`)).toBe(2)
      })
      await settle()

      expect(queryClient.getQueryData<Group>(['groups', groupId])).toEqual({
        ...groupWithoutPlan,
        planAmountMinor: 60000,
        planCurrency: 'EUR',
      })
      expect(requestCount(fetchMock, 'GET', groupPath)).toBe(groupRequestsBeforeSave)
    })

    it('replaces the amount and currency of the plan in the cached group', async () => {
      const { queryClient, sheet } = await openExistingPlanSheet({ answers: raisedPlanAnswers() })

      typePlanAmount(sheet, '2500')
      fireEvent.click(savePlanButton(sheet))
      await waitFor(() => {
        expect(queryPlanSheet()).toBeNull()
      })

      expect(queryClient.getQueryData<Group>(['groups', groupId])).toEqual({
        ...groupWithMkdPlan,
        planAmountMinor: 250000,
        planCurrency: 'MKD',
      })
    })

    it('marks the spending as out of date and reads it again, so the card shows the new plan', async () => {
      const { fetchMock, sheet } = await openExistingPlanSheet({ answers: raisedPlanAnswers() })

      typePlanAmount(sheet, '2500')
      fireEvent.click(savePlanButton(sheet))

      await waitFor(() => {
        expect(requestCount(fetchMock, 'GET', `${groupPath}/spending`)).toBe(2)
      })
      const card = await findPlanCard()
      await waitFor(() => {
        expect(requireLine(card, ofAmountLine(135000, 250000, 'MKD'))).toBeTruthy()
      })
    })

    it('leaves the list of groups alone, because its answer has no plan', async () => {
      const { fetchMock, sheet } = await openNewPlanSheet({ answers: newEuroPlanAnswers() })

      typePlanAmount(sheet, '1500')
      fireEvent.click(savePlanButton(sheet))
      await waitFor(() => {
        expect(queryPlanSheet()).toBeNull()
      })
      await settle()

      expect(requestCount(fetchMock, 'GET', '/api/groups')).toBe(0)
    })

    it('brings the focus to the plan card that now stands where the "Set a plan" row was', async () => {
      const { sheet } = await openNewPlanSheet({ answers: newEuroPlanAnswers() })

      chooseCurrency(sheet, 'EUR')
      typePlanAmount(sheet, '600')
      fireEvent.click(savePlanButton(sheet))
      await waitFor(() => {
        expect(queryPlanSheet()).toBeNull()
      })

      await waitFor(() => {
        expect(document.activeElement).toBe(planControl())
      })
    })

    it('brings the focus back to the plan card that opened the sheet', async () => {
      const { sheet } = await openExistingPlanSheet({ answers: raisedPlanAnswers() })

      typePlanAmount(sheet, '2500')
      fireEvent.click(savePlanButton(sheet))
      await waitFor(() => {
        expect(queryPlanSheet()).toBeNull()
      })

      await waitFor(() => {
        expect(document.activeElement).toBe(planControl())
      })
    })

    it('leaves no dialog open', async () => {
      const { sheet } = await openExistingPlanSheet({ answers: raisedPlanAnswers() })

      typePlanAmount(sheet, '2500')
      fireEvent.click(savePlanButton(sheet))

      await waitFor(() => {
        expect(screen.queryByRole('dialog')).toBeNull()
      })
    })
  })
})
