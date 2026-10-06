import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '@/core/api/apiClient'
import {
  captureError,
  problemResponse,
  sentRequest,
  stubFetch,
} from '@/test/apiTestHelpers'
import {
  accommodationCategory,
  deletedRows,
  dinnerRow,
  exactSplitDetail,
  expenseRows,
  groceriesDetail,
  hotelDetail,
  hotelExpenseId,
  museumDeletedRow,
  taxiRow,
  untitledRow,
} from '@/test/expenseTestData'
import { testGroupId } from '@/test/groupTestData'
import { anaMember, filipMember, markoMember } from '@/test/memberTestData'
import {
  createExpense,
  deleteExpense,
  getDeletedExpenses,
  getExpense,
  getExpenses,
  restoreExpense,
  updateExpense,
  type ExpenseInput,
} from './expensesService'

const expensesPath = `/api/groups/${testGroupId}/expenses`
const expensePath = `${expensesPath}/${hotelExpenseId}`

const expenseChanges: Omit<ExpenseInput, 'clientRequestId'> = {
  title: 'Hotel',
  note: null,
  amountMinor: 300000,
  currency: 'MKD',
  expenseDate: '2026-10-03',
  categoryId: accommodationCategory.id,
  paidByMemberId: filipMember.id,
  splitType: 'Equal',
  shares: [
    { memberId: filipMember.id, inputValue: 0 },
    { memberId: anaMember.id, inputValue: 0 },
    { memberId: markoMember.id, inputValue: 60000 },
  ],
}

const expenseInput: ExpenseInput = {
  clientRequestId: '5b1f2c3d-4e5f-4a6b-8c7d-9e0f1a2b3c4d',
  ...expenseChanges,
}

const rowFields = Object.keys(dinnerRow)
const deletedRowFields = Object.keys(museumDeletedRow)
const detailFields = Object.keys(hotelDetail)
const shareFields = Object.keys(hotelDetail.shares[0])
const historyFields = Object.keys(hotelDetail.history[0])

function withoutField(source: object, field: string): object {
  return Object.fromEntries(Object.entries(source).filter(([name]) => name !== field))
}

function noContent(): Response {
  return new Response(null, { status: 204 })
}

