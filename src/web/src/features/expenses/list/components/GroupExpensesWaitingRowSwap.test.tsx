import { useQueryClient } from '@tanstack/react-query'
import { screen, waitFor } from '@testing-library/react'
import { useEffect, useMemo, type ReactNode } from 'react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { OutboxProvider } from '@/core/outbox/OutboxProvider'
import { createOutboxSender, type OutboxSender } from '@/core/outbox/outboxSender'
import type { ExpenseListRow } from '@/core/services/expenses/expensesService'
import { refreshAfterQueuedItem, sendQueuedItem } from '@/features/outbox/outboxDispatch'
import { expenseRows, foodCategory, hotelDetail, testCategories } from '@/test/expenseTestData'
import { categoriesPath, expensesPath } from '@/test/expenseTestHelpers'
import { testGroupId } from '@/test/groupTestData'
import { queuedExpense } from '@/test/outboxScreenTestData'
import { seedOutbox } from '@/test/outboxTestHelpers'
import { renderElementWithProviders } from '@/test/renderWithProviders'
import { jsonAnswer, stubFetchByRequest } from '@/test/requestTestHelpers'
import { seedMe, testMe } from '@/test/testMe'
import { GroupExpenses } from './GroupExpenses'

const slowOtherRefreshesMilliseconds = 25

const savedRow: ExpenseListRow = {
  id: 'f0000000-0000-4000-8000-000000000001',
  title: 'Museum tickets',
  amountMinor: 150000,
  currency: 'MKD',
  expenseDate: '2026-10-06',
  categoryId: foodCategory.id,
  paidByMemberId: 'member-1',
  paidByName: 'Ana',
  yourShareMinor: 75000,
  canEdit: true,
}

interface ActFlag {
  IS_REACT_ACT_ENVIRONMENT?: boolean
}

let actEnvironmentBefore: boolean | undefined

function WithSlowRefreshSender({ onSender, children }: { onSender: (sender: OutboxSender) => void; children: ReactNode }) {
  const queryClient = useQueryClient()
  const sender = useMemo(
    () =>
      createOutboxSender({
        personId: testMe.id,
        sendItem: sendQueuedItem,
        afterSent: async (item) => {
          await refreshAfterQueuedItem(queryClient, item)
          await new Promise((resolve) => setTimeout(resolve, slowOtherRefreshesMilliseconds))
        },
      }),
    [queryClient],
  )
  useEffect(() => {
    onSender(sender)
  }, [sender, onSender])
  return <OutboxProvider sender={sender}>{children}</OutboxProvider>
}

function waitingRowOnScreen(): boolean {
  return Array.from(document.querySelectorAll('[data-waiting-row]')).some((row) =>
    (row.textContent ?? '').includes('Museum tickets'),
  )
}

function realRowOnScreen(): boolean {
  return Array.from(document.querySelectorAll('a')).some((link) => (link.textContent ?? '').includes('Museum tickets'))
}

async function renderExpensesWithOneWaitingExpense() {
  let hasPosted = false
  stubFetchByRequest({
    [`GET ${expensesPath}`]: () => Response.json({ expenses: hasPosted ? [savedRow, ...expenseRows] : expenseRows }),
    [`GET ${categoriesPath}`]: jsonAnswer({ categories: testCategories }),
    [`POST ${expensesPath}`]: () => {
      hasPosted = true
      return Response.json(hotelDetail)
    },
  })
  seedOutbox(testMe.id, [queuedExpense('waiting')])
  let createdSender: OutboxSender | null = null
  await renderElementWithProviders(
    <WithSlowRefreshSender
      onSender={(sender) => {
        createdSender = sender
      }}
    >
      <GroupExpenses groupId={testGroupId} actionsWhenEmpty={null} />
    </WithSlowRefreshSender>,
    '/expenses',
    { seedCache: seedMe(testMe) },
  )
  await screen.findByRole('link', { name: /Dinner/ })
  await waitFor(() => {
    expect(waitingRowOnScreen()).toBe(true)
  })
  if (createdSender === null) {
    throw new Error('The test sender was not created')
  }
  return createdSender as OutboxSender
}

interface Look {
  waitingRow: boolean
  realRow: boolean
}

function watchTheRows() {
  const looks: Look[] = []
  const observer = new MutationObserver(() => {
    looks.push({ waitingRow: waitingRowOnScreen(), realRow: realRowOnScreen() })
  })
  observer.observe(document.body, { childList: true, subtree: true, characterData: true })
  return { looks, stop: () => observer.disconnect() }
}

beforeEach(() => {
  actEnvironmentBefore = (globalThis as ActFlag).IS_REACT_ACT_ENVIRONMENT
  ;(globalThis as ActFlag).IS_REACT_ACT_ENVIRONMENT = false
})

afterEach(() => {
  ;(globalThis as ActFlag).IS_REACT_ACT_ENVIRONMENT = actEnvironmentBefore
  vi.unstubAllGlobals()
  vi.restoreAllMocks()
})

describe('the group Expenses list while the sender sends a waiting expense', () => {
  async function sendAndWatch() {
    const sender = await renderExpensesWithOneWaitingExpense()
    const watch = watchTheRows()
    await sender.run()
    await waitFor(() => {
      expect(waitingRowOnScreen()).toBe(false)
      expect(realRowOnScreen()).toBe(true)
    })
    watch.stop()
    return watch.looks
  }

  it('ends with the real row and without the waiting row', async () => {
    const looks = await sendAndWatch()

    expect(looks.at(-1)).toEqual({ waitingRow: false, realRow: true })
  })

  it('never has the waiting row and the real row on the page at the same time', async () => {
    const looks = await sendAndWatch()

    expect(looks.filter((look) => look.waitingRow && look.realRow)).toEqual([])
  })

  it('never has a moment with neither the waiting row nor the real row', async () => {
    const looks = await sendAndWatch()

    expect(looks.filter((look) => !look.waitingRow && !look.realRow)).toEqual([])
  })
})
