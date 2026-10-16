import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { languages, type Language } from '@/core/i18n/language'
import { routes } from '@/core/router/routes'
import {
  dashboardActivityRowOf,
  dashboardOf,
  danaJoinedRow,
  expenseEditedRow,
  flatGroupId,
  greeceGroupId,
  longMacedonianGroupName,
  longNameGroupId,
  paymentRecordedRow,
  taxiAddedRow,
} from '@/test/dashboardTestData'
import { freezeTime, unfreezeTime } from '@/test/expenseTestHelpers'
import { plainTextOf, renderHome, textMatching } from '@/test/homeTestHelpers'
import { translated } from '@/test/translated'
import { formatMoney } from '@/shared/utils/formatMoney'

function taxiSentence(language: Language = 'en'): string {
  return translated(language, 'activity.expenseAdded', {
    name: 'Ana',
    title: 'Taxi',
    amount: formatMoney(90000, 'MKD', language),
  })
}

function paymentSentence(language: Language = 'en'): string {
  return translated(language, 'activity.settlementRecorded', {
    name: 'Marko',
    from: 'Marko',
    to: 'Filip',
    amount: formatMoney(60000, 'MKD', language),
  })
}

function joinedSentence(language: Language = 'en'): string {
  return translated(language, 'activity.memberJoined', { member: 'Dana' })
}

function editedSentence(language: Language = 'en'): string {
  return translated(language, 'home.activityExpenseEdited', { name: 'Ana' })
}

async function findRow(sentence: string, language: Language = 'en'): Promise<HTMLElement> {
  await screen.findByRole('heading', { name: translated(language, 'home.recentActivity') })
  const row = screen.getByText(textMatching(sentence)).closest('li')
  if (row === null) {
    throw new Error(`The activity sentence "${sentence}" is not inside a list item`)
  }
  return row
}