describe('getExpenses', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('asks GET /api/groups/{id}/expenses and returns the rows in the order of the answer', async () => {
    const fetchMock = stubFetch(Response.json({ expenses: expenseRows }))

    const rows = await getExpenses(testGroupId)

    const request = sentRequest(fetchMock)
    expect(request.url).toBe(expensesPath)
    expect(request.method ?? 'GET').toBe('GET')
    expect(rows).toEqual(expenseRows)
  })

  it('returns an empty list when the group has no expenses', async () => {
    stubFetch(Response.json({ expenses: [] }))

    const rows = await getExpenses(testGroupId)

    expect(rows).toEqual([])
  })

  it('keeps null for a missing title and for a caller who is not in the split', async () => {
    stubFetch(Response.json({ expenses: [untitledRow] }))

    const rows = await getExpenses(testGroupId)

    expect(rows[0]).toMatchObject({ title: null, yourShareMinor: null })
  })

  it('keeps null for a missing category', async () => {
    stubFetch(Response.json({ expenses: [{ ...taxiRow, categoryId: null }] }))

    const rows = await getExpenses(testGroupId)

    expect(rows[0].categoryId).toBeNull()
  })

  it.each([
    ['a server error', new Response(null, { status: 500 }), 500, null],
    ['a group that is not found', problemResponse(404, 'GROUP_NOT_FOUND'), 404, 'GROUP_NOT_FOUND'],
    ['a rate limit', problemResponse(429, 'RATE_LIMITED'), 429, 'RATE_LIMITED'],
  ])('rethrows the ApiError unchanged for %s', async (_name, response, httpStatus, errorCode) => {
    stubFetch(response)

    const error = await captureError(getExpenses(testGroupId))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus, errorCode })
  })

  it('rethrows the ApiError unchanged when the network fails', async () => {
    stubFetch(new TypeError('Failed to fetch'))

    const error = await captureError(getExpenses(testGroupId))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus: null })
  })

  it('throws an error naming "expenses" when the answer lacks the list', async () => {
    stubFetch(Response.json({}))

    await expect(getExpenses(testGroupId)).rejects.toThrow('expenses')
  })

  it.each([
    ['a text', 'Dinner'],
    ['null', null],
    ['an object', {}],
  ])('throws an error naming "expenses" when the list is %s', async (_name, value) => {
    stubFetch(Response.json({ expenses: value }))

    await expect(getExpenses(testGroupId)).rejects.toThrow('expenses')
  })

  it.each(rowFields)('throws an error naming "%s" when a row lacks it', async (field) => {
    stubFetch(Response.json({ expenses: [withoutField(dinnerRow, field)] }))

    await expect(getExpenses(testGroupId)).rejects.toThrow(field)
  })

  it.each([
    ['id', 42],
    ['title', 5],
    ['amountMinor', '2400'],
    ['amountMinor', 12.5],
    ['amountMinor', null],
    ['currency', 'USD'],
    ['currency', 'mkd'],
    ['expenseDate', 20261006],
    ['expenseDate', null],
    ['categoryId', 7],
    ['paidByMemberId', null],
    ['paidByName', null],
    ['yourShareMinor', '600'],
    ['yourShareMinor', 6.5],
    ['canEdit', 'yes'],
    ['canEdit', 1],
  ])('throws an error naming "%s" when its value in a row is %j', async (field, value) => {
    stubFetch(Response.json({ expenses: [{ ...dinnerRow, [field]: value }] }))

    await expect(getExpenses(testGroupId)).rejects.toThrow(field)
  })

  it('checks every row, not only the first', async () => {
    stubFetch(Response.json({ expenses: [dinnerRow, { ...taxiRow, canEdit: 'yes' }] }))

    await expect(getExpenses(testGroupId)).rejects.toThrow('canEdit')
  })

  it.each([
    ['null', null],
    ['a text', 'Healthy'],
    ['a list', []],
  ])('throws an error saying an object was expected when the answer is %s', async (_name, body) => {
    stubFetch(Response.json(body))

    await expect(getExpenses(testGroupId)).rejects.toThrow(/object/)
  })
})

