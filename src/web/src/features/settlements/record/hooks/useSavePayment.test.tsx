import { act, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { SettlementInput } from '@/core/services/settlements/settlementsService'
import { settlementsPath } from '@/test/balancesTestData'
import { anaMember, filipMember, markoMember } from '@/test/memberTestData'
import { holdOutboxSender } from '@/test/queuedSaveTestHelpers'
import { renderHookWithProviders } from '@/test/renderWithProviders'
import { jsonAnswer, problemAnswer, stubFetchByRequest, type AnswerFactory } from '@/test/requestTestHelpers'
import { pendingPaymentOf } from '@/test/settlementActionsTestData'
import { seedMe, testMe } from '@/test/testMe'
import { testGroupId } from '@/test/groupTestData'
import { useSavePayment } from './useSavePayment'

const members = [filipMember, anaMember, markoMember]
const names = { payerName: 'Ana', receiverName: 'Filip' }
const anaPaidMe: SettlementInput = {
  clientRequestId: 'payment-request-1',
  fromMemberId: anaMember.id,
  toMemberId: filipMember.id,
  amountMinor: 20000,
  currency: 'MKD',
}

async function renderSavePayment(postAnswer: AnswerFactory) {
  stubFetchByRequest({ [`POST ${settlementsPath}`]: postAnswer })
  const rendered = await renderHookWithProviders(() => useSavePayment(testGroupId, members), {
    seedCache: seedMe(testMe),
  })
  holdOutboxSender(rendered.outboxSender)
  return rendered
}

beforeEach(() => {
  vi.spyOn(console, 'error').mockImplementation(() => {})
})

afterEach(() => {
  vi.unstubAllGlobals()
  vi.restoreAllMocks()
})

describe('useSavePayment for a payment that counts right away', () => {
  it('calls onSaved once when the server saved the payment', async () => {
    const { result } = await renderSavePayment(jsonAnswer(pendingPaymentOf(anaMember, filipMember, 20000)))
    const onSaved = vi.fn()

    act(() => {
      result.current.save(anaPaidMe, names, onSaved)
    })

    await waitFor(() => {
      expect(onSaved).toHaveBeenCalledTimes(1)
    })
    expect(result.current.error).toBeNull()
  })

  it('keeps the error and never calls onSaved when the server refused the payment', async () => {
    const { result } = await renderSavePayment(problemAnswer(400, 'GROUP_FINISHED'))
    const onSaved = vi.fn()

    act(() => {
      result.current.save(anaPaidMe, names, onSaved)
    })

    await waitFor(() => {
      expect(result.current.error).not.toBeNull()
    })
    expect(onSaved).not.toHaveBeenCalled()
  })

  it('does not hide an error thrown by onSaved: it reaches console.error', async () => {
    const { result } = await renderSavePayment(jsonAnswer(pendingPaymentOf(anaMember, filipMember, 20000)))
    const thrown = new Error('closing the sheet failed')

    act(() => {
      result.current.save(anaPaidMe, names, () => {
        throw thrown
      })
    })

    await waitFor(() => {
      expect(vi.mocked(console.error).mock.calls.some((call) => call.includes(thrown))).toBe(true)
    })
  })
})
