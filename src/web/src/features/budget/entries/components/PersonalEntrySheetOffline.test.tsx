import { fireEvent, screen } from '@testing-library/react'
import { toast } from 'sonner'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { saveButton, typeAmount } from '@/test/expenseFormTestHelpers'
import { answerOnlyWhileOnline, goOffline, goOnline, postCount } from '@/test/offlineTestHelpers'
import { coffeeCategory } from '@/test/personalCategoryTestData'
import { coffeeEntry, entryAnswerRow } from '@/test/personalEntryTestData'
import { personalEntriesPost, pickCategory, renderSpendingSheet } from '@/test/personalEntryTestHelpers'
import { holdOutboxSender, installFakeTimers, letTimeGoBy, readSavedOutbox } from '@/test/queuedSaveTestHelpers'
import { jsonAnswer } from '@/test/requestTestHelpers'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const entriesUrl = '/api/personal/entries'
const sheetNow = new Date('2026-10-19T10:00:00Z')

describe('PersonalSpendingSheet saving while the browser is offline', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  afterEach(() => {
    goOnline()
    vi.useRealTimers()
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  async function openWhileOnline() {
    const rendered = await renderSpendingSheet({
      answers: { [personalEntriesPost]: answerOnlyWhileOnline(jsonAnswer(entryAnswerRow(coffeeEntry))) },
    })
    await pickCategory(coffeeCategory)
    const run = holdOutboxSender(rendered.outboxSender)
    return { ...rendered, run }
  }

  it('queues the entry at once, long before the 3 seconds are over, and closes the sheet', async () => {
    const { onClose, run } = await openWhileOnline()
    typeAmount('120')
    goOffline()
    installFakeTimers(sheetNow)

    fireEvent.click(saveButton())
    await letTimeGoBy(100)

    expect(readSavedOutbox()).toHaveLength(1)
    expect(onClose).toHaveBeenCalledOnce()
    expect(run).toHaveBeenCalledTimes(1)
  })

  it('shows no error text and no error toast while it queues the entry', async () => {
    const { onClose } = await openWhileOnline()
    typeAmount('120')
    goOffline()
    installFakeTimers(sheetNow)

    fireEvent.click(saveButton())
    await letTimeGoBy(100)

    expect(onClose).toHaveBeenCalledOnce()
    expect(vi.mocked(toast.error)).not.toHaveBeenCalled()
    expect(screen.queryByRole('alert')).toBeNull()
  })

  it('sends the one request of the sheet itself', async () => {
    const { fetchMock, onClose } = await openWhileOnline()
    typeAmount('120')
    goOffline()
    installFakeTimers(sheetNow)

    fireEvent.click(saveButton())
    await letTimeGoBy(100)

    expect(onClose).toHaveBeenCalledOnce()
    expect(postCount(fetchMock, entriesUrl)).toBe(1)
  })
})
