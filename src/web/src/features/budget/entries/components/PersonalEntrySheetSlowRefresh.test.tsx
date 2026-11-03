import { useQuery } from '@tanstack/react-query'
import { fireEvent } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { outboxQueueAfterMilliseconds } from '@/core/outbox/saveOrQueue'
import { answerFirstRequestAtOnceAndHoldTheRest } from '@/test/addExpenseScreenTestHelpers'
import { saveButton, typeAmount } from '@/test/expenseFormTestHelpers'
import { postCount } from '@/test/offlineTestHelpers'
import { coffeeCategory } from '@/test/personalCategoryTestData'
import { coffeeEntry, entryAnswerRow } from '@/test/personalEntryTestData'
import { pickCategory, renderSheet, seededMonth } from '@/test/personalEntryTestHelpers'
import { holdOutboxSender, installFakeTimers, letTimeGoBy, readSavedOutbox } from '@/test/queuedSaveTestHelpers'
import { jsonAnswer, neverAnswer, type AnswerFactory } from '@/test/requestTestHelpers'
import { personalEntriesQueryKey } from '../personalEntryCache'
import { PersonalSpendingSheet } from './PersonalSpendingSheet'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const entriesUrl = '/api/personal/entries'
const entriesOfTheMonthUrl = `${entriesUrl}?month=${seededMonth}`
const slowRefreshMilliseconds = outboxQueueAfterMilliseconds + 1500
const sheetNow = new Date('2026-10-19T10:00:00Z')

function MonthOfEntriesOnScreen() {
  useQuery({
    queryKey: personalEntriesQueryKey(seededMonth),
    queryFn: async () => (await fetch(entriesOfTheMonthUrl)).json() as Promise<unknown>,
  })
  return null
}

async function openSheetWithAMonthOnScreen(answers: Record<string, AnswerFactory>) {
  const onClose = vi.fn<() => void>()
  const rendered = await renderSheet(
    <>
      <PersonalSpendingSheet onClose={onClose} />
      <MonthOfEntriesOnScreen />
    </>,
    onClose,
    { answers },
  )
  await pickCategory(coffeeCategory)
  const run = holdOutboxSender(rendered.outboxSender)
  return { ...rendered, run }
}

describe('PersonalSpendingSheet when the server answered the save but the refresh after it is slow', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  afterEach(() => {
    vi.useRealTimers()
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  async function saveWithSlowRefresh() {
    const month = answerFirstRequestAtOnceAndHoldTheRest([])
    const rendered = await openSheetWithAMonthOnScreen({
      [`POST ${entriesUrl}`]: jsonAnswer(entryAnswerRow(coffeeEntry)),
      [`GET ${entriesOfTheMonthUrl}`]: month.answer,
    })
    typeAmount('120')
    installFakeTimers(sheetNow)
    fireEvent.click(saveButton())
    return { ...rendered, month }
  }

  it('does not queue the entry although more than 3 seconds pass before the refresh is done', async () => {
    const { run } = await saveWithSlowRefresh()

    await letTimeGoBy(slowRefreshMilliseconds)

    expect(readSavedOutbox()).toEqual([])
    expect(run).not.toHaveBeenCalled()
  })

  it('keeps the sheet open until the refresh is done and then closes it once', async () => {
    const { onClose, month } = await saveWithSlowRefresh()
    await letTimeGoBy(slowRefreshMilliseconds)
    expect(onClose).not.toHaveBeenCalled()

    month.release([])
    await letTimeGoBy(0)

    expect(onClose).toHaveBeenCalledOnce()
  })

  it('queues nothing and sends nothing a second time, also after the refresh is done', async () => {
    const { fetchMock, month, run } = await saveWithSlowRefresh()
    await letTimeGoBy(slowRefreshMilliseconds)

    month.release([])
    await letTimeGoBy(60000)

    expect(readSavedOutbox()).toEqual([])
    expect(run).not.toHaveBeenCalled()
    expect(postCount(fetchMock, entriesUrl)).toBe(1)
  })
})

describe('PersonalSpendingSheet when the server does not answer the save', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  afterEach(() => {
    vi.useRealTimers()
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  it('still waits the full 3 seconds and then queues the entry and closes the sheet', async () => {
    const { onClose, run } = await openSheetWithAMonthOnScreen({
      [`POST ${entriesUrl}`]: neverAnswer(),
      [`GET ${entriesOfTheMonthUrl}`]: jsonAnswer([]),
    })
    typeAmount('120')
    installFakeTimers(sheetNow)

    fireEvent.click(saveButton())
    await letTimeGoBy(outboxQueueAfterMilliseconds - 1)

    expect(readSavedOutbox()).toEqual([])
    expect(onClose).not.toHaveBeenCalled()
    await letTimeGoBy(1)
    expect(readSavedOutbox()).toHaveLength(1)
    expect(onClose).toHaveBeenCalledOnce()
    expect(run).toHaveBeenCalledTimes(1)
  })
})