describe('the recent activity of the home screen', () => {
  beforeEach(() => {
    freezeTime()
  })

  afterEach(() => {
    unfreezeTime()
    vi.unstubAllGlobals()
  })

  it.each(languages)('has the heading "Recent activity" in %s', async (language) => {
    await renderHome({ language })

    expect(await screen.findByRole('heading', { name: translated(language, 'home.recentActivity') })).toBeTruthy()
  })

  it.each(languages)('writes each event as the same sentence the activity tab of a group uses, in %s', async (language) => {
    await renderHome({ language })
    await screen.findByRole('heading', { name: translated(language, 'home.recentActivity') })

    for (const sentence of [taxiSentence(language), paymentSentence(language), joinedSentence(language)]) {
      expect(screen.getByText(textMatching(sentence))).toBeTruthy()
    }
  })

  it.each(languages)('writes an edit as a short sentence without old and new values in %s', async (language) => {
    await renderHome({ language })
    const row = await findRow(editedSentence(language), language)

    expect(plainTextOf(row)).not.toMatch(/→|1\s?200|900/)
  })

  it('shows the rows in the order the server sent them', async () => {
    await renderHome()
    const rows = [
      await findRow(taxiSentence()),
      await findRow(paymentSentence()),
      await findRow(joinedSentence()),
      await findRow(editedSentence()),
    ]

    expect(new Set(rows.map((row) => row.parentElement)).size).toBe(1)
    expect(Array.from(rows[0].parentElement?.children ?? [])).toEqual(rows)
  })

  it('shows the name of the group on each row', async () => {
    await renderHome()

    expect(plainTextOf(await findRow(taxiSentence()))).toContain('Greece trip')
    expect(plainTextOf(await findRow(joinedSentence()))).toContain('Flat')
    expect(plainTextOf(await findRow(editedSentence()))).toContain(longMacedonianGroupName)
  })

  it('shows the group name in an element of its own, apart from the sentence', async () => {
    await renderHome()
    const row = await findRow(taxiSentence())

    expect(within(row).getByText('Greece trip')).not.toBe(within(row).getByText(textMatching(taxiSentence())))
  })

  it.each([
    ['en', 'time.hours_other', '2', taxiSentence],
    ['en', 'time.hours_other', '5', paymentSentence],
    ['en', 'time.days_one', '1', joinedSentence],
    ['en', 'time.days_other', '2', editedSentence],
  ] as const)('shows how long ago it was in %s: %s with %s', async (language, key, count, sentenceOf) => {
    await renderHome({ language })
    const row = await findRow(sentenceOf(language), language)

    expect(plainTextOf(row)).toContain(translated(language, key, { count }))
  })

  it('shows how long ago it was in Macedonian', async () => {
    await renderHome({ language: 'mk' })
    const row = await findRow(taxiSentence('mk'), 'mk')

    expect(plainTextOf(row)).toContain(translated('mk', 'time.hours_other', { count: '2' }))
  })

  it.each([
    [taxiSentence, greeceGroupId],
    [joinedSentence, flatGroupId],
    [editedSentence, longNameGroupId],
  ])('opens the group of the row when the row is tapped', async (sentenceOf, groupId) => {
    const { router } = await renderHome()
    const row = await findRow(sentenceOf())

    fireEvent.click(within(row).getByRole('link'))

    await waitFor(() => {
      expect(router.state.location.pathname).toBe(routes.group(groupId))
    })
  })

  it('links the row to the group address', async () => {
    await renderHome()
    const row = await findRow(taxiSentence())

    expect(within(row).getByRole('link').getAttribute('href')).toBe(routes.group(greeceGroupId))
  })

  it('shows at most the first 5 rows when the server sends more', async () => {
    const sixRows = Array.from({ length: 6 }, (_unused, index) =>
      dashboardActivityRowOf(
        {
          id: `0f000000-0000-4000-8000-0000000001${index}0`,
          type: 'MemberJoined',
          actorName: `Person${index}`,
          data: { name: `Person${index}` },
          createdAt: '2026-10-06T09:00:00Z',
        },
        greeceGroupId,
        'Greece trip',
      ),
    )
    await renderHome({ dashboard: dashboardOf({ recentActivity: sixRows }) })
    await screen.findByRole('heading', { name: translated('en', 'home.recentActivity') })

    for (let index = 0; index < 5; index += 1) {
      expect(screen.getByText(textMatching(translated('en', 'activity.memberJoined', { member: `Person${index}` })))).toBeTruthy()
    }
    expect(screen.queryByText(textMatching(translated('en', 'activity.memberJoined', { member: 'Person5' })))).toBeNull()
  })

  it('shows all 5 rows when the server sends exactly 5', async () => {
    const fiveRows = [taxiAddedRow, paymentRecordedRow, danaJoinedRow, expenseEditedRow, { ...taxiAddedRow, id: 'fifth' }]
    await renderHome({ dashboard: dashboardOf({ recentActivity: fiveRows }) })
    await screen.findByRole('heading', { name: translated('en', 'home.recentActivity') })

    expect(screen.getAllByText(textMatching(taxiSentence()))).toHaveLength(2)
  })

  it('shows no heading and no rows when there is no activity yet', async () => {
    await renderHome({ dashboard: dashboardOf({ recentActivity: [] }) })
    await screen.findByRole('heading', { name: translated('en', 'home.groups') })

    expect(screen.queryByRole('heading', { name: translated('en', 'home.recentActivity') })).toBeNull()
  })

  it('shows own actions too, because the decision is that everyone\'s actions are listed', async () => {
    const ownRow = dashboardActivityRowOf(
      {
        id: '0f000000-0000-4000-8000-000000000077',
        type: 'ExpenseAdded',
        actorUserId: '0b6f2a52-6c1e-4f55-9a37-3f2d8c1b7e10',
        actorName: 'Filip',
        data: { title: 'Dinner', amountMinor: 120000, currency: 'MKD' },
        createdAt: '2026-10-06T09:30:00Z',
      },
      greeceGroupId,
      'Greece trip',
    )
    await renderHome({ dashboard: dashboardOf({ recentActivity: [ownRow] }) })
    await screen.findByRole('heading', { name: translated('en', 'home.recentActivity') })

    const sentence = translated('en', 'activity.expenseAdded', {
      name: 'Filip',
      title: 'Dinner',
      amount: formatMoney(120000, 'MKD', 'en'),
    })
    expect(screen.getByText(textMatching(sentence))).toBeTruthy()
  })
})