describe('getExpense', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('asks GET /api/groups/{id}/expenses/{expenseId} and returns the detail', async () => {
    const fetchMock = stubFetch(Response.json(hotelDetail))

    const detail = await getExpense(testGroupId, hotelExpenseId)

    const request = sentRequest(fetchMock)
    expect(request.url).toBe(expensePath)
    expect(request.method ?? 'GET').toBe('GET')
    expect(detail).toEqual(hotelDetail)
  })

  it('returns an EUR expense with its saved rate', async () => {
    stubFetch(Response.json(groceriesDetail))

    const detail = await getExpense(testGroupId, groceriesDetail.id)

    expect(detail).toMatchObject({ currency: 'EUR', mkdPerEur: 61.561, rateDate: '2026-09-24' })
  })

  it('returns the typed values of an Exact split', async () => {
    stubFetch(Response.json(exactSplitDetail))

    const detail = await getExpense(testGroupId, exactSplitDetail.id)

    expect(detail.splitType).toBe('Exact')
    expect(detail.shares.map((share) => share.inputValue)).toEqual([100000, 100000, 100000])
  })

  it.each([['Equal'], ['Exact'], ['Percentage'], ['Shares']])('accepts the split type %s', async (splitType) => {
    stubFetch(Response.json({ ...hotelDetail, splitType }))

    const detail = await getExpense(testGroupId, hotelExpenseId)

    expect(detail.splitType).toBe(splitType)
  })

  it.each([
    ['note', null],
    ['title', null],
    ['categoryId', null],
    ['updatedAt', null],
  ])('keeps null for %s', async (field, value) => {
    stubFetch(Response.json({ ...hotelDetail, [field]: value }))

    const detail = await getExpense(testGroupId, hotelExpenseId)

    expect(detail).toMatchObject({ [field]: value })
  })

  it('keeps null for the changes of a history entry that is not an edit', async () => {
    stubFetch(Response.json(hotelDetail))

    const detail = await getExpense(testGroupId, hotelExpenseId)

    expect(detail.history[1]).toMatchObject({ type: 'ExpenseAdded', changes: null })
  })

  it('returns the changes of an edit as a list of field, old and new', async () => {
    stubFetch(Response.json(hotelDetail))

    const detail = await getExpense(testGroupId, hotelExpenseId)

    expect(detail.history[0].changes).toEqual([{ field: 'amount', old: '280000', new: '300000' }])
  })

  it('returns an edit with several changes and empty texts for none', async () => {
    const changes = [
      { field: 'title', old: '', new: 'Hotel' },
      { field: 'split', old: 'Equal: Ana 600, Filip 600', new: 'Exact: Ana 400, Filip 800' },
    ]
    stubFetch(
      Response.json({
        ...hotelDetail,
        history: [{ ...hotelDetail.history[0], changes }],
      }),
    )

    const detail = await getExpense(testGroupId, hotelExpenseId)

    expect(detail.history[0].changes).toEqual(changes)
  })

  it.each(['ExpenseAdded', 'ExpenseEdited', 'ExpenseDeleted', 'ExpenseRestored'])('accepts the history entry type %s', async (type) => {
    stubFetch(
      Response.json({
        ...hotelDetail,
        history: [{ ...hotelDetail.history[1], type }],
      }),
    )

    const detail = await getExpense(testGroupId, hotelExpenseId)

    expect(detail.history[0].type).toBe(type)
  })

  it('returns an empty history and an empty split as empty lists', async () => {
    stubFetch(Response.json({ ...hotelDetail, history: [], shares: [] }))

    const detail = await getExpense(testGroupId, hotelExpenseId)

    expect(detail).toMatchObject({ history: [], shares: [] })
  })

  it.each([
    ['a missing expense', problemResponse(404, 'EXPENSE_NOT_FOUND'), 404, 'EXPENSE_NOT_FOUND'],
    ['a group that is not found', problemResponse(404, 'GROUP_NOT_FOUND'), 404, 'GROUP_NOT_FOUND'],
    ['an id that is not a guid', new Response(null, { status: 404 }), 404, null],
    ['a server error', new Response(null, { status: 500 }), 500, null],
  ])('rethrows the ApiError unchanged for %s', async (_name, response, httpStatus, errorCode) => {
    stubFetch(response)

    const error = await captureError(getExpense(testGroupId, hotelExpenseId))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus, errorCode })
  })

  it.each(detailFields)('throws an error naming "%s" when the answer lacks it', async (field) => {
    stubFetch(Response.json(withoutField(hotelDetail, field)))

    await expect(getExpense(testGroupId, hotelExpenseId)).rejects.toThrow(field)
  })

  it.each([
    ['id', 42],
    ['groupId', null],
    ['title', 5],
    ['note', 5],
    ['amountMinor', '300000'],
    ['amountMinor', 1.5],
    ['currency', 'USD'],
    ['expenseDate', 7],
    ['categoryId', 7],
    ['paidByMemberId', null],
    ['paidByName', null],
    ['splitType', 'Weird'],
    ['splitType', 'equal'],
    ['splitType', null],
    ['mkdPerEur', '61.5610'],
    ['mkdPerEur', null],
    ['rateDate', 5],
    ['createdByUserId', 5],
    ['createdByName', null],
    ['createdAt', 'yesterday'],
    ['createdAt', null],
    ['updatedAt', 5],
    ['canEdit', 'true'],
    ['canEdit', null],
    ['shares', 'Filip'],
    ['shares', null],
    ['history', 'none'],
    ['history', null],
  ])('throws an error naming "%s" when its value is %j', async (field, value) => {
    stubFetch(Response.json({ ...hotelDetail, [field]: value }))

    await expect(getExpense(testGroupId, hotelExpenseId)).rejects.toThrow(field)
  })

  it.each(shareFields)('throws an error naming "%s" when a person in the split lacks it', async (field) => {
    stubFetch(
      Response.json({ ...hotelDetail, shares: [withoutField(hotelDetail.shares[0], field)] }),
    )

    await expect(getExpense(testGroupId, hotelExpenseId)).rejects.toThrow(field)
  })

  it.each([
    ['memberId', 5],
    ['name', null],
    ['inputValue', '600'],
    ['shareMinor', '48000'],
    ['shareMinor', 1.5],
  ])('throws an error naming "%s" when its value in the split is %j', async (field, value) => {
    stubFetch(
      Response.json({ ...hotelDetail, shares: [{ ...hotelDetail.shares[0], [field]: value }] }),
    )

    await expect(getExpense(testGroupId, hotelExpenseId)).rejects.toThrow(field)
  })

  it.each(historyFields)('throws an error naming "%s" when a history entry lacks it', async (field) => {
    stubFetch(
      Response.json({ ...hotelDetail, history: [withoutField(hotelDetail.history[0], field)] }),
    )

    await expect(getExpense(testGroupId, hotelExpenseId)).rejects.toThrow(field)
  })

  it.each([
    ['type', 'ExpenseMoved'],
    ['type', null],
    ['actorName', null],
    ['actorName', 5],
    ['createdAt', 'a while ago'],
    ['changes', 'amount'],
    ['changes', {}],
  ])('throws an error naming "%s" when its value in a history entry is %j', async (field, value) => {
    stubFetch(
      Response.json({ ...hotelDetail, history: [{ ...hotelDetail.history[0], [field]: value }] }),
    )

    await expect(getExpense(testGroupId, hotelExpenseId)).rejects.toThrow(field)
  })

  it.each(['field', 'old', 'new'])('throws an error naming "%s" when a change lacks it', async (field) => {
    const change = { field: 'amount', old: '280000', new: '300000' }
    stubFetch(
      Response.json({
        ...hotelDetail,
        history: [{ ...hotelDetail.history[0], changes: [withoutField(change, field)] }],
      }),
    )

    await expect(getExpense(testGroupId, hotelExpenseId)).rejects.toThrow(field)
  })

  it.each([
    ['field', 5],
    ['old', 280000],
    ['new', null],
  ])('throws an error naming "%s" when its value in a change is %j', async (field, value) => {
    const change = { field: 'amount', old: '280000', new: '300000', [field]: value }
    stubFetch(
      Response.json({
        ...hotelDetail,
        history: [{ ...hotelDetail.history[0], changes: [change] }],
      }),
    )

    await expect(getExpense(testGroupId, hotelExpenseId)).rejects.toThrow(field)
  })

  it.each([
    ['null', null],
    ['a text', 'Hotel'],
    ['a list', []],
  ])('throws an error saying an object was expected when the answer is %s', async (_name, body) => {
    stubFetch(Response.json(body))

    await expect(getExpense(testGroupId, hotelExpenseId)).rejects.toThrow(/object/)
  })
})

