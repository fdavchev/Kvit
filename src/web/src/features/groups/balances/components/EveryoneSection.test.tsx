import { screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import type { ExpensePerson } from '@/features/expenses/shared/expensePeople'
import { languages, type Language } from '@/core/i18n/language'
import { formatSignedMoney } from '@/shared/utils/formatSignedMoney'
import { plainSpaces } from '@/test/expenseTestHelpers'
import { renderElementWithProviders } from '@/test/renderWithProviders'
import type { EveryoneCardView } from '../balancesView'
import { EveryoneSection } from './EveryoneSection'

const twoWordName = 'Александра Миладиновска'
const singleLongWord = 'Aaaaaaaaaaaaaaaaaaaaaaaaa'

function personNamed(name: string, colorIndex: number): ExpensePerson {
  return {
    memberId: `a0000000-0000-4000-8000-00000000000${colorIndex}`,
    name,
    pictureUrl: null,
    colorIndex,
    isYou: false,
    isCurrentMember: true,
  }
}

const cards: EveryoneCardView[] = [
  {
    currency: 'MKD',
    rows: [
      { person: personNamed(twoWordName, 1), balanceMinor: -123456700 },
      { person: personNamed(singleLongWord, 2), balanceMinor: 150000 },
      { person: personNamed('Ana', 3), balanceMinor: 0 },
    ],
  },
  { currency: 'EUR', rows: [{ person: personNamed('Marko', 4), balanceMinor: -2000 }] },
]

function classesOf(element: HTMLElement): string[] {
  return Array.from(element.classList)
}

async function renderedRow(name: string, language: Language = 'en') {
  await renderElementWithProviders(<EveryoneSection cards={cards} language={language} />, '/everyone', { language })
  const nameElement = screen.getByText(name)
  const wrappingRow = nameElement.parentElement
  if (wrappingRow === null) {
    throw new Error(`Expected the name "${name}" to sit inside a row: ${document.body.innerHTML}`)
  }
  return { nameElement, wrappingRow }
}

function amountIn(wrappingRow: HTMLElement, amountMinor: number, currency: 'MKD' | 'EUR', language: Language = 'en'): HTMLElement {
  const amountText = plainSpaces(formatSignedMoney(amountMinor, currency, language))
  const amount = Array.from(wrappingRow.children).find((child) => plainSpaces(child.textContent ?? '') === amountText)
  if (!(amount instanceof HTMLElement)) {
    throw new Error(`Expected the amount "${amountText}" beside the name in: ${wrappingRow.outerHTML}`)
  }
  return amount
}

describe('the rows of the Everyone section with a long name', () => {
  it('puts the name and the amount in one row that may wrap, so the amount drops under a long name', async () => {
    const { nameElement, wrappingRow } = await renderedRow(twoWordName)

    expect(classesOf(wrappingRow)).toEqual(expect.arrayContaining(['flex', 'flex-wrap', 'min-w-0', 'flex-1']))
    expect(wrappingRow.contains(nameElement)).toBe(true)
    expect(amountIn(wrappingRow, -123456700, 'MKD')).toBeTruthy()
  })

  it('lets the name shrink and grow and breaks it only at word edges, never letter by letter', async () => {
    const { nameElement } = await renderedRow(twoWordName)

    expect(classesOf(nameElement)).toEqual(expect.arrayContaining(['min-w-0', 'grow', 'break-words']))
    expect(classesOf(nameElement)).not.toContain('wrap-anywhere')
    expect(classesOf(nameElement)).not.toContain('truncate')
  })

  it('keeps a single 25-letter word breakable with break-words and in the same wrapping row as its amount', async () => {
    const { nameElement, wrappingRow } = await renderedRow(singleLongWord)

    expect(classesOf(nameElement)).toContain('break-words')
    expect(amountIn(wrappingRow, 150000, 'MKD')).toBeTruthy()
  })

  it.each(languages)('keeps every amount on one line and on the right when it shares the row (%s)', async (language) => {
    await renderElementWithProviders(<EveryoneSection cards={cards} language={language} />, '/everyone', { language })

    const amounts: [string, number, 'MKD' | 'EUR'][] = [
      [twoWordName, -123456700, 'MKD'],
      [singleLongWord, 150000, 'MKD'],
      ['Ana', 0, 'MKD'],
      ['Marko', -2000, 'EUR'],
    ]
    for (const [name, amountMinor, currency] of amounts) {
      const wrappingRow = screen.getByText(name).parentElement
      if (wrappingRow === null) {
        throw new Error(`Expected the name "${name}" to sit inside a row`)
      }
      const amount = amountIn(wrappingRow, amountMinor, currency, language)
      expect(classesOf(amount), `the amount of ${name}`).toEqual(expect.arrayContaining(['whitespace-nowrap', 'ml-auto']))
    }
  })
})
