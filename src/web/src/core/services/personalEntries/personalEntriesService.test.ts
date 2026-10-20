import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '@/core/api/apiClient'
import { captureError, problemResponse, sentRequest, stubFetch } from '@/test/apiTestHelpers'
import { archivedGymCategory, coffeeCategory } from '@/test/personalCategoryTestData'
import {
  coffeeEntry,
  entryAnswerRow,
  gymEntryWithArchivedCategory,
  lunchInEuroEntry,
  monthAnswerBody,
  octoberMonth,
} from '@/test/personalEntryTestData'
import {
  addPersonalEntry,
  deletePersonalEntry,
  editPersonalEntry,
  getPersonalEntries,
  restorePersonalEntry,
  type NewPersonalEntry,
  type PersonalEntryFields,
} from './personalEntriesService'

const typedFields: PersonalEntryFields = {
  categoryId: coffeeCategory.id,
  amountMinor: 12000,
  currency: 'MKD',
  entryDate: '2026-10-19',
  note: null,
}

const newEntry: NewPersonalEntry = {
  clientRequestId: '11111111-2222-4333-8444-555555555555',
  ...typedFields,
}

function withoutField(source: object, field: string): object {
  return Object.fromEntries(Object.entries(source).filter(([name]) => name !== field))
}

function noContent(): Response {
  return new Response(null, { status: 204 })
}

describe('getPersonalEntries', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('asks GET /api/personal/entries?month=2026-10 for the month', async () => {
    const fetchMock = stubFetch(Response.json(monthAnswerBody(octoberMonth)))

    await getPersonalEntries('2026-10')

    const request = sentRequest(fetchMock)
    expect(request.url).toBe('/api/personal/entries?month=2026-10')
    expect(request.method ?? 'GET').toBe('GET')
  })

  it('returns the month, the entries in the order of the answer and the totals, MKD and EUR apart', async () => {
    stubFetch(Response.json(monthAnswerBody(octoberMonth)))

    expect(await getPersonalEntries('2026-10')).toEqual(octoberMonth)
  })

  it('keeps an entry whose category is archived, with the archived category row', async () => {
    stubFetch(Response.json({ month: '2026-09', entries: [entryAnswerRow(gymEntryWithArchivedCategory)], totals: [], incomeTotals: [] }))

    const month = await getPersonalEntries('2026-09')

    expect(month.entries[0].category).toEqual(archivedGymCategory)
  })

  it('returns an empty month when the server has nothing', async () => {
    stubFetch(Response.json({ month: '2026-08', entries: [], totals: [], incomeTotals: [] }))

    expect(await getPersonalEntries('2026-08')).toEqual({ month: '2026-08', entries: [], totals: [], incomeTotals: [] })
  })

  it.each(['month', 'entries', 'totals'])('throws an error naming "%s" when the answer lacks it', async (field) => {
    stubFetch(Response.json(withoutField(monthAnswerBody(octoberMonth), field)))

    await expect(getPersonalEntries('2026-10')).rejects.toThrow(field)
  })

  it.each(Object.keys(entryAnswerRow(coffeeEntry)))('throws an error naming "%s" when an entry lacks it', async (field) => {
    stubFetch(Response.json({ month: '2026-10', entries: [withoutField(entryAnswerRow(coffeeEntry), field)], totals: [], incomeTotals: [] }))

    await expect(getPersonalEntries('2026-10')).rejects.toThrow(field)
  })

  it.each([
    ['amountMinor', 12.5],
    ['amountMinor', -100],
    ['currency', 'USD'],
    ['entryDate', 'yesterday'],
    ['note', 42],
    ['mkdPerEur', 0],
    ['rateDate', null],
    ['updatedAt', 'later'],
  ])('throws an error naming "%s" when an entry has %j', async (field, value) => {
    stubFetch(Response.json({ month: '2026-10', entries: [{ ...entryAnswerRow(coffeeEntry), [field]: value }], totals: [], incomeTotals: [] }))

    await expect(getPersonalEntries('2026-10')).rejects.toThrow(field)
  })

  it('throws an error naming "color" when the category of an entry has a colour the app does not know', async () => {
    const row = { ...entryAnswerRow(coffeeEntry), category: { ...coffeeCategory, color: 'brown' } }
    stubFetch(Response.json({ month: '2026-10', entries: [row], totals: [], incomeTotals: [] }))

    await expect(getPersonalEntries('2026-10')).rejects.toThrow('color')
  })

  it('throws an error naming "categoryId" when it is not the id of the category the entry carries', async () => {
    const row = { ...entryAnswerRow(coffeeEntry), categoryId: archivedGymCategory.id }
    stubFetch(Response.json({ month: '2026-10', entries: [row], totals: [], incomeTotals: [] }))

    await expect(getPersonalEntries('2026-10')).rejects.toThrow('categoryId')
  })

  it.each([
    ['currency', 'USD'],
    ['totalMinor', -1],
    ['categories', null],
  ])('throws an error naming "%s" when a total has %j', async (field, value) => {
    const body = monthAnswerBody(octoberMonth)
    const totals = body.totals as Record<string, unknown>[]
    stubFetch(Response.json({ ...body, totals: [{ ...totals[0], [field]: value }] }))

    await expect(getPersonalEntries('2026-10')).rejects.toThrow(field)
  })

  it.each(['categoryId', 'category', 'totalMinor', 'count'])('throws an error naming "%s" when a category total lacks it', async (field) => {
    const row = { categoryId: coffeeCategory.id, category: coffeeCategory, totalMinor: 12000, count: 1 }
    const total = { currency: 'MKD', totalMinor: 12000, categories: [withoutField(row, field)] }
    stubFetch(Response.json({ month: '2026-10', entries: [], totals: [total], incomeTotals: [] }))

    await expect(getPersonalEntries('2026-10')).rejects.toThrow(field)
  })

  it('rethrows the guest refusal unchanged', async () => {
    stubFetch(problemResponse(403, 'GUEST_NOT_ALLOWED'))

    const error = await captureError(getPersonalEntries('2026-10'))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus: 403, errorCode: 'GUEST_NOT_ALLOWED' })
  })
})