describe('createExpense', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('sends POST /api/groups/{id}/expenses with the whole input as JSON, including the clientRequestId, and returns the detail', async () => {
    const fetchMock = stubFetch(Response.json(hotelDetail))

    const detail = await createExpense(testGroupId, expenseInput)

    expect(detail).toEqual(hotelDetail)
    expect(sentRequest(fetchMock)).toEqual({
      url: expensesPath,
      method: 'POST',
      contentType: 'application/json',
      body: expenseInput,
    })
  })

  it('sends null for an empty title, note and category', async () => {
    const fetchMock = stubFetch(Response.json(hotelDetail))
    const bare: ExpenseInput = { ...expenseInput, title: null, note: null, categoryId: null }

    await createExpense(testGroupId, bare)

    expect(sentRequest(fetchMock).body).toMatchObject({ title: null, note: null, categoryId: null })
  })

  it('sends the typed values of the split as inputValue for every person', async () => {
    const fetchMock = stubFetch(Response.json(hotelDetail))

    await createExpense(testGroupId, expenseInput)

    expect(sentRequest(fetchMock).body).toMatchObject({
      splitType: 'Equal',
      shares: [
        { memberId: filipMember.id, inputValue: 0 },
        { memberId: anaMember.id, inputValue: 0 },
        { memberId: markoMember.id, inputValue: 60000 },
      ],
    })
  })

  it.each([
    [400, 'EXPENSE_AMOUNT_NOT_POSITIVE'],
    [400, 'EXPENSE_SPLIT_DOES_NOT_ADD_UP'],
    [400, 'EXPENSE_CLIENT_REQUEST_ID_USED'],
    [404, 'MEMBER_NOT_FOUND'],
    [404, 'GROUP_NOT_FOUND'],
  ])('rethrows the ApiError unchanged for %i %s', async (httpStatus, errorCode) => {
    stubFetch(problemResponse(httpStatus, errorCode))

    const error = await captureError(createExpense(testGroupId, expenseInput))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus, errorCode })
  })

  it('rethrows the ApiError unchanged when the network fails', async () => {
    stubFetch(new TypeError('Failed to fetch'))

    const error = await captureError(createExpense(testGroupId, expenseInput))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus: null })
  })

  it('throws an error naming "id" when the answer lacks the id of the new expense', async () => {
    stubFetch(Response.json(withoutField(hotelDetail, 'id')))

    await expect(createExpense(testGroupId, expenseInput)).rejects.toThrow('id')
  })
})

