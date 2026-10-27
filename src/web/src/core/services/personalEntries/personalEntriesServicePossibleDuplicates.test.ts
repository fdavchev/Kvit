import { afterEach, describe, expect, it, vi } from 'vitest'
import { stubFetch } from '@/test/apiTestHelpers'
import {
  dinnerOutEntry,
  dinnerPair,
  groceriesPair,
  mergedDinnerOutEntry,
  octoberWithTwoPairs,
  taxiPair,
} from '@/test/budgetDuplicateTestData'
import { dinnerShare } from '@/test/groupShareTestData'
import { personalFoodCategory } from '@/test/personalCategoryTestData'
import { coffeeEntry, emptyMonth, entryAnswerRow, monthAnswerBody, octoberMonth } from '@/test/personalEntryTestData'
import {
  addPersonalEntry,
  editPersonalEntry,
  getPersonalEntries,
  type NewPersonalEntry,
  type PersonalSpendingFields,
} from './personalEntriesService'

function withoutField(source: object, field: string): object {
  return Object.fromEntries(Object.entries(source).filter(([name]) => name !== field))
}

function bodyWith(fields: Record<string, unknown>): Record<string, unknown> {
  return { ...monthAnswerBody(emptyMonth('2026-10')), ...fields }
}

function bodyWithEntries(entries: unknown[]): Record<string, unknown> {
  return { ...monthAnswerBody(emptyMonth('2026-10')), entries }
}

const editedFields: PersonalSpendingFields = {
  kind: 'Spending',
  categoryId: personalFoodCategory.id,
  amountMinor: 100000,
  currency: 'MKD',
  entryDate: '2026-10-17',
  note: 'Dinner out',
}

const newEntry: NewPersonalEntry = { clientRequestId: '11111111-2222-4333-8444-555555555555', ...editedFields }

describe('getPersonalEntries, the possible duplicates', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('returns the pairs in the order of the answer, oldest first', async () => {
    stubFetch(Response.json(monthAnswerBody(octoberWithTwoPairs)))

    const month = await getPersonalEntries('2026-10')

    expect(month.possibleDuplicates).toEqual([dinnerPair, taxiPair])
  })

  it('returns an empty list when the month has no possible duplicates', async () => {
    stubFetch(Response.json(monthAnswerBody(octoberMonth)))

    expect((await getPersonalEntries('2026-10')).possibleDuplicates).toEqual([])
  })

  it('keeps a pair matched on the whole expense', async () => {
    stubFetch(Response.json(bodyWith({ possibleDuplicates: [groceriesPair] })))

    const [pair] = (await getPersonalEntries('2026-10')).possibleDuplicates

    expect(pair).toEqual(groceriesPair)
    expect(pair?.matchedOn).toBe('Whole')
  })

  it('keeps a pair with no note and no title as nulls', async () => {
    const pair = { ...dinnerPair, entryNote: null, expenseTitle: null }
    stubFetch(Response.json(bodyWith({ possibleDuplicates: [pair] })))

    expect((await getPersonalEntries('2026-10')).possibleDuplicates).toEqual([pair])
  })

  it('keeps a pair in euros with its amounts in cents', async () => {
    stubFetch(Response.json(bodyWith({ possibleDuplicates: [taxiPair] })))

    const [pair] = (await getPersonalEntries('2026-10')).possibleDuplicates

    expect(pair?.currency).toBe('EUR')
    expect(pair?.entryAmountMinor).toBe(850)
    expect(pair?.shareMinor).toBe(800)
  })

  it('throws an error naming "possibleDuplicates" when the answer lacks it', async () => {
    stubFetch(Response.json(withoutField(monthAnswerBody(octoberMonth), 'possibleDuplicates')))

    await expect(getPersonalEntries('2026-10')).rejects.toThrow('possibleDuplicates')
  })

  it.each([null, 42, 'none', {}])('throws an error naming "possibleDuplicates" when it is %j instead of a list', async (value) => {
    stubFetch(Response.json(bodyWith({ possibleDuplicates: value })))

    await expect(getPersonalEntries('2026-10')).rejects.toThrow('possibleDuplicates')
  })

  it.each(Object.keys(dinnerPair))('throws an error naming "%s" when a pair lacks it', async (field) => {
    stubFetch(Response.json(bodyWith({ possibleDuplicates: [withoutField(dinnerPair, field)] })))

    await expect(getPersonalEntries('2026-10')).rejects.toThrow(field)
  })

  it.each([
    ['entryId', 42],
    ['entryId', null],
    ['entryNote', 42],
    ['entryNote', true],
    ['entryAmountMinor', 12.5],
    ['entryAmountMinor', -1],
    ['entryAmountMinor', '100000'],
    ['entryAmountMinor', null],
    ['entryDate', 'yesterday'],
    ['entryDate', 42],
    ['entryDate', null],
    ['entryCategoryId', 42],
    ['entryCategoryId', null],
    ['expenseId', 42],
    ['expenseId', null],
    ['groupId', 42],
    ['groupId', null],
    ['groupName', 42],
    ['groupName', null],
    ['expenseTitle', 42],
    ['expenseTitle', {}],
    ['currency', 'USD'],
    ['currency', 'mkd'],
    ['currency', null],
    ['shareMinor', 0],
    ['shareMinor', -5],
    ['shareMinor', 12.5],
    ['shareMinor', '95000'],
    ['shareMinor', null],
    ['expenseAmountMinor', -1],
    ['expenseAmountMinor', 1.5],
    ['expenseAmountMinor', '190000'],
    ['expenseAmountMinor', null],
    ['expenseDate', 'tomorrow'],
    ['expenseDate', 18],
    ['expenseDate', null],
    ['matchedOn', 'Both'],
    ['matchedOn', 'share'],
    ['matchedOn', ''],
    ['matchedOn', 1],
    ['matchedOn', null],
  ])('throws an error naming "%s" when a pair has %j', async (field, value) => {
    stubFetch(Response.json(bodyWith({ possibleDuplicates: [{ ...dinnerPair, [field]: value }] })))

    await expect(getPersonalEntries('2026-10')).rejects.toThrow(field)
  })

  it.each(['Dinner', 42, null, []])('throws an error about a possible duplicate when a pair is %j instead of an object', async (value) => {
    stubFetch(Response.json(bodyWith({ possibleDuplicates: [value] })))

    await expect(getPersonalEntries('2026-10')).rejects.toThrow(/duplicate/i)
  })

  it('throws when only the second pair is wrong, because one bad pair makes the whole answer wrong', async () => {
    stubFetch(Response.json(bodyWith({ possibleDuplicates: [dinnerPair, withoutField(taxiPair, 'matchedOn')] })))

    await expect(getPersonalEntries('2026-10')).rejects.toThrow('matchedOn')
  })
})

