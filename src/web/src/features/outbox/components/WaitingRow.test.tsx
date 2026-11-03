import { fireEvent, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { languages, type Language } from '@/core/i18n/language'
import { money } from '@/test/budgetLeftCardTestHelpers'
import { minHeightStepOf } from '@/test/groupSpendingTestHelpers'
import { outboxText } from '@/test/outboxTextsTestData'
import { tapTargetOf, waitingRows, waitingRowText } from '@/test/outboxScreenTestHelpers'
import { renderElementWithProviders } from '@/test/renderWithProviders'
import { seedMe, testMe } from '@/test/testMe'
import type { Currency } from '@/shared/utils/formatMoney'
import { WaitingRow } from './WaitingRow'

interface RenderOptions {
  language?: Language
  title?: string
  amountMinor?: number
  currency?: Currency
  detail?: string
  showsRateNote?: boolean
  onOpen?: () => void
}

async function renderRow(options: RenderOptions = {}) {
  const language = options.language ?? 'en'
  await renderElementWithProviders(
    <WaitingRow
      title={options.title ?? 'Museum tickets'}
      amountMinor={options.amountMinor ?? 150000}
      currency={options.currency ?? 'MKD'}
      detail={options.detail ?? 'Paid by Ana'}
      {...(options.showsRateNote === undefined ? {} : { showsRateNote: options.showsRateNote })}
      onOpen={options.onOpen ?? (() => {})}
    />,
    '/waiting',
    { language, seedCache: seedMe({ ...testMe, language }) },
  )
  const [row] = waitingRows()
  return row
}

function dashedElementsOf(row: HTMLElement): HTMLElement[] {
  return [row, ...Array.from(row.querySelectorAll<HTMLElement>('*'))].filter((element) =>
    element.classList.contains('border-dashed'),
  )
}

function opacityClassesOf(element: Element): string[] {
  return Array.from(element.classList).filter((token) => /^opacity-\d+$/.test(token))
}

function opacityClassesAround(element: HTMLElement): string[] {
  const around: string[] = []
  for (let ancestor = element.parentElement; ancestor !== null; ancestor = ancestor.parentElement) {
    around.push(...opacityClassesOf(ancestor))
  }
  return around
}

function opacityClassesInside(row: HTMLElement): string[] {
  return Array.from(row.querySelectorAll<HTMLElement>('*')).flatMap(opacityClassesOf)
}

function opacityClassesFromRowTo(row: HTMLElement, element: HTMLElement): string[] {
  const found: string[] = []
  for (let current: HTMLElement | null = element; current !== null && current !== row; current = current.parentElement) {
    found.push(...opacityClassesOf(current))
  }
  return found
}

function opacityStepOf(token: string): number {
  return Number(token.slice('opacity-'.length))
}

describe('WaitingRow', () => {
  it('is marked as a waiting row so the lists and the tests can find it', async () => {
    const row = await renderRow()

    expect(row).toBeTruthy()
    expect(waitingRows()).toHaveLength(1)
  })

  it('shows the title', async () => {
    const row = await renderRow({ title: 'Museum tickets' })

    expect(waitingRowText(row)).toContain('Museum tickets')
  })

  it.each(languages)('shows the amount like the real rows do, with its currency (%s)', async (language) => {
    const row = await renderRow({ language, amountMinor: 150000, currency: 'MKD' })

    expect(waitingRowText(row)).toContain(money(150000, 'MKD', language))
  })

  it('shows an amount in euros with the euro sign of the real rows', async () => {
    const row = await renderRow({ amountMinor: 4550, currency: 'EUR' })

    expect(waitingRowText(row)).toContain(money(4550, 'EUR', 'en'))
  })

  it.each(languages)('writes the detail followed by "Not synced yet" in its second line (%s)', async (language) => {
    await renderRow({ language, detail: 'Paid by Ana' })

    expect(screen.getByText(`Paid by Ana · ${outboxText(language, 'outbox.notSynced')}`)).toBeTruthy()
  })

  it('writes the title before the second line', async () => {
    const row = await renderRow({ title: 'Museum tickets', detail: 'Paid by Ana' })

    const text = waitingRowText(row)

    expect(text.indexOf('Museum tickets')).toBeLessThan(text.indexOf('Paid by Ana'))
  })

  it('is a button that can be tapped, not a link', async () => {
    const row = await renderRow()

    const tapTarget = tapTargetOf(row)

    expect(tapTarget.tagName).toBe('BUTTON')
    expect(row.closest('a')).toBeNull()
    expect(row.querySelector('a')).toBeNull()
  })

  it('has a tap target at least 44 px tall', async () => {
    const row = await renderRow()

    expect(minHeightStepOf(tapTargetOf(row))).toBeGreaterThanOrEqual(11)
  })

  it('calls onOpen once when it is tapped', async () => {
    const onOpen = vi.fn()
    const row = await renderRow({ onOpen })

    fireEvent.click(tapTargetOf(row))

    expect(onOpen).toHaveBeenCalledTimes(1)
  })

  it('has a dashed border', async () => {
    const row = await renderRow()

    expect(dashedElementsOf(row)).not.toEqual([])
  })

  it('draws the dashed edge at full strength: no opacity class on the element that has it or on any element around it', async () => {
    const row = await renderRow()

    const dashedElements = dashedElementsOf(row)

    expect(dashedElements).not.toEqual([])
    for (const dashedElement of dashedElements) {
      expect(opacityClassesOf(dashedElement)).toEqual([])
      expect(opacityClassesAround(dashedElement)).toEqual([])
    }
  })

  it('draws the dashed edge with the border colour or the stronger field border colour', async () => {
    const row = await renderRow()

    for (const dashedElement of dashedElementsOf(row)) {
      const looks = Array.from(dashedElement.classList)
      expect(looks.includes('border-border') || looks.includes('border-field-border')).toBe(true)
    }
  })

  it('greys the content inside the row by lowering its opacity a little, to 80 percent or more', async () => {
    const row = await renderRow()

    const opacitySteps = opacityClassesInside(row).map(opacityStepOf)

    expect(opacitySteps.length).toBeGreaterThan(0)
    expect(opacitySteps.every((step) => step >= 80 && step < 100)).toBe(true)
  })

  it('never lowers the opacity of the text below 80 percent, so it keeps a contrast of 4.5 to 1', async () => {
    const row = await renderRow({ currency: 'EUR', amountMinor: 4550 })

    const opacitySteps = [...opacityClassesOf(row), ...opacityClassesInside(row)].map(opacityStepOf)

    expect(opacitySteps.every((step) => step >= 80)).toBe(true)
  })

  it('puts the title, the second line and the amount under the greyed content', async () => {
    const row = await renderRow({ title: 'Museum tickets', detail: 'Paid by Ana' })

    const texts = [
      screen.getByText('Museum tickets'),
      screen.getByText(`Paid by Ana · ${outboxText('en', 'outbox.notSynced')}`),
      screen.getByText(money(150000, 'MKD', 'en')),
    ]

    for (const text of texts) {
      expect(opacityClassesFromRowTo(row, text).length).toBeGreaterThan(0)
    }
  })

  it.each(languages)('tells that the euro price is set when sent on a euro row (%s)', async (language) => {
    const row = await renderRow({ language, currency: 'EUR', amountMinor: 4550 })

    expect(waitingRowText(row)).toContain(outboxText(language, 'outbox.rateWhenSent'))
  })

  it.each(languages)('tells that the euro price is set when sent on a euro row when showsRateNote is true (%s)', async (language) => {
    const row = await renderRow({ language, currency: 'EUR', amountMinor: 4550, showsRateNote: true })

    expect(waitingRowText(row)).toContain(outboxText(language, 'outbox.rateWhenSent'))
  })

  it.each(languages)('leaves out the euro price line on a euro row when showsRateNote is false (%s)', async (language) => {
    const row = await renderRow({ language, currency: 'EUR', amountMinor: 4550, showsRateNote: false })

    expect(waitingRowText(row)).not.toContain(outboxText(language, 'outbox.rateWhenSent'))
  })

  it('still shows the amount in euros and the second line when showsRateNote is false', async () => {
    const row = await renderRow({ currency: 'EUR', amountMinor: 4550, detail: 'Paid by Ana', showsRateNote: false })

    expect(waitingRowText(row)).toContain(money(4550, 'EUR', 'en'))
    expect(waitingRowText(row)).toContain(`Paid by Ana · ${outboxText('en', 'outbox.notSynced')}`)
  })

  it('does not talk about the euro price on a denar row', async () => {
    const row = await renderRow({ currency: 'MKD' })

    expect(waitingRowText(row)).not.toContain(outboxText('en', 'outbox.rateWhenSent'))
  })

  it('puts the euro price line in a line of its own, apart from the second line', async () => {
    await renderRow({ currency: 'EUR', amountMinor: 4550, detail: 'Paid by Ana' })

    const secondLine = screen.getByText(`Paid by Ana · ${outboxText('en', 'outbox.notSynced')}`)
    const rateLine = screen.getByText(outboxText('en', 'outbox.rateWhenSent'))

    expect(rateLine).not.toBe(secondLine)
    expect(secondLine.contains(rateLine)).toBe(false)
  })
})
