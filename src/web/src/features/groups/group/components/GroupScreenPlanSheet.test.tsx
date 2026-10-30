import { screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { languages } from '@/core/i18n/language'
import { freezeTime, isBefore, unfreezeTime } from '@/test/expenseTestHelpers'
import { dismissSheetByTappingBackdrop } from '@/test/expenseFormTestHelpers'
import {
  eurAmountCases,
  eurPlanOfSixHundred,
  groupWithEurPlan,
  groupWithMkdPlan,
  mkdAmountCases,
  planPath,
  spendingWithPlanOf,
} from '@/test/groupPlanTestData'
import {
  chooseCurrency,
  openPlanSheetFrom,
  planAmountField,
  planCurrencyChip,
  pressedCurrencies,
  queryPlanSheet,
  queryRemovePlanButton,
  removePlanButton,
  savePlanButton,
  typePlanAmount,
} from '@/test/groupPlanTestHelpers'
import { isDescribedBy } from '@/test/describedByTestHelpers'
import { groupOf } from '@/test/groupTestData'
import { requestCount } from '@/test/requestTestHelpers'
import { stubDeviceColorScheme } from '@/test/themeTestHelpers'
import { groupWithoutPlan, openEuroPlanSheet, openExistingPlanSheet, openNewPlanSheet } from '@/test/groupPlanSheetTestHelpers'
import { translated } from '@/test/translated'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const amountInvalidText = 'Type an amount above 0 (whole denars for MKD).'
const eurDefaultGroup = groupOf({ defaultCurrency: 'EUR' })

describe('GroupScreen Totals tab, the plan sheet', () => {
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

  describe('what it shows', () => {
    it.each([
      ['en', 'Spending plan'],
      ['mk', 'План за трошоците'],
    ] as const)('is titled with the name of the plan and opens from the "Set a plan" row (%s)', async (language, title) => {
      await openNewPlanSheet({ language })

      expect(screen.getByRole('dialog', { name: title })).toBeTruthy()
    })

    it('opens from the plan card when there is a plan', async () => {
      const { sheet } = await openExistingPlanSheet()

      expect(sheet).toBeTruthy()
    })

    it('puts the cursor in the amount field at once and asks for the number keyboard', async () => {
      const { sheet } = await openNewPlanSheet()

      await waitFor(() => {
        expect(document.activeElement).toBe(planAmountField(sheet))
      })
      expect(planAmountField(sheet).getAttribute('inputmode')).toBe('numeric')
    })

    it('asks for the decimal keyboard when the plan is in euros', async () => {
      const { sheet } = await openEuroPlanSheet()

      expect(planAmountField(sheet).getAttribute('inputmode')).toBe('decimal')
    })

    it('leaves the amount empty for a new plan', async () => {
      const { sheet } = await openNewPlanSheet()

      expect(planAmountField(sheet).value).toBe('')
    })

    it.each([
      ['a plan of 2,000 MKD', () => openExistingPlanSheet(), '2000'],
      ['a plan of 600 EUR', () => openEuroPlanSheet(), '600'],
    ])('has the amount of %s typed in, in whole units', async (_name, open, expectedText) => {
      const { sheet } = await open()

      expect(planAmountField(sheet).value).toBe(expectedText)
    })

    it('has the cents of a plan in euros typed in', async () => {
      const { sheet } = await openExistingPlanSheet({
        group: { ...groupWithEurPlan, planAmountMinor: 60050 },
        spending: spendingWithPlanOf({ ...eurPlanOfSixHundred, amountMinor: 60050 }),
      })

      expect(planAmountField(sheet).value).toBe('600.50')
    })

    it.each(languages)('says under the field that everyone in the group sees the plan (%s)', async (language) => {
      const { sheet } = await openNewPlanSheet({ language })

      const hint = within(sheet).getByText(translated(language, 'groupPlan.hint'))

      expect(isBefore(planAmountField(sheet, language), hint)).toBe(true)
      expect(isBefore(hint, savePlanButton(sheet, language))).toBe(true)
    })

    it('shows the Remove plan button only when there is a plan, below Save', async () => {
      const { sheet } = await openExistingPlanSheet()

      expect(isBefore(savePlanButton(sheet), removePlanButton(sheet))).toBe(true)
    })

    it('shows no Remove plan button for a new plan', async () => {
      const { sheet } = await openNewPlanSheet()

      expect(queryRemovePlanButton(sheet)).toBeNull()
    })
  })

  describe('the currency chips', () => {
    it.each(languages)('offers exactly the two chips MKD and EUR, as buttons that show which one is chosen (%s)', async (language) => {
      const { sheet } = await openNewPlanSheet({ language })

      expect(planCurrencyChip(sheet, 'MKD', language).tagName).toBe('BUTTON')
      expect(planCurrencyChip(sheet, 'EUR', language).tagName).toBe('BUTTON')
      expect(pressedCurrencies(sheet, language)).toHaveLength(1)
    })

    it.each([
      ['a group in denars', groupWithoutPlan, 'MKD'],
      ['a group in euros', eurDefaultGroup, 'EUR'],
    ] as const)('starts a new plan in the currency of %s', async (_name, group, expected) => {
      const { sheet } = await openNewPlanSheet({ group })

      expect(pressedCurrencies(sheet)).toEqual([expected])
    })

    it('starts the plan of a group in euros in the currency of the plan, which is denars', async () => {
      const { sheet } = await openExistingPlanSheet({ group: { ...groupWithMkdPlan, defaultCurrency: 'EUR' } })

      expect(pressedCurrencies(sheet)).toEqual(['MKD'])
    })

    it('starts the plan of a group in denars in the currency of the plan, which is euros', async () => {
      const { sheet } = await openEuroPlanSheet({ group: { ...groupWithEurPlan, defaultCurrency: 'MKD' } })

      expect(pressedCurrencies(sheet)).toEqual(['EUR'])
    })

    it('switches the currency with one tap and keeps what was typed', async () => {
      const { sheet } = await openNewPlanSheet()
      typePlanAmount(sheet, '1500')

      chooseCurrency(sheet, 'EUR')

      await waitFor(() => {
        expect(pressedCurrencies(sheet)).toEqual(['EUR'])
      })
      expect(planAmountField(sheet).value).toBe('1500')
    })

    it('asks for the decimal keyboard after a switch to euros', async () => {
      const { sheet } = await openNewPlanSheet()

      chooseCurrency(sheet, 'EUR')

      await waitFor(() => {
        expect(planAmountField(sheet).getAttribute('inputmode')).toBe('decimal')
      })
    })
  })

  describe('when Save can be tapped for a new plan', () => {
    it('is off while the amount is empty', async () => {
      const { sheet } = await openNewPlanSheet()

      expect(savePlanButton(sheet).disabled).toBe(true)
    })

    it.each(mkdAmountCases)('has Save enabled: $isValid, for $label in denars ("$text")', async ({ text, isValid }) => {
      const { sheet } = await openNewPlanSheet()

      typePlanAmount(sheet, text)

      expect(savePlanButton(sheet).disabled).toBe(!isValid)
    })

    it.each(eurAmountCases)('has Save enabled: $isValid, for $label in euros ("$text")', async ({ text, isValid }) => {
      const { sheet } = await openNewPlanSheet()
      chooseCurrency(sheet, 'EUR')

      typePlanAmount(sheet, text)

      expect(savePlanButton(sheet).disabled).toBe(!isValid)
    })

    it('turns off again when the amount is deleted', async () => {
      const { sheet } = await openNewPlanSheet()
      typePlanAmount(sheet, '1500')

      typePlanAmount(sheet, '')

      expect(savePlanButton(sheet).disabled).toBe(true)
    })

    it('turns off when the currency is switched from euros to denars while the amount has cents', async () => {
      const { sheet } = await openNewPlanSheet()
      chooseCurrency(sheet, 'EUR')
      typePlanAmount(sheet, '12.5')
      expect(savePlanButton(sheet).disabled).toBe(false)

      chooseCurrency(sheet, 'MKD')

      await waitFor(() => {
        expect(savePlanButton(sheet).disabled).toBe(true)
      })
    })
  })

  describe('when Save can be tapped for a plan that exists', () => {
    it('is off while nothing has changed', async () => {
      const { sheet } = await openExistingPlanSheet()

      expect(savePlanButton(sheet).disabled).toBe(true)
    })

    it('is on once the amount is a different valid amount', async () => {
      const { sheet } = await openExistingPlanSheet()

      typePlanAmount(sheet, '2500')

      expect(savePlanButton(sheet).disabled).toBe(false)
    })

    it.each(['2000', '02000'])('is off again when the amount is typed back to the same plan (%s)', async (sameAmount) => {
      const { sheet } = await openExistingPlanSheet()
      typePlanAmount(sheet, '2500')

      typePlanAmount(sheet, sameAmount)

      expect(savePlanButton(sheet).disabled).toBe(true)
    })

    it('is on when only the currency is changed, although the typed number is the same', async () => {
      const { sheet } = await openExistingPlanSheet()

      chooseCurrency(sheet, 'EUR')

      await waitFor(() => {
        expect(savePlanButton(sheet).disabled).toBe(false)
      })
    })

    it('is off again when the currency is changed back', async () => {
      const { sheet } = await openExistingPlanSheet()
      chooseCurrency(sheet, 'EUR')
      await waitFor(() => {
        expect(savePlanButton(sheet).disabled).toBe(false)
      })

      chooseCurrency(sheet, 'MKD')

      await waitFor(() => {
        expect(savePlanButton(sheet).disabled).toBe(true)
      })
    })

    it('is on for the same number of minor units in another currency: 600 EUR typed as 600 denars', async () => {
      const { sheet } = await openEuroPlanSheet()

      chooseCurrency(sheet, 'MKD')

      await waitFor(() => {
        expect(savePlanButton(sheet).disabled).toBe(false)
      })
    })

    it.each(['', '0', '12.5', 'abc', '10000000000'])('is off for the amount "%s", which is not a valid amount of denars', async (text) => {
      const { sheet } = await openExistingPlanSheet()

      typePlanAmount(sheet, text)

      expect(savePlanButton(sheet).disabled).toBe(true)
    })
  })

  describe('the message about an amount that cannot be used', () => {
    it.each(mkdAmountCases.filter((amountCase) => !amountCase.isValid))('shows the message under the field for $label in denars ("$text")', async ({ text }) => {
      const { sheet } = await openNewPlanSheet()

      typePlanAmount(sheet, text)

      const message = within(sheet).getByText(amountInvalidText)
      expect(isBefore(planAmountField(sheet), message)).toBe(true)
      expect(isBefore(message, savePlanButton(sheet))).toBe(true)
      expect(isDescribedBy(planAmountField(sheet), message)).toBe(true)
      expect(planAmountField(sheet).getAttribute('aria-invalid')).toBe('true')
      expect(savePlanButton(sheet).disabled).toBe(true)
    })

    it.each(eurAmountCases.filter((amountCase) => !amountCase.isValid))('shows the message under the field for $label in euros ("$text")', async ({ text }) => {
      const { sheet } = await openNewPlanSheet()
      chooseCurrency(sheet, 'EUR')

      typePlanAmount(sheet, text)

      const message = within(sheet).getByText(amountInvalidText)
      expect(isDescribedBy(planAmountField(sheet), message)).toBe(true)
      expect(planAmountField(sheet).getAttribute('aria-invalid')).toBe('true')
      expect(savePlanButton(sheet).disabled).toBe(true)
    })

    it('shows no message and no red edge while the field is empty', async () => {
      const { sheet } = await openNewPlanSheet()

      expect(within(sheet).queryByText(amountInvalidText)).toBeNull()
      expect(planAmountField(sheet).getAttribute('aria-invalid')).toBe('false')
    })

    it.each(mkdAmountCases.filter((amountCase) => amountCase.isValid))('shows no message for $label in denars ("$text")', async ({ text }) => {
      const { sheet } = await openNewPlanSheet()

      typePlanAmount(sheet, text)

      expect(within(sheet).queryByText(amountInvalidText)).toBeNull()
      expect(planAmountField(sheet).getAttribute('aria-invalid')).toBe('false')
    })

    it.each(eurAmountCases.filter((amountCase) => amountCase.isValid))('shows no message for $label in euros ("$text")', async ({ text }) => {
      const { sheet } = await openNewPlanSheet()
      chooseCurrency(sheet, 'EUR')

      typePlanAmount(sheet, text)

      expect(within(sheet).queryByText(amountInvalidText)).toBeNull()
      expect(planAmountField(sheet).getAttribute('aria-invalid')).toBe('false')
    })

    it('shows no message for the amount of the plan that exists', async () => {
      const { sheet } = await openExistingPlanSheet()

      expect(within(sheet).queryByText(amountInvalidText)).toBeNull()
    })

    it('goes away once the amount is corrected', async () => {
      const { sheet } = await openNewPlanSheet()
      typePlanAmount(sheet, '12.5')
      expect(within(sheet).getByText(amountInvalidText)).toBeTruthy()

      typePlanAmount(sheet, '12')

      expect(within(sheet).queryByText(amountInvalidText)).toBeNull()
      expect(planAmountField(sheet).getAttribute('aria-invalid')).toBe('false')
    })

    it('goes away when the currency is switched to euros, which accept cents', async () => {
      const { sheet } = await openNewPlanSheet()
      typePlanAmount(sheet, '12.5')
      expect(within(sheet).getByText(amountInvalidText)).toBeTruthy()

      chooseCurrency(sheet, 'EUR')

      await waitFor(() => {
        expect(within(sheet).queryByText(amountInvalidText)).toBeNull()
      })
    })

    it('comes when the currency is switched to denars with cents typed', async () => {
      const { sheet } = await openNewPlanSheet()
      chooseCurrency(sheet, 'EUR')
      typePlanAmount(sheet, '12.5')
      expect(within(sheet).queryByText(amountInvalidText)).toBeNull()

      chooseCurrency(sheet, 'MKD')

      await waitFor(() => {
        expect(within(sheet).getByText(amountInvalidText)).toBeTruthy()
      })
    })

    it.each(languages)('writes the message in the language of the app (%s)', async (language) => {
      const { sheet } = await openNewPlanSheet({ language })

      typePlanAmount(sheet, '0', language)

      const message = within(sheet).getByText(translated(language, 'groupPlan.amountInvalid'))
      expect(isDescribedBy(planAmountField(sheet, language), message)).toBe(true)
    })
  })

  describe('closing without saving', () => {
    it('sends nothing, closes the sheet and brings the focus back to the card', async () => {
      const { fetchMock, sheet, control } = await openExistingPlanSheet()
      typePlanAmount(sheet, '2500')

      dismissSheetByTappingBackdrop()

      await waitFor(() => {
        expect(queryPlanSheet()).toBeNull()
      })
      await waitFor(() => {
        expect(document.activeElement).toBe(control)
      })
      expect(requestCount(fetchMock, 'PUT', planPath)).toBe(0)
      expect(requestCount(fetchMock, 'DELETE', planPath)).toBe(0)
    })

    it('opens with the saved plan again, not the unsaved typing', async () => {
      const { sheet, control } = await openExistingPlanSheet()
      typePlanAmount(sheet, '2500')
      dismissSheetByTappingBackdrop()
      await waitFor(() => {
        expect(queryPlanSheet()).toBeNull()
      })

      const reopened = await openPlanSheetFrom(control)

      expect(planAmountField(reopened).value).toBe('2000')
    })

    it('brings the focus back to the "Set a plan" row', async () => {
      const { sheet, row } = await openNewPlanSheet()
      typePlanAmount(sheet, '2500')

      dismissSheetByTappingBackdrop()

      await waitFor(() => {
        expect(queryPlanSheet()).toBeNull()
      })
      await waitFor(() => {
        expect(document.activeElement).toBe(row)
      })
      expect(screen.queryByRole('dialog')).toBeNull()
    })
  })
})
