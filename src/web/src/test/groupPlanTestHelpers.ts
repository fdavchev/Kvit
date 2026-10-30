import { fireEvent, screen, within } from '@testing-library/react'
import type { Language } from '@/core/i18n/language'
import { currencyLabel, type Currency } from '@/shared/utils/formatMoney'
import { money, readableText } from './budgetLeftCardTestHelpers'
import { plainSpaces } from './expenseTestHelpers'
import type { AnswerFactory } from './requestTestHelpers'
import { translated } from './translated'

export function planHeading(language: Language = 'en'): string {
  return translated(language, 'groupPlan.heading')
}

export function findPlanCard(language: Language = 'en'): Promise<HTMLElement> {
  return screen.findByRole('region', { name: planHeading(language) })
}

export function queryPlanCard(language: Language = 'en'): HTMLElement | null {
  return screen.queryByRole('region', { name: planHeading(language) })
}

export function planCard(language: Language = 'en'): HTMLElement {
  return screen.getByRole('region', { name: planHeading(language) })
}

export function planControlOf(card: HTMLElement): HTMLElement | null {
  const enclosing = card.closest('button')
  if (enclosing !== null) {
    return enclosing
  }
  const buttons = within(card).queryAllByRole('button')
  if (buttons.length > 1) {
    throw new Error(`The plan card has ${buttons.length} buttons, but the whole card should be one control`)
  }
  return buttons[0] ?? null
}

export function planControl(language: Language = 'en'): HTMLElement {
  const control = planControlOf(planCard(language))
  if (control === null) {
    throw new Error('The plan card has no control that opens the plan sheet')
  }
  return control
}

export function planBarOf(card: HTMLElement, language: Language = 'en'): HTMLElement {
  return within(card).getByRole('progressbar', { name: planHeading(language) })
}

export function lineWithText(card: HTMLElement, text: string): HTMLElement | null {
  const wanted = plainSpaces(text)
  const matches = [card, ...Array.from(card.querySelectorAll<HTMLElement>('*'))].filter(
    (element) => readableText(element) === wanted,
  )
  return matches.at(-1) ?? null
}

export function requireLine(card: HTMLElement, text: string): HTMLElement {
  const line = lineWithText(card, text)
  if (line === null) {
    throw new Error(`The plan card has no line reading "${text}". Its text is: "${readableText(card)}"`)
  }
  return line
}

export function ofAmountLine(
  spentMinor: number,
  amountMinor: number,
  currency: Currency,
  language: Language = 'en',
): string {
  return translated(language, 'groupPlan.ofAmount', {
    spent: money(spentMinor, currency, language),
    amount: money(amountMinor, currency, language),
  })
}

export function onTrackLine(language: Language = 'en'): string {
  return translated(language, 'groupPlan.onTrack')
}

export function nearLine(leftMinor: number, currency: Currency, language: Language = 'en'): string {
  return translated(language, 'groupPlan.near', { amount: money(leftMinor, currency, language) })
}

export function overLine(overMinor: number, currency: Currency, language: Language = 'en'): string {
  return translated(language, 'groupPlan.over', { amount: money(overMinor, currency, language) })
}

export function includedLine(
  includedMinor: number,
  includedCurrency: Currency,
  planCurrency: Currency,
  language: Language = 'en',
): string {
  return translated(language, 'groupPlan.included', {
    amount: money(includedMinor, includedCurrency, language),
    currency: currencyLabel(planCurrency, language),
  })
}

export function setPlanRow(language: Language = 'en'): HTMLElement {
  return screen.getByRole('button', { name: translated(language, 'groupPlan.setRow') })
}

export function querySetPlanRow(language: Language = 'en'): HTMLElement | null {
  return screen.queryByRole('button', { name: translated(language, 'groupPlan.setRow') })
}

export function findSetPlanRow(language: Language = 'en'): Promise<HTMLElement> {
  return screen.findByRole('button', { name: translated(language, 'groupPlan.setRow') })
}

export function findPlanSheet(language: Language = 'en'): Promise<HTMLElement> {
  return screen.findByRole('dialog', { name: translated(language, 'groupPlan.title') })
}

export function queryPlanSheet(language: Language = 'en'): HTMLElement | null {
  return screen.queryByRole('dialog', { name: translated(language, 'groupPlan.title') })
}

export async function openPlanSheetFrom(opener: HTMLElement, language: Language = 'en'): Promise<HTMLElement> {
  opener.focus()
  fireEvent.click(opener)
  return findPlanSheet(language)
}

export function planAmountField(sheet: HTMLElement, language: Language = 'en'): HTMLInputElement {
  return within(sheet).getByLabelText<HTMLInputElement>(translated(language, 'expense.amount'))
}

export function typePlanAmount(sheet: HTMLElement, text: string, language: Language = 'en'): void {
  fireEvent.change(planAmountField(sheet, language), { target: { value: text } })
}

export function planCurrencyChip(sheet: HTMLElement, currency: Currency, language: Language = 'en'): HTMLElement {
  return within(sheet).getByRole('button', { name: currencyLabel(currency, language) })
}

export function chooseCurrency(sheet: HTMLElement, currency: Currency, language: Language = 'en'): void {
  fireEvent.click(planCurrencyChip(sheet, currency, language))
}

export function pressedCurrencies(sheet: HTMLElement, language: Language = 'en'): Currency[] {
  return (['MKD', 'EUR'] as const).filter(
    (currency) => planCurrencyChip(sheet, currency, language).getAttribute('aria-pressed') === 'true',
  )
}

export function savePlanButton(sheet: HTMLElement, language: Language = 'en'): HTMLButtonElement {
  return within(sheet).getByRole<HTMLButtonElement>('button', { name: translated(language, 'groupSettings.save') })
}

export function removePlanButton(sheet: HTMLElement, language: Language = 'en'): HTMLButtonElement {
  return within(sheet).getByRole<HTMLButtonElement>('button', { name: translated(language, 'groupPlan.remove') })
}

export function queryRemovePlanButton(sheet: HTMLElement, language: Language = 'en'): HTMLElement | null {
  return within(sheet).queryByRole('button', { name: translated(language, 'groupPlan.remove') })
}

export interface ChangingPlan {
  spendingAnswer: AnswerFactory
  acceptChange: AnswerFactory
}

export function planThatChanges(before: unknown, after: unknown): ChangingPlan {
  let current = before
  return {
    spendingAnswer: () => Response.json(current),
    acceptChange: () => {
      current = after
      return new Response(null, { status: 204 })
    },
  }
}