describe('updateExpense', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('sends PUT /api/groups/{id}/expenses/{expenseId} with the changes as JSON and no clientRequestId, and resolves on 204', async () => {
    const fetchMock = stubFetch(noContent())

    const result = await updateExpense(testGroupId, hotelExpenseId, expenseChanges)

    expect(result).toBeUndefined()
    expect(sentRequest(fetchMock)).toEqual({
      url: expensePath,
      method: 'PUT',
      contentType: 'application/json',
      body: expenseChanges,
    })
    expect(sentRequest(fetchMock).body).not.toHaveProperty('clientRequestId')
  })

  it.each([
    [403, 'EXPENSE_NOT_ALLOWED'],
    [404, 'EXPENSE_NOT_FOUND'],
    [400, 'EXPENSE_SPLIT_EXTRAS_EXCEED_TOTAL'],
  ])('rethrows the ApiError unchanged for %i %s', async (httpStatus, errorCode) => {
    stubFetch(problemResponse(httpStatus, errorCode))

    const error = await captureError(updateExpense(testGroupId, hotelExpenseId, expenseChanges))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus, errorCode })
  })
})

describe('deleteExpense', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('sends DELETE /api/groups/{id}/expenses/{expenseId} with no body and resolves on 204', async () => {
    const fetchMock = stubFetch(noContent())

    const result = await deleteExpense(testGroupId, hotelExpenseId)

    expect(result).toBeUndefined()
    expect(sentRequest(fetchMock)).toEqual({
      url: expensePath,
      method: 'DELETE',
      contentType: null,
      body: undefined,
    })
  })

  it.each([
    [403, 'EXPENSE_NOT_ALLOWED'],
    [404, 'EXPENSE_NOT_FOUND'],
  ])('rethrows the ApiError unchanged for %i %s', async (httpStatus, errorCode) => {
    stubFetch(problemResponse(httpStatus, errorCode))

    const error = await captureError(deleteExpense(testGroupId, hotelExpenseId))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus, errorCode })
  })
})

