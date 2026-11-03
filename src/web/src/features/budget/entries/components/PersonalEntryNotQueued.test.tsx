import { fireEvent, screen, waitFor } from '@testing-library/react'
import { toast } from 'sonner'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { saveButton, typeAmount } from '@/test/expenseFormTestHelpers'
import { coffeeCategory } from '@/test/personalCategoryTestData'
import { coffeeEntry, salaryIncomeEntry } from '@/test/personalEntryTestData'
import {
  deleteButton,
  findDetailsSheet,
  personalEntryDelete,
  personalEntryPut,
  personalEntryRestorePost,
  renderSpendingSheet,
  waitForEveryDialogToClose,
} from '@/test/personalEntryTestHelpers'
import { findIncomeEditSheet, renderIncomeSheet } from '@/test/personalIncomeTestHelpers'
import { holdOutboxSender, readSavedOutbox } from '@/test/queuedSaveTestHelpers'
import { networkFailureAnswer, noContentAnswer } from '@/test/requestTestHelpers'
import { pressToastAction, toastShownWithText } from '@/test/toastTestHelpers'
import { translated } from '@/test/translated'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

describe('the paths of the personal entry sheets that are not queued', () => {
  beforeEach(() => {
    window.localStorage.clear()
    vi.useFakeTimers({ toFake: ['Date'] })
    vi.setSystemTime(new Date('2026-10-19T10:00:00Z'))
  })

  afterEach(() => {
    vi.useRealTimers()
    vi.unstubAllGlobals()
    vi.clearAllMocks()
    vi.restoreAllMocks()
  })

  it('shows the network message and keeps the sheet open when editing a spending cannot reach the server', async () => {
    const { onClose, outboxSender } = await renderSpendingSheet({
      entry: coffeeEntry,
      answers: { [personalEntryPut(coffeeEntry.id)]: networkFailureAnswer() },
    })
    const run = holdOutboxSender(outboxSender)
    await findDetailsSheet(coffeeCategory)
    typeAmount('130')

    fireEvent.click(saveButton())

    expect((await screen.findByRole('alert')).textContent).toBe(translated('en', 'errors.network'))
    expect(onClose).not.toHaveBeenCalled()
    expect(readSavedOutbox()).toEqual([])
    expect(run).not.toHaveBeenCalled()
  })

  it('shows the network message and keeps the sheet open when editing an income cannot reach the server', async () => {
    const { onClose, outboxSender } = await renderIncomeSheet({
      entry: salaryIncomeEntry,
      answers: { [personalEntryPut(salaryIncomeEntry.id)]: networkFailureAnswer() },
    })
    const run = holdOutboxSender(outboxSender)
    await findIncomeEditSheet()
    typeAmount('70000')

    fireEvent.click(saveButton())

    expect((await screen.findByRole('alert')).textContent).toBe(translated('en', 'errors.network'))
    expect(onClose).not.toHaveBeenCalled()
    expect(readSavedOutbox()).toEqual([])
    expect(run).not.toHaveBeenCalled()
  })

  it('shows the network message as a toast and keeps the sheet open when a delete cannot reach the server', async () => {
    const { onClose, outboxSender } = await renderSpendingSheet({
      entry: coffeeEntry,
      answers: { [personalEntryDelete(coffeeEntry.id)]: networkFailureAnswer() },
    })
    const run = holdOutboxSender(outboxSender)
    const sheet = await findDetailsSheet(coffeeCategory)

    fireEvent.click(deleteButton(sheet))

    await waitFor(() => {
      expect(vi.mocked(toast.error)).toHaveBeenCalledWith(translated('en', 'errors.network'))
    })
    expect(onClose).not.toHaveBeenCalled()
    expect(readSavedOutbox()).toEqual([])
    expect(run).not.toHaveBeenCalled()
  })

  it('shows the network message as a toast when Undo cannot reach the server', async () => {
    const { outboxSender } = await renderSpendingSheet({
      entry: coffeeEntry,
      answers: {
        [personalEntryDelete(coffeeEntry.id)]: noContentAnswer(),
        [personalEntryRestorePost(coffeeEntry.id)]: networkFailureAnswer(),
      },
    })
    const run = holdOutboxSender(outboxSender)
    fireEvent.click(deleteButton(await findDetailsSheet(coffeeCategory)))
    await waitForEveryDialogToClose()

    pressToastAction(toastShownWithText(translated('en', 'budget.entries.deleted')))

    await waitFor(() => {
      expect(vi.mocked(toast.error)).toHaveBeenCalledWith(translated('en', 'errors.network'))
    })
    expect(readSavedOutbox()).toEqual([])
    expect(run).not.toHaveBeenCalled()
  })
})
