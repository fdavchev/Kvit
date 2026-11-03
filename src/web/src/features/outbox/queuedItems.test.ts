import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { ExpenseInput, OneBillInput } from '@/core/services/expenses/expensesService'
import type { NewPersonalEntry } from '@/core/services/personalEntries/personalEntriesService'
import type { SettlementInput } from '@/core/services/settlements/settlementsService'
import { filipMember, markoMember } from '@/test/memberTestData'
import {
  buildExpenseOutboxItem,
  buildOneBillOutboxItem,
  buildPersonalEntryOutboxItem,
  buildSettlementOutboxItem,
} from './queuedItems'

const now = new Date('2026-11-01T09:30:00.000Z')

const expenseInput: ExpenseInput = {
  clientRequestId: 'request-expense',
  title: 'Hotel',
  note: null,
  amountMinor: 300000,
  currency: 'MKD',
  expenseDate: '2026-10-19',
  categoryId: 'category-hotel',
  paidByMemberId: filipMember.id,
  splitType: 'Equal',
  shares: [
    { memberId: filipMember.id, inputValue: 1 },
    { memberId: markoMember.id, inputValue: 1 },
  ],
}

const oneBillInput: OneBillInput = {
  clientRequestId: 'request-one-bill',
  title: 'Dinner',
  groupName: null,
  names: ['Marko'],
  note: null,
  amountMinor: 180000,
  currency: 'MKD',
  expenseDate: '2026-10-20',
  categoryId: 'category-food',
  paidByPersonIndex: 0,
  splitType: 'Equal',
  shares: [
    { personIndex: 0, inputValue: 1 },
    { personIndex: 1, inputValue: 1 },
  ],
}

const spendingInput: NewPersonalEntry = {
  clientRequestId: 'request-spending',
  kind: 'Spending',
  categoryId: 'personal-category-coffee',
  amountMinor: 12000,
  currency: 'MKD',
  entryDate: '2026-10-21',
  note: 'Latte',
}

const incomeInput: NewPersonalEntry = {
  clientRequestId: 'request-income',
  kind: 'Income',
  amountMinor: 6500000,
  currency: 'MKD',
  entryDate: '2026-10-22',
  note: 'Salary',
}

const settlementInput: SettlementInput = {
  clientRequestId: 'request-settlement',
  fromMemberId: markoMember.id,
  toMemberId: filipMember.id,
  amountMinor: 80000,
  currency: 'EUR',
}

beforeEach(() => {
  vi.useFakeTimers({ toFake: ['Date'] })
  vi.setSystemTime(now)
})

afterEach(() => {
  vi.useRealTimers()
})

describe('buildExpenseOutboxItem', () => {
  it('builds a waiting expense item of the group with the request id as its id', () => {
    const item = buildExpenseOutboxItem({ groupId: 'group-1', input: expenseInput, paidByName: 'Filip' })

    expect(item).toMatchObject({
      id: 'request-expense',
      kind: 'expense',
      groupId: 'group-1',
      state: 'waiting',
      failure: null,
      createdAt: now.toISOString(),
    })
  })

  it('keeps the exact service input as the payload', () => {
    const item = buildExpenseOutboxItem({ groupId: 'group-1', input: expenseInput, paidByName: 'Filip' })

    expect(item.payload).toEqual(expenseInput)
  })

  it('shows the title, amount, currency, date, category and payer on the waiting row', () => {
    const item = buildExpenseOutboxItem({ groupId: 'group-1', input: expenseInput, paidByName: 'Filip' })

    expect(item.display).toEqual({
      title: 'Hotel',
      amountMinor: 300000,
      currency: 'MKD',
      date: '2026-10-19',
      categoryId: 'category-hotel',
      paidByName: 'Filip',
    })
  })

  it('shows no title when the expense has none', () => {
    const item = buildExpenseOutboxItem({
      groupId: 'group-1',
      input: { ...expenseInput, title: null },
      paidByName: 'Filip',
    })

    expect(item.display.title).toBeUndefined()
  })

  it('keeps a missing category as null', () => {
    const item = buildExpenseOutboxItem({
      groupId: 'group-1',
      input: { ...expenseInput, categoryId: null },
      paidByName: 'Filip',
    })

    expect(item.display.categoryId).toBeNull()
  })

  it('takes the time of the moment it was built', () => {
    vi.setSystemTime(new Date('2026-11-02T10:00:00.000Z'))

    const item = buildExpenseOutboxItem({ groupId: 'group-1', input: expenseInput, paidByName: 'Filip' })

    expect(item.createdAt).toBe('2026-11-02T10:00:00.000Z')
  })

  it('gives another item another id when the request id differs', () => {
    const first = buildExpenseOutboxItem({ groupId: 'group-1', input: expenseInput, paidByName: 'Filip' })
    const second = buildExpenseOutboxItem({
      groupId: 'group-1',
      input: { ...expenseInput, clientRequestId: 'request-expense-2' },
      paidByName: 'Filip',
    })

    expect(second.id).not.toBe(first.id)
  })
})

