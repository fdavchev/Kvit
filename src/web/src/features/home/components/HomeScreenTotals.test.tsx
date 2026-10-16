import { screen } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { languages, type Language } from '@/core/i18n/language'
import { amountParagraphIn } from '@/test/balancesTestHelpers'
import { dashboardOf, fullDashboard, newUserDashboard } from '@/test/dashboardTestData'
import { plainSpaces } from '@/test/expenseTestHelpers'
import { regionNamed, renderHome } from '@/test/homeTestHelpers'
import { translated } from '@/test/translated'
import { formatMoney } from '@/shared/utils/formatMoney'

const dashAfterTheLabel = /^[-–—−]$/

function amountIn(region: HTMLElement, amountMinor: number, currency: 'MKD' | 'EUR', language: Language): HTMLElement {
  return amountParagraphIn(region, plainSpaces(formatMoney(amountMinor, currency, language)))
}

async function findBoxes(language: Language = 'en'): Promise<{ owed: HTMLElement; owe: HTMLElement }> {
  await screen.findByRole('region', { name: translated(language, 'home.owed') })
  return {
    owed: regionNamed(translated(language, 'home.owed')),
    owe: regionNamed(translated(language, 'home.owe')),
  }
}

describe('the two totals boxes of the home screen', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it.each(languages)('shows a box named "You are owed" and a box named "You owe" in %s', async (language) => {
    await renderHome({ language })

    const { owed, owe } = await findBoxes(language)

    expect(owed).not.toBe(owe)
  })

  it.each(languages)('lists every currency the person is owed in the first box, MKD and EUR apart, in %s', async (language) => {
    await renderHome({ language })
    const { owed } = await findBoxes(language)

    expect(amountIn(owed, 185000, 'MKD', language)).toBeTruthy()
    expect(amountIn(owed, 2000, 'EUR', language)).toBeTruthy()
    expect(plainSpaces(owed.textContent ?? '')).not.toContain(formatMoney(60000, 'MKD', language))
  })

  it.each(languages)('lists every currency the person owes in the second box in %s', async (language) => {
    await renderHome({ language })
    const { owe } = await findBoxes(language)

    expect(amountIn(owe, 60000, 'MKD', language)).toBeTruthy()
    expect(plainSpaces(owe.textContent ?? '')).not.toContain(formatMoney(2000, 'EUR', language))
  })

  it('writes the owed amounts in the positive colour', async () => {
    await renderHome()
    const { owed } = await findBoxes()

    expect(amountIn(owed, 185000, 'MKD', 'en').classList.contains('text-positive')).toBe(true)
    expect(amountIn(owed, 2000, 'EUR', 'en').classList.contains('text-positive')).toBe(true)
  })

  it('writes the amounts the person owes in the negative colour', async () => {
    await renderHome()
    const { owe } = await findBoxes()

    expect(amountIn(owe, 60000, 'MKD', 'en').classList.contains('text-negative')).toBe(true)
    expect(amountIn(owe, 60000, 'MKD', 'en').classList.contains('text-positive')).toBe(false)
  })

  it('gives the two boxes different background classes, never the same colour', async () => {
    await renderHome()
    const { owed, owe } = await findBoxes()

    const backgroundOf = (box: HTMLElement): string[] =>
      [...box.classList].filter((name) => name.startsWith('bg-') || name.includes(':bg-'))

    expect(backgroundOf(owed).length).toBeGreaterThan(0)
    expect(backgroundOf(owe).length).toBeGreaterThan(0)
    expect(backgroundOf(owed)).not.toEqual(backgroundOf(owe))
  })

  it('writes the amounts without a plus or a minus sign, because the box already says the direction', async () => {
    await renderHome()
    const { owed, owe } = await findBoxes()

    expect(plainSpaces(owed.textContent ?? '')).not.toMatch(/[+−-]\s?\d/)
    expect(plainSpaces(owe.textContent ?? '')).not.toMatch(/[+−-]\s?\d/)
  })

  it('shows a dash in a box that has no amount, next to its label', async () => {
    await renderHome({
      dashboard: dashboardOf({ totals: { owed: [{ currency: 'MKD', amountMinor: 185000 }], owe: [] } }),
    })
    const { owe } = await findBoxes()

    const rest = plainSpaces(owe.textContent ?? '').replace(translated('en', 'home.owe'), '').trim()

    expect(rest).toMatch(dashAfterTheLabel)
    expect(rest).not.toMatch(/NaN|undefined|null|0/)
  })

  it('shows both boxes with dashes when the groups all cancel out', async () => {
    await renderHome({ dashboard: dashboardOf({ totals: { owed: [], owe: [] } }) })
    const { owed, owe } = await findBoxes()

    for (const box of [owed, owe]) {
      const label = box === owed ? translated('en', 'home.owed') : translated('en', 'home.owe')
      expect(plainSpaces(box.textContent ?? '').replace(label, '').trim()).toMatch(dashAfterTheLabel)
    }
  })

  it('shows no totals boxes for a person without groups', async () => {
    await renderHome({ dashboard: newUserDashboard })
    await screen.findByText(translated('en', 'home.noGroupsTitle'))

    expect(screen.queryByRole('region', { name: translated('en', 'home.owed') })).toBeNull()
    expect(screen.queryByRole('region', { name: translated('en', 'home.owe') })).toBeNull()
  })

  it('shows the totals of the saved dashboard straight away, before the server has answered', async () => {
    await renderHome({
      seedCache: (queryClient) => queryClient.setQueryData(['dashboard'], fullDashboard),
    })

    expect(regionNamed(translated('en', 'home.owed'))).toBeTruthy()
  })
})
