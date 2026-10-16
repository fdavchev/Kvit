import { screen, within } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { languages, type Language } from '@/core/i18n/language'
import { routes } from '@/core/router/routes'
import { amountParagraphIn } from '@/test/balancesTestHelpers'
import { greeceGroupId, newUserDashboard } from '@/test/dashboardTestData'
import { plainSpaces } from '@/test/expenseTestHelpers'
import { regionNamed, renderHome } from '@/test/homeTestHelpers'
import { stubDesktopScreen } from '@/test/matchMediaTestHelpers'
import { translated } from '@/test/translated'
import { formatMoney } from '@/shared/utils/formatMoney'

function actionNames(language: Language): string[] {
  return [
    translated(language, 'home.plusAddTo', { group: 'Greece trip' }),
    translated(language, 'home.plusNewOneBill'),
    translated(language, 'home.plusAnotherGroup'),
  ]
}

async function findOwedBox(language: Language = 'en'): Promise<HTMLElement> {
  await screen.findByRole('region', { name: translated(language, 'home.owed') })
  return regionNamed(translated(language, 'home.owed'))
}

describe('the home screen on a desktop-sized window', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it.each(languages)('shows the items of the plus menu as round buttons under the totals, in %s', async (language) => {
    stubDesktopScreen()
    await renderHome({ language })
    await findOwedBox(language)

    const links = actionNames(language).map((name) => screen.getByRole('link', { name }))

    expect(links.map((link) => link.getAttribute('href'))).toEqual([
      routes.groupExpenseNew(greeceGroupId),
      routes.newBill,
      routes.groups,
    ])
    for (const link of links) {
      expect(link.className.split(/\s+/)).toEqual(expect.arrayContaining(['rounded-full', 'min-h-12']))
    }
  })

  it('draws Add to in the main button colour and the others as outlined buttons', async () => {
    stubDesktopScreen()
    await renderHome()
    await findOwedBox()

    const [addTo, oneBill, another] = actionNames('en').map((name) => screen.getByRole('link', { name }))

    expect(addTo.className).toContain('bg-primary')
    expect(oneBill.className).toContain('border-field-border')
    expect(another.className).toContain('border-field-border')
  })

  it('puts the round buttons after the totals and before the Needs you bar', async () => {
    stubDesktopScreen()
    await renderHome()
    const owed = await findOwedBox()

    const addTo = screen.getByRole('link', { name: actionNames('en')[0] })
    const needsYou = screen.getByRole('button', { expanded: false, name: /2/ })

    expect(owed.compareDocumentPosition(addTo) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy()
    expect(addTo.compareDocumentPosition(needsYou) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy()
  })

  it('shows no floating plus button, because the round buttons replace it', async () => {
    stubDesktopScreen()
    await renderHome()
    await findOwedBox()

    expect(screen.queryByRole('button', { name: translated('en', 'home.plus') })).toBeNull()
  })

  it('shows no round buttons and no plus for a person without groups', async () => {
    stubDesktopScreen()
    await renderHome({ dashboard: newUserDashboard })
    await screen.findByText(translated('en', 'home.noGroupsTitle'))

    expect(screen.queryByRole('link', { name: translated('en', 'home.plusNewOneBill') })).toBeNull()
    expect(screen.queryByRole('button', { name: translated('en', 'home.plus') })).toBeNull()
  })

  it('makes the totals huge on desktop with a clamp, still drawn by the rolling amount', async () => {
    stubDesktopScreen()
    await renderHome()
    const owed = await findOwedBox()

    const amount = amountParagraphIn(owed, plainSpaces(formatMoney(185000, 'MKD', 'en')))

    expect(amount.className).toContain('lg:text-[clamp(2.25rem,5vw,4.25rem)]')
    expect(amount.querySelector('number-flow-react')).not.toBeNull()
  })

  it('takes the coloured box away on desktop so the numbers sit on the page', async () => {
    stubDesktopScreen()
    await renderHome()
    const owed = await findOwedBox()

    expect(owed.className.split(/\s+/)).toEqual(expect.arrayContaining(['bg-positive-surface', 'lg:bg-transparent', 'lg:p-0']))
  })

  it('lays the group cards out in a grid that fills the width with cards at least 12 rem wide', async () => {
    stubDesktopScreen()
    await renderHome()
    const groups = await screen.findByRole('region', { name: translated('en', 'home.groups') })

    const grid = within(groups).getByRole('list')

    expect(grid.className.split(/\s+/)).toEqual(
      expect.arrayContaining(['grid-cols-2', 'lg:grid-cols-[repeat(auto-fill,minmax(min(100%,12rem),1fr))]']),
    )
  })

  it('puts Your groups and Recent activity side by side when there is room and one under the other when not', async () => {
    stubDesktopScreen()
    await renderHome()
    const groups = await screen.findByRole('region', { name: translated('en', 'home.groups') })
    const activity = screen.getByRole('region', { name: translated('en', 'home.recentActivity') })

    const pair = groups.parentElement

    expect(activity.parentElement).toBe(pair)
    expect(pair?.className.split(/\s+/)).toEqual(
      expect.arrayContaining(['contents', 'lg:grid', 'lg:grid-cols-[repeat(auto-fit,minmax(min(100%,26rem),1fr))]']),
    )
  })

  it('uses the wide page width on desktop', async () => {
    stubDesktopScreen()
    await renderHome()
    await findOwedBox()

    expect(screen.getByRole('main').className).toContain('lg:max-w-[64rem]')
  })
})

describe('the home screen on a phone-sized window', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('keeps the floating plus button and shows no round buttons', async () => {
    await renderHome()
    await findOwedBox()

    expect(screen.getByRole('button', { name: translated('en', 'home.plus') })).toBeTruthy()
    for (const name of actionNames('en')) {
      expect(screen.queryByRole('link', { name })).toBeNull()
    }
  })
})
