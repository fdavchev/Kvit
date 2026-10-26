import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '@/core/api/apiClient'
import { captureError, problemResponse, sentRequest, stubFetch } from '@/test/apiTestHelpers'
import { archivedGymCategory, coffeeCategory } from '@/test/personalCategoryTestData'
import {
  coffeeEntry,
  emptyMonth,
  entryAnswerRow,
  generatedSalaryEntry,
  gymEntryWithArchivedCategory,
  lunchInEuroEntry,
  monthAnswerBody,
  octoberMonth,
  octoberWithSalary,
  salaryIncomeEntry,
} from '@/test/personalEntryTestData'
import {
  addPersonalEntry,
  deletePersonalEntry,
  editPersonalEntry,
  getPersonalEntries,
  restorePersonalEntry,
  type NewPersonalEntry,
  type PersonalIncomeFields,
  type PersonalSpendingFields,
} from './personalEntriesService'

const typedFields: PersonalSpendingFields = {
  kind: 'Spending',
  categoryId: coffeeCategory.id,
  amountMinor: 12000,
  currency: 'MKD',
  entryDate: '2026-10-19',
  note: null,
}

const incomeFields: PersonalIncomeFields = {
  kind: 'Income',
  amountMinor: 6500000,
  currency: 'MKD',
  entryDate: '2026-10-15',
  note: 'Salary',
}

const newEntry: NewPersonalEntry = {
  clientRequestId: '11111111-2222-4333-8444-555555555555',
  ...typedFields,
}

const newIncome: NewPersonalEntry = {
  clientRequestId: '66666666-7777-4888-8999-000000000000',
  ...incomeFields,
}

function withoutField(source: object, field: string): object {
  return Object.fromEntries(Object.entries(source).filter(([name]) => name !== field))
}

function noContent(): Response {
  return new Response(null, { status: 204 })
}

