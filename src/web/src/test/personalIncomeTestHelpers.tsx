import { fireEvent, screen } from '@testing-library/react'
import { vi } from 'vitest'
import type { Language } from '@/core/i18n/language'
import { PersonalIncomeSheet } from '@/features/budget/entries/components/PersonalIncomeSheet'
import { choiceButton, findChoiceSheet, renderSheet, type SheetOptions } from './personalEntryTestHelpers'
import { translated } from './translated'

export async function renderIncomeSheet(options: SheetOptions = {}) {
  const onClose = vi.fn<() => void>()
  return renderSheet(<PersonalIncomeSheet entry={options.entry} recurringIncome={options.recurringIncome} onClose={onClose} />, onClose, options)
}

export async function findIncomeSheet(language: Language = 'en'): Promise<HTMLElement> {
  return screen.findByRole('dialog', { name: translated(language, 'budget.income.title') })
}

export async function chooseIncome(language: Language = 'en'): Promise<HTMLElement> {
  fireEvent.click(choiceButton(await findChoiceSheet(language), 'income', language))
  return findIncomeSheet(language)
}