describe('restoreExpense', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('sends POST /api/groups/{id}/expenses/{expenseId}/restore with no body and resolves on 204', async () => {
    const fetchMock = stubFetch(noContent())

    const result = await restoreExpense(testGroupId, hotelExpenseId)

    expect(result).toBeUndefined()
    expect(sentRequest(fetchMock)).toEqual({
      url: `${expensePath}/restore`,
      method: 'POST',
      contentType: null,
      body: undefined,
    })
  })

  it.each([
    [400, 'EXPENSE_RESTORE_EXPIRED'],
    [400, 'EXPENSE_NOT_DELETED'],
    [403, 'EXPENSE_NOT_ALLOWED'],
    [404, 'EXPENSE_NOT_FOUND'],
  ])('rethrows the ApiError unchanged for %i %s', async (httpStatus, errorCode) => {
    stubFetch(problemResponse(httpStatus, errorCode))

    const error = await captureError(restoreExpense(testGroupId, hotelExpenseId))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus, errorCode })
  })
})

describe('getDeletedExpenses', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('asks GET /api/groups/{id}/expenses/deleted and returns the rows in the order of the answer', async () => {
    const fetchMock = stubFetch(Response.json({ expenses: deletedRows }))

    const rows = await getDeletedExpenses(testGroupId)

    const request = sentRequest(fetchMock)
    expect(request.url).toBe(`${expensesPath}/deleted`)
    expect(request.method ?? 'GET').toBe('GET')
    expect(rows).toEqual(deletedRows)
  })

  it('returns an empty list when nothing was deleted lately', async () => {
    stubFetch(Response.json({ expenses: [] }))

    const rows = await getDeletedExpenses(testGroupId)

    expect(rows).toEqual([])
  })

  it.each([
    ['title', null],
    ['categoryId', null],
  ])('keeps null for %s', async (field, value) => {
    stubFetch(Response.json({ expenses: [{ ...museumDeletedRow, [field]: value }] }))

    const rows = await getDeletedExpenses(testGroupId)

    expect(rows[0]).toMatchObject({ [field]: value })
  })

  it.each([
    ['a server error', new Response(null, { status: 500 }), 500, null],
    ['a group that is not found', problemResponse(404, 'GROUP_NOT_FOUND'), 404, 'GROUP_NOT_FOUND'],
  ])('rethrows the ApiError unchanged for %s', async (_name, response, httpStatus, errorCode) => {
    stubFetch(response)

    const error = await captureError(getDeletedExpenses(testGroupId))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus, errorCode })
  })

  it.each(deletedRowFields)('throws an error naming "%s" when a row lacks it', async (field) => {
    stubFetch(Response.json({ expenses: [withoutField(museumDeletedRow, field)] }))

    await expect(getDeletedExpenses(testGroupId)).rejects.toThrow(field)
  })

  it.each([
    ['id', 42],
    ['amountMinor', '800'],
    ['currency', 'USD'],
    ['expenseDate', 5],
    ['paidByName', null],
    ['deletedAt', 5],
    ['deletedAt', 'last week'],
    ['restorableUntil', null],
    ['restorableUntil', 'next month'],
    ['canEdit', 'yes'],
  ])('throws an error naming "%s" when its value in a row is %j', async (field, value) => {
    stubFetch(Response.json({ expenses: [{ ...museumDeletedRow, [field]: value }] }))

    await expect(getDeletedExpenses(testGroupId)).rejects.toThrow(field)
  })

  it('throws an error naming "expenses" when the answer lacks the list', async () => {
    stubFetch(Response.json({}))

    await expect(getDeletedExpenses(testGroupId)).rejects.toThrow('expenses')
  })

  it.each([
    ['null', null],
    ['a text', 'Healthy'],
  ])('throws an error saying an object was expected when the answer is %s', async (_name, body) => {
    stubFetch(Response.json(body))

    await expect(getDeletedExpenses(testGroupId)).rejects.toThrow(/object/)
  })
})