describe('addPersonalEntry', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('sends POST /api/personal/entries with the request id, the category, the amount in minor units, the currency, the date and the note', async () => {
    const fetchMock = stubFetch(Response.json(entryAnswerRow(coffeeEntry)))

    await addPersonalEntry(newEntry)

    const request = sentRequest(fetchMock)
    expect(request.url).toBe('/api/personal/entries')
    expect(request.method).toBe('POST')
    expect(request.contentType).toBe('application/json')
    expect(request.body).toEqual({
      clientRequestId: newEntry.clientRequestId,
      categoryId: coffeeCategory.id,
      amountMinor: 12000,
      currency: 'MKD',
      entryDate: '2026-10-19',
      note: null,
    })
  })

  it('returns the saved entry from the answer', async () => {
    stubFetch(Response.json(entryAnswerRow(coffeeEntry)))

    expect(await addPersonalEntry(newEntry)).toEqual(coffeeEntry)
  })

  it('throws an error naming "amountMinor" when the answer has no amount', async () => {
    stubFetch(Response.json(withoutField(entryAnswerRow(coffeeEntry), 'amountMinor')))

    await expect(addPersonalEntry(newEntry)).rejects.toThrow('amountMinor')
  })

  it('rethrows the refusal with its error code', async () => {
    stubFetch(problemResponse(400, 'PERSONAL_ENTRY_CATEGORY_INVALID'))

    const error = await captureError(addPersonalEntry(newEntry))

    expect(error).toMatchObject({ httpStatus: 400, errorCode: 'PERSONAL_ENTRY_CATEGORY_INVALID' })
  })
})

describe('editPersonalEntry', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('sends PUT /api/personal/entries/{id} with the fields and no request id', async () => {
    const fetchMock = stubFetch(Response.json(entryAnswerRow(lunchInEuroEntry)))
    const fields: PersonalEntryFields = { ...typedFields, currency: 'EUR', amountMinor: 1250, note: 'Lunch in Ohrid' }

    await editPersonalEntry(lunchInEuroEntry.id, fields)

    const request = sentRequest(fetchMock)
    expect(request.url).toBe(`/api/personal/entries/${lunchInEuroEntry.id}`)
    expect(request.method).toBe('PUT')
    expect(request.body).toEqual(fields)
  })

  it('returns the changed entry from the answer', async () => {
    stubFetch(Response.json(entryAnswerRow(lunchInEuroEntry)))

    expect(await editPersonalEntry(lunchInEuroEntry.id, typedFields)).toEqual(lunchInEuroEntry)
  })

  it('rethrows a not-found refusal with its error code', async () => {
    stubFetch(problemResponse(404, 'PERSONAL_ENTRY_NOT_FOUND'))

    const error = await captureError(editPersonalEntry(coffeeEntry.id, typedFields))

    expect(error).toMatchObject({ httpStatus: 404, errorCode: 'PERSONAL_ENTRY_NOT_FOUND' })
  })
})

describe('deletePersonalEntry and restorePersonalEntry', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('sends DELETE /api/personal/entries/{id}', async () => {
    const fetchMock = stubFetch(noContent())

    await deletePersonalEntry(coffeeEntry.id)

    const request = sentRequest(fetchMock)
    expect(request.url).toBe(`/api/personal/entries/${coffeeEntry.id}`)
    expect(request.method).toBe('DELETE')
  })

  it('sends POST /api/personal/entries/{id}/restore', async () => {
    const fetchMock = stubFetch(noContent())

    await restorePersonalEntry(coffeeEntry.id)

    const request = sentRequest(fetchMock)
    expect(request.url).toBe(`/api/personal/entries/${coffeeEntry.id}/restore`)
    expect(request.method).toBe('POST')
  })

  it('rethrows a restore that came too late with its error code', async () => {
    stubFetch(problemResponse(400, 'PERSONAL_ENTRY_RESTORE_EXPIRED'))

    const error = await captureError(restorePersonalEntry(coffeeEntry.id))

    expect(error).toMatchObject({ httpStatus: 400, errorCode: 'PERSONAL_ENTRY_RESTORE_EXPIRED' })
  })
})