describe('buildOneBillOutboxItem', () => {
  it('builds a waiting One bill item with no group and the request id as its id', () => {
    const item = buildOneBillOutboxItem({ input: oneBillInput })

    expect(item).toMatchObject({
      id: 'request-one-bill',
      kind: 'oneBill',
      groupId: null,
      state: 'waiting',
      failure: null,
      createdAt: now.toISOString(),
    })
  })

  it('keeps the exact service input as the payload', () => {
    const item = buildOneBillOutboxItem({ input: oneBillInput })

    expect(item.payload).toEqual(oneBillInput)
  })

  it('uses the typed title as the title and as the group name when there is one', () => {
    const item = buildOneBillOutboxItem({ input: oneBillInput })

    expect(item.display).toEqual({
      title: 'Dinner',
      groupName: 'Dinner',
      amountMinor: 180000,
      currency: 'MKD',
      date: '2026-10-20',
      categoryId: 'category-food',
    })
  })

  it('uses the group name as the title when no title was typed', () => {
    const item = buildOneBillOutboxItem({
      input: { ...oneBillInput, title: null, groupName: 'Bill of Oct 20' },
    })

    expect(item.display.title).toBe('Bill of Oct 20')
    expect(item.display.groupName).toBe('Bill of Oct 20')
  })

  it('keeps a missing category as null', () => {
    const item = buildOneBillOutboxItem({ input: { ...oneBillInput, categoryId: null } })

    expect(item.display.categoryId).toBeNull()
  })
})

describe('buildPersonalEntryOutboxItem', () => {
  it('builds a waiting personal item with no group and the request id as its id', () => {
    const item = buildPersonalEntryOutboxItem({ input: spendingInput })

    expect(item).toMatchObject({
      id: 'request-spending',
      kind: 'personalEntry',
      groupId: null,
      state: 'waiting',
      failure: null,
      createdAt: now.toISOString(),
    })
  })

  it('keeps the exact service input as the payload', () => {
    expect(buildPersonalEntryOutboxItem({ input: spendingInput }).payload).toEqual(spendingInput)
    expect(buildPersonalEntryOutboxItem({ input: incomeInput }).payload).toEqual(incomeInput)
  })

  it('shows a spending with its note as the title, its category and the kind Spending', () => {
    const item = buildPersonalEntryOutboxItem({ input: spendingInput })

    expect(item.display).toEqual({
      title: 'Latte',
      amountMinor: 12000,
      currency: 'MKD',
      date: '2026-10-21',
      categoryId: 'personal-category-coffee',
      entryKind: 'Spending',
    })
  })

  it('shows an income with no category and the kind Income', () => {
    const item = buildPersonalEntryOutboxItem({ input: incomeInput })

    expect(item.display).toEqual({
      title: 'Salary',
      amountMinor: 6500000,
      currency: 'MKD',
      date: '2026-10-22',
      categoryId: null,
      entryKind: 'Income',
    })
  })

  it('shows no title when the entry has no note', () => {
    const item = buildPersonalEntryOutboxItem({ input: { ...spendingInput, note: null } })

    expect(item.display.title).toBeUndefined()
  })
})

describe('buildSettlementOutboxItem', () => {
  it('builds a waiting payment item of the group with the request id as its id', () => {
    const item = buildSettlementOutboxItem({
      groupId: 'group-1',
      input: settlementInput,
      payerName: 'Marko',
      receiverName: 'Filip',
    })

    expect(item).toMatchObject({
      id: 'request-settlement',
      kind: 'settlement',
      groupId: 'group-1',
      state: 'waiting',
      failure: null,
      createdAt: now.toISOString(),
    })
  })

  it('keeps the exact service input as the payload', () => {
    const item = buildSettlementOutboxItem({
      groupId: 'group-1',
      input: settlementInput,
      payerName: 'Marko',
      receiverName: 'Filip',
    })

    expect(item.payload).toEqual(settlementInput)
  })

  it('shows the amount, the currency and the names of the payer and the receiver', () => {
    const item = buildSettlementOutboxItem({
      groupId: 'group-1',
      input: settlementInput,
      payerName: 'Marko',
      receiverName: 'Filip',
    })

    expect(item.display).toEqual({
      amountMinor: 80000,
      currency: 'EUR',
      payerName: 'Marko',
      receiverName: 'Filip',
    })
  })
})