async function errorMessageOf(promise: Promise<unknown>): Promise<string> {
  const error = await captureError(promise)
  if (!(error instanceof Error)) {
    throw new Error('Expected the call to throw an Error')
  }
  return error.message
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
    stubFetch(Response.json({ ...monthAnswerBody(emptyMonth('2026-09')), entries: [entryAnswerRow(gymEntryWithArchivedCategory)] }))

    const month = await getPersonalEntries('2026-09')

    expect(month.entries[0].category).toEqual(archivedGymCategory)
  })

  it('returns an empty month when the server has nothing', async () => {
    stubFetch(Response.json({ ...monthAnswerBody(emptyMonth('2026-08')), entries: [] }))

    expect(await getPersonalEntries('2026-08')).toEqual(emptyMonth('2026-08'))
  })

  it.each(['month', 'entries', 'totals', 'incomeTotals'])('throws an error naming "%s" when the answer lacks it', async (field) => {
    stubFetch(Response.json(withoutField(monthAnswerBody(octoberMonth), field)))

    await expect(getPersonalEntries('2026-10')).rejects.toThrow(field)
  })

  it.each(Object.keys(entryAnswerRow(coffeeEntry)).filter((field) => field !== 'recurringIncomeId'))(
    'throws an error naming "%s" when an entry lacks it',
    async (field) => {
      stubFetch(Response.json({ ...monthAnswerBody(emptyMonth('2026-10')), entries: [withoutField(entryAnswerRow(coffeeEntry), field)] }))

      await expect(getPersonalEntries('2026-10')).rejects.toThrow(field)
    },
  )

  it('keeps the repeating income an entry was made by, and null for an entry the person added', async () => {
    stubFetch(Response.json({ ...monthAnswerBody(emptyMonth('2026-10')), entries: [entryAnswerRow(generatedSalaryEntry), entryAnswerRow(salaryIncomeEntry)] }))

    const month = await getPersonalEntries('2026-10')

    expect(month.entries.map((entry) => entry.recurringIncomeId)).toEqual([generatedSalaryEntry.recurringIncomeId, null])
  })

  it('reads an entry without "recurringIncomeId" as one the person added, so an older API still works', async () => {
    stubFetch(Response.json({ ...monthAnswerBody(emptyMonth('2026-10')), entries: [withoutField(entryAnswerRow(salaryIncomeEntry), 'recurringIncomeId')] }))

    const month = await getPersonalEntries('2026-10')

    expect(month.entries).toEqual([salaryIncomeEntry])
  })

  it.each([42, true, {}])('throws an error naming "recurringIncomeId" when an entry has %j', async (value) => {
    stubFetch(Response.json({ ...monthAnswerBody(emptyMonth('2026-10')), entries: [{ ...entryAnswerRow(salaryIncomeEntry), recurringIncomeId: value }] }))

    await expect(getPersonalEntries('2026-10')).rejects.toThrow('recurringIncomeId')
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
    stubFetch(Response.json({ ...monthAnswerBody(emptyMonth('2026-10')), entries: [{ ...entryAnswerRow(coffeeEntry), [field]: value }] }))

    await expect(getPersonalEntries('2026-10')).rejects.toThrow(field)
  })

  it('throws an error naming "color" when the category of an entry has a colour the app does not know', async () => {
    const row = { ...entryAnswerRow(coffeeEntry), category: { ...coffeeCategory, color: 'brown' } }
    stubFetch(Response.json({ ...monthAnswerBody(emptyMonth('2026-10')), entries: [row] }))

    await expect(getPersonalEntries('2026-10')).rejects.toThrow('color')
  })

  it('throws an error naming "categoryId" when it is not the id of the category the entry carries', async () => {
    const row = { ...entryAnswerRow(coffeeEntry), categoryId: archivedGymCategory.id }
    stubFetch(Response.json({ ...monthAnswerBody(emptyMonth('2026-10')), entries: [row] }))

    await expect(getPersonalEntries('2026-10')).rejects.toThrow('categoryId')
  })

  it('returns an income entry with a null category and keeps the income totals', async () => {
    stubFetch(Response.json(monthAnswerBody(octoberWithSalary)))

    const month = await getPersonalEntries('2026-10')

    expect(month).toEqual(octoberWithSalary)
    expect(month.entries[2].category).toBeNull()
    expect(month.incomeTotals).toEqual([{ currency: 'MKD', totalMinor: 6500000, count: 1 }])
  })

  it('reads the JSON nulls of an income row as a null category', async () => {
    const row = entryAnswerRow(salaryIncomeEntry)
    stubFetch(Response.json({ ...monthAnswerBody(emptyMonth('2026-10')), entries: [row] }))

    expect(row).toMatchObject({ kind: 'Income', category: null, categoryId: null })
    expect((await getPersonalEntries('2026-10')).entries).toEqual([salaryIncomeEntry])
  })

  it('throws an error naming Spending and the category when a Spending row has no category', async () => {
    const row = { ...entryAnswerRow(coffeeEntry), category: null, categoryId: null }
    stubFetch(Response.json({ ...monthAnswerBody(emptyMonth('2026-10')), entries: [row] }))

    const message = await errorMessageOf(getPersonalEntries('2026-10'))

    expect(message).toMatch(/spending/i)
    expect(message).toMatch(/category/i)
  })

  it.each([
    ['a category and its id', { category: coffeeCategory, categoryId: coffeeCategory.id }],
    ['only a category', { category: coffeeCategory, categoryId: null }],
    ['only a category id', { category: null, categoryId: coffeeCategory.id }],
  ])('throws an error naming Income and the category when an Income row has %s', async (_name, categoryFields) => {
    const row = { ...entryAnswerRow(salaryIncomeEntry), ...categoryFields }
    stubFetch(Response.json({ ...monthAnswerBody(emptyMonth('2026-10')), entries: [row] }))

    const message = await errorMessageOf(getPersonalEntries('2026-10'))

    expect(message).toMatch(/income/i)
    expect(message).toMatch(/category/i)
  })

  it('throws an error naming "kind" when a row has a kind the app does not know', async () => {
    const row = { ...entryAnswerRow(coffeeEntry), kind: 'Transfer' }
    stubFetch(Response.json({ ...monthAnswerBody(emptyMonth('2026-10')), entries: [row] }))

    await expect(getPersonalEntries('2026-10')).rejects.toThrow('kind')
  })

  it.each([
    ['currency', 'USD'],
    ['totalMinor', -1],
    ['count', 1.5],
  ])('throws an error naming "%s" when an income total has %j', async (field, value) => {
    const incomeTotal = { currency: 'MKD', totalMinor: 6500000, count: 1, [field]: value }
    stubFetch(Response.json({ ...monthAnswerBody(octoberMonth), incomeTotals: [incomeTotal] }))

    await expect(getPersonalEntries('2026-10')).rejects.toThrow(field)
  })

  it.each(['currency', 'totalMinor', 'count'])('throws an error naming "%s" when an income total lacks it', async (field) => {
    const incomeTotal = withoutField({ currency: 'MKD', totalMinor: 6500000, count: 1 }, field)
    stubFetch(Response.json({ ...monthAnswerBody(octoberMonth), incomeTotals: [incomeTotal] }))

    await expect(getPersonalEntries('2026-10')).rejects.toThrow(field)
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
    stubFetch(Response.json({ ...monthAnswerBody(emptyMonth('2026-10')), totals: [total] }))

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

  it('sends POST /api/personal/entries with the request id, kind Spending, the category, the amount in minor units, the currency, the date and the note', async () => {
    const fetchMock = stubFetch(Response.json(entryAnswerRow(coffeeEntry)))

    await addPersonalEntry(newEntry)

    const request = sentRequest(fetchMock)
    expect(request.url).toBe('/api/personal/entries')
    expect(request.method).toBe('POST')
    expect(request.contentType).toBe('application/json')
    expect(request.body).toEqual({
      clientRequestId: newEntry.clientRequestId,
      kind: 'Spending',
      categoryId: coffeeCategory.id,
      amountMinor: 12000,
      currency: 'MKD',
      entryDate: '2026-10-19',
      note: null,
    })
  })

  it('sends POST /api/personal/entries with kind Income and no categoryId key for an income', async () => {
    const fetchMock = stubFetch(Response.json(entryAnswerRow(salaryIncomeEntry)))

    await addPersonalEntry(newIncome)

    const request = sentRequest(fetchMock)
    expect(request.url).toBe('/api/personal/entries')
    expect(request.method).toBe('POST')
    expect(request.body).toEqual({
      clientRequestId: newIncome.clientRequestId,
      kind: 'Income',
      amountMinor: 6500000,
      currency: 'MKD',
      entryDate: '2026-10-15',
      note: 'Salary',
    })
    expect(Object.keys(request.body as object)).not.toContain('categoryId')
  })

  it('returns the saved entry from the answer', async () => {
    stubFetch(Response.json(entryAnswerRow(coffeeEntry)))

    expect(await addPersonalEntry(newEntry)).toEqual(coffeeEntry)
  })

  it('returns the saved income from the answer, with a null category', async () => {
    stubFetch(Response.json(entryAnswerRow(salaryIncomeEntry)))

    expect(await addPersonalEntry(newIncome)).toEqual(salaryIncomeEntry)
  })

  it('reads an answer without "recurringIncomeId" as an entry the person added, so an older API still works', async () => {
    stubFetch(Response.json(withoutField(entryAnswerRow(salaryIncomeEntry), 'recurringIncomeId')))

    expect(await addPersonalEntry(newIncome)).toEqual(salaryIncomeEntry)
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

  it('rethrows the refusal of an income with a category with its error code', async () => {
    stubFetch(problemResponse(400, 'PERSONAL_ENTRY_INCOME_HAS_CATEGORY'))

    const error = await captureError(addPersonalEntry(newIncome))

    expect(error).toMatchObject({ httpStatus: 400, errorCode: 'PERSONAL_ENTRY_INCOME_HAS_CATEGORY' })
  })
})

describe('editPersonalEntry', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('sends PUT /api/personal/entries/{id} with kind Spending, the fields and no request id', async () => {
    const fetchMock = stubFetch(Response.json(entryAnswerRow(lunchInEuroEntry)))
    const fields: PersonalSpendingFields = { ...typedFields, currency: 'EUR', amountMinor: 1250, note: 'Lunch in Ohrid' }

    await editPersonalEntry(lunchInEuroEntry.id, fields)

    const request = sentRequest(fetchMock)
    expect(request.url).toBe(`/api/personal/entries/${lunchInEuroEntry.id}`)
    expect(request.method).toBe('PUT')
    expect(request.body).toEqual({
      kind: 'Spending',
      categoryId: coffeeCategory.id,
      amountMinor: 1250,
      currency: 'EUR',
      entryDate: '2026-10-19',
      note: 'Lunch in Ohrid',
    })
    expect(Object.keys(request.body as object)).not.toContain('clientRequestId')
  })

  it('sends PUT with kind Income, no categoryId key and no request id for an income', async () => {
    const fetchMock = stubFetch(Response.json(entryAnswerRow(salaryIncomeEntry)))

    await editPersonalEntry(salaryIncomeEntry.id, incomeFields)

    const request = sentRequest(fetchMock)
    expect(request.url).toBe(`/api/personal/entries/${salaryIncomeEntry.id}`)
    expect(request.method).toBe('PUT')
    expect(request.body).toEqual({
      kind: 'Income',
      amountMinor: 6500000,
      currency: 'MKD',
      entryDate: '2026-10-15',
      note: 'Salary',
    })
    expect(Object.keys(request.body as object)).not.toContain('categoryId')
    expect(Object.keys(request.body as object)).not.toContain('clientRequestId')
  })

  it('returns the changed entry from the answer', async () => {
    stubFetch(Response.json(entryAnswerRow(lunchInEuroEntry)))

    expect(await editPersonalEntry(lunchInEuroEntry.id, typedFields)).toEqual(lunchInEuroEntry)
  })

  it('returns the changed income from the answer, with a null category', async () => {
    stubFetch(Response.json(entryAnswerRow(salaryIncomeEntry)))

    expect(await editPersonalEntry(salaryIncomeEntry.id, incomeFields)).toEqual(salaryIncomeEntry)
  })

  it('returns the repeating income an edited entry was made by', async () => {
    stubFetch(Response.json(entryAnswerRow(generatedSalaryEntry)))

    expect(await editPersonalEntry(generatedSalaryEntry.id, incomeFields)).toEqual(generatedSalaryEntry)
  })

  it('reads an answer without "recurringIncomeId" as an entry the person added, so an older API still works', async () => {
    stubFetch(Response.json(withoutField(entryAnswerRow(salaryIncomeEntry), 'recurringIncomeId')))

    expect(await editPersonalEntry(salaryIncomeEntry.id, incomeFields)).toEqual(salaryIncomeEntry)
  })

  it('rethrows the refusal to move the date of an entry made by a repeating income with its error code', async () => {
    stubFetch(problemResponse(400, 'PERSONAL_ENTRY_DATE_LOCKED'))

    const error = await captureError(editPersonalEntry(generatedSalaryEntry.id, incomeFields))

    expect(error).toMatchObject({ httpStatus: 400, errorCode: 'PERSONAL_ENTRY_DATE_LOCKED' })
  })

  it('rethrows a not-found refusal with its error code', async () => {
    stubFetch(problemResponse(404, 'PERSONAL_ENTRY_NOT_FOUND'))

    const error = await captureError(editPersonalEntry(coffeeEntry.id, typedFields))

    expect(error).toMatchObject({ httpStatus: 404, errorCode: 'PERSONAL_ENTRY_NOT_FOUND' })
  })

  it('rethrows the refusal to change the kind of an entry with its error code', async () => {
    stubFetch(problemResponse(400, 'PERSONAL_ENTRY_KIND_CANNOT_CHANGE'))

    const error = await captureError(editPersonalEntry(coffeeEntry.id, incomeFields))

    expect(error).toMatchObject({ httpStatus: 400, errorCode: 'PERSONAL_ENTRY_KIND_CANNOT_CHANGE' })
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