describe('the entries of a month, "mergedWithExpenseId"', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('returns the id of the expense that an entry is merged with', async () => {
    stubFetch(Response.json(bodyWithEntries([entryAnswerRow(mergedDinnerOutEntry)])))

    const [entry] = (await getPersonalEntries('2026-10')).entries

    expect(entry?.mergedWithExpenseId).toBe(dinnerShare.expenseId)
  })

  it('returns null for an entry that is not merged', async () => {
    stubFetch(Response.json(bodyWithEntries([entryAnswerRow(coffeeEntry)])))

    const [entry] = (await getPersonalEntries('2026-10')).entries

    expect(entry?.mergedWithExpenseId).toBeNull()
  })

  it('throws an error naming "mergedWithExpenseId" when an entry lacks it, because the server sends null on purpose', async () => {
    stubFetch(Response.json(bodyWithEntries([withoutField(entryAnswerRow(coffeeEntry), 'mergedWithExpenseId')])))

    await expect(getPersonalEntries('2026-10')).rejects.toThrow('mergedWithExpenseId')
  })

  it.each([42, true, {}, []])('throws an error naming "mergedWithExpenseId" when an entry has %j', async (value) => {
    stubFetch(Response.json(bodyWithEntries([{ ...entryAnswerRow(coffeeEntry), mergedWithExpenseId: value }])))

    await expect(getPersonalEntries('2026-10')).rejects.toThrow('mergedWithExpenseId')
  })
})

describe('the answers of adding and editing an entry, "mergedWithExpenseId"', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('returns it as null from the answer of adding an entry', async () => {
    stubFetch(Response.json(entryAnswerRow(dinnerOutEntry)))

    expect((await addPersonalEntry(newEntry)).mergedWithExpenseId).toBeNull()
  })

  it('returns the expense id from the answer of editing a merged entry', async () => {
    stubFetch(Response.json(entryAnswerRow(mergedDinnerOutEntry)))

    const edited = await editPersonalEntry(mergedDinnerOutEntry.id, editedFields)

    expect(edited.mergedWithExpenseId).toBe(dinnerShare.expenseId)
  })

  it('throws an error naming "mergedWithExpenseId" when the answer of adding an entry lacks it', async () => {
    stubFetch(Response.json(withoutField(entryAnswerRow(dinnerOutEntry), 'mergedWithExpenseId')))

    await expect(addPersonalEntry(newEntry)).rejects.toThrow('mergedWithExpenseId')
  })

  it('throws an error naming "mergedWithExpenseId" when the answer of editing an entry lacks it', async () => {
    stubFetch(Response.json(withoutField(entryAnswerRow(mergedDinnerOutEntry), 'mergedWithExpenseId')))

    await expect(editPersonalEntry(mergedDinnerOutEntry.id, editedFields)).rejects.toThrow('mergedWithExpenseId')
  })

  it.each([42, true, {}])('throws an error naming "mergedWithExpenseId" when the answer of editing has %j', async (value) => {
    stubFetch(Response.json({ ...entryAnswerRow(mergedDinnerOutEntry), mergedWithExpenseId: value }))

    await expect(editPersonalEntry(mergedDinnerOutEntry.id, editedFields)).rejects.toThrow('mergedWithExpenseId')
  })
})
