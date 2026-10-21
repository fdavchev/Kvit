import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '@/core/api/apiClient'
import { captureError, problemResponse, sentRequest, stubFetch } from '@/test/apiTestHelpers'
import {
  flatRentRecurringIncome,
  recurringIncomeAnswerRow,
  recurringListAnswerBody,
  salaryRecurringIncome,
  tutoringRecurringIncome,
  untitledRecurringIncome,
} from '@/test/recurringIncomeTestData'
import {
  catchUpRecurringIncomes,
  createRecurringIncome,
  editRecurringIncome,
  getRecurringIncomes,
  restartRecurringIncome,
  stopRecurringIncome,
  type NewRecurringIncome,
  type RecurringIncomeFields,
} from './recurringIncomesService'

const monthlyFields: RecurringIncomeFields = {
  amountMinor: 3000000,
  currency: 'MKD',
  title: 'Salary',
  frequency: 'Monthly',
  dayOfMonth: 1,
  dayOfWeek: null,
  startsOn: '2026-08-01',
  endsOn: null,
}

const weeklyFields: RecurringIncomeFields = {
  amountMinor: 150000,
  currency: 'MKD',
  title: null,
  frequency: 'Weekly',
  dayOfMonth: null,
  dayOfWeek: 2,
  startsOn: '2026-09-01',
  endsOn: '2027-01-20',
}

const newMonthly: NewRecurringIncome = {
  clientRequestId: '11111111-2222-4333-8444-555555555555',
  ...monthlyFields,
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

describe('getRecurringIncomes', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('asks GET /api/personal/recurring-incomes', async () => {
    const fetchMock = stubFetch(Response.json(recurringListAnswerBody([])))

    await getRecurringIncomes()

    const request = sentRequest(fetchMock)
    expect(request.url).toBe('/api/personal/recurring-incomes')
    expect(request.method ?? 'GET').toBe('GET')
  })

  it('returns the repeating incomes in the order of the answer, with their next dates', async () => {
    const incomes = [tutoringRecurringIncome, salaryRecurringIncome, flatRentRecurringIncome, untitledRecurringIncome]
    stubFetch(Response.json(recurringListAnswerBody(incomes)))

    expect(await getRecurringIncomes()).toEqual(incomes)
  })

  it('returns an empty list when the person has no repeating income', async () => {
    stubFetch(Response.json(recurringListAnswerBody([])))

    expect(await getRecurringIncomes()).toEqual([])
  })

  it('keeps a null title and a null next date as null', async () => {
    stubFetch(Response.json(recurringListAnswerBody([untitledRecurringIncome])))

    const [income] = await getRecurringIncomes()

    expect(income.title).toBeNull()
    expect(income.nextDate).toBeNull()
    expect(income.endsOn).toBe('2026-10-15')
  })

  it('keeps whether the income already has entries', async () => {
    stubFetch(Response.json(recurringListAnswerBody([{ ...salaryRecurringIncome, hasEntries: true }, flatRentRecurringIncome])))

    const incomes = await getRecurringIncomes()

    expect(incomes.map((income) => income.hasEntries)).toEqual([true, false])
  })

  it('keeps a null end date as null', async () => {
    stubFetch(Response.json(recurringListAnswerBody([salaryRecurringIncome])))

    const [income] = await getRecurringIncomes()

    expect(income.endsOn).toBeNull()
  })

  it('throws an error naming "recurringIncomes" when the answer lacks the list', async () => {
    stubFetch(Response.json({}))

    await expect(getRecurringIncomes()).rejects.toThrow('recurringIncomes')
  })

  it('throws an error naming "recurringIncomes" when the answer is not a list', async () => {
    stubFetch(Response.json({ recurringIncomes: 'none' }))

    await expect(getRecurringIncomes()).rejects.toThrow('recurringIncomes')
  })

  it.each(Object.keys(recurringIncomeAnswerRow(salaryRecurringIncome)))('throws an error naming "%s" when a row lacks it', async (field) => {
    stubFetch(Response.json({ recurringIncomes: [withoutField(recurringIncomeAnswerRow(salaryRecurringIncome), field)] }))

    await expect(getRecurringIncomes()).rejects.toThrow(field)
  })

  it.each([
    ['id', 42],
    ['amountMinor', 12.5],
    ['amountMinor', -100],
    ['currency', 'USD'],
    ['title', 42],
    ['frequency', 'Daily'],
    ['dayOfMonth', 'first'],
    ['dayOfMonth', 0],
    ['dayOfMonth', 32],
    ['startsOn', 'soon'],
    ['endsOn', 'later'],
    ['createdAt', null],
    ['nextDate', 'tomorrow'],
    ['hasEntries', 'yes'],
    ['hasEntries', null],
    ['hasEntries', 1],
  ])('throws an error naming "%s" when a row has %j', async (field, value) => {
    stubFetch(Response.json({ recurringIncomes: [{ ...recurringIncomeAnswerRow(salaryRecurringIncome), [field]: value }] }))

    await expect(getRecurringIncomes()).rejects.toThrow(field)
  })

  it.each([
    ['dayOfWeek', 7],
    ['dayOfWeek', -1],
    ['dayOfWeek', 1.5],
    ['dayOfMonth', 3],
  ])('throws an error naming "%s" when a weekly row has %j', async (field, value) => {
    stubFetch(Response.json({ recurringIncomes: [{ ...recurringIncomeAnswerRow(tutoringRecurringIncome), [field]: value }] }))

    await expect(getRecurringIncomes()).rejects.toThrow(field)
  })

  it('throws an error naming Monthly and "dayOfMonth" when a monthly row has no day of the month', async () => {
    stubFetch(Response.json({ recurringIncomes: [{ ...recurringIncomeAnswerRow(salaryRecurringIncome), dayOfMonth: null }] }))

    const message = await errorMessageOf(getRecurringIncomes())

    expect(message).toMatch(/monthly/i)
    expect(message).toContain('dayOfMonth')
  })

  it('throws an error naming Monthly and "dayOfWeek" when a monthly row also has a weekday', async () => {
    stubFetch(Response.json({ recurringIncomes: [{ ...recurringIncomeAnswerRow(salaryRecurringIncome), dayOfWeek: 2 }] }))

    const message = await errorMessageOf(getRecurringIncomes())

    expect(message).toMatch(/monthly/i)
    expect(message).toContain('dayOfWeek')
  })

  it('throws an error naming Weekly and "dayOfWeek" when a weekly row has no weekday', async () => {
    stubFetch(Response.json({ recurringIncomes: [{ ...recurringIncomeAnswerRow(tutoringRecurringIncome), dayOfWeek: null }] }))

    const message = await errorMessageOf(getRecurringIncomes())

    expect(message).toMatch(/weekly/i)
    expect(message).toContain('dayOfWeek')
  })

  it('accepts day 31 of the month and Sunday (0) as weekday', async () => {
    const lastDay = { ...salaryRecurringIncome, dayOfMonth: 31 }
    const sunday = { ...tutoringRecurringIncome, dayOfWeek: 0 }
    stubFetch(Response.json(recurringListAnswerBody([lastDay, sunday])))

    expect(await getRecurringIncomes()).toEqual([lastDay, sunday])
  })

  it('rethrows the guest refusal unchanged', async () => {
    stubFetch(problemResponse(403, 'GUEST_NOT_ALLOWED'))

    const error = await captureError(getRecurringIncomes())

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus: 403, errorCode: 'GUEST_NOT_ALLOWED' })
  })
})

describe('createRecurringIncome', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('sends POST /api/personal/recurring-incomes with the request id and a monthly income: the day of the month and no weekday', async () => {
    const fetchMock = stubFetch(Response.json(recurringIncomeAnswerRow(salaryRecurringIncome)))

    await createRecurringIncome(newMonthly)

    const request = sentRequest(fetchMock)
    expect(request.url).toBe('/api/personal/recurring-incomes')
    expect(request.method).toBe('POST')
    expect(request.contentType).toBe('application/json')
    expect(request.body).toEqual({
      clientRequestId: newMonthly.clientRequestId,
      amountMinor: 3000000,
      currency: 'MKD',
      title: 'Salary',
      frequency: 'Monthly',
      dayOfMonth: 1,
      dayOfWeek: null,
      startsOn: '2026-08-01',
      endsOn: null,
    })
  })

  it('sends a weekly income with the weekday, a null day of the month, no title and an end date', async () => {
    const fetchMock = stubFetch(Response.json(recurringIncomeAnswerRow(tutoringRecurringIncome)))

    await createRecurringIncome({ clientRequestId: newMonthly.clientRequestId, ...weeklyFields })

    expect(sentRequest(fetchMock).body).toEqual({
      clientRequestId: newMonthly.clientRequestId,
      amountMinor: 150000,
      currency: 'MKD',
      title: null,
      frequency: 'Weekly',
      dayOfMonth: null,
      dayOfWeek: 2,
      startsOn: '2026-09-01',
      endsOn: '2027-01-20',
    })
  })

  it('returns the saved row from the answer', async () => {
    stubFetch(Response.json(recurringIncomeAnswerRow(salaryRecurringIncome)))

    expect(await createRecurringIncome(newMonthly)).toEqual(salaryRecurringIncome)
  })

  it('throws an error naming "frequency" when the answer lacks it', async () => {
    stubFetch(Response.json(withoutField(recurringIncomeAnswerRow(salaryRecurringIncome), 'frequency')))

    await expect(createRecurringIncome(newMonthly)).rejects.toThrow('frequency')
  })

  it.each(['RECURRING_INCOME_LIMIT_REACHED', 'RECURRING_INCOME_START_DATE_INVALID', 'RECURRING_INCOME_CLIENT_REQUEST_ID_USED'])(
    'rethrows the refusal %s with its error code',
    async (errorCode) => {
      stubFetch(problemResponse(400, errorCode))

      const error = await captureError(createRecurringIncome(newMonthly))

      expect(error).toMatchObject({ httpStatus: 400, errorCode })
    },
  )
})

describe('editRecurringIncome', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('sends PUT /api/personal/recurring-incomes/{id} with the fields and no request id', async () => {
    const fetchMock = stubFetch(Response.json(recurringIncomeAnswerRow(salaryRecurringIncome)))

    await editRecurringIncome(salaryRecurringIncome.id, monthlyFields)

    const request = sentRequest(fetchMock)
    expect(request.url).toBe(`/api/personal/recurring-incomes/${salaryRecurringIncome.id}`)
    expect(request.method).toBe('PUT')
    expect(request.body).toEqual(monthlyFields)
    expect(Object.keys(request.body as object)).not.toContain('clientRequestId')
  })

  it('sends a weekly edit with the weekday and a null day of the month', async () => {
    const fetchMock = stubFetch(Response.json(recurringIncomeAnswerRow(tutoringRecurringIncome)))

    await editRecurringIncome(tutoringRecurringIncome.id, weeklyFields)

    expect(sentRequest(fetchMock).body).toEqual(weeklyFields)
  })

  it('returns the changed row from the answer', async () => {
    stubFetch(Response.json(recurringIncomeAnswerRow(flatRentRecurringIncome)))

    expect(await editRecurringIncome(flatRentRecurringIncome.id, monthlyFields)).toEqual(flatRentRecurringIncome)
  })

  it.each([
    [404, 'RECURRING_INCOME_NOT_FOUND'],
    [400, 'RECURRING_INCOME_START_CANNOT_CHANGE'],
  ])('rethrows the refusal %i %s with its error code', async (status, errorCode) => {
    stubFetch(problemResponse(status, errorCode))

    const error = await captureError(editRecurringIncome(salaryRecurringIncome.id, monthlyFields))

    expect(error).toMatchObject({ httpStatus: status, errorCode })
  })
})

describe('stopRecurringIncome and restartRecurringIncome', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('sends POST /api/personal/recurring-incomes/{id}/stop', async () => {
    const fetchMock = stubFetch(noContent())

    await stopRecurringIncome(salaryRecurringIncome.id)

    const request = sentRequest(fetchMock)
    expect(request.url).toBe(`/api/personal/recurring-incomes/${salaryRecurringIncome.id}/stop`)
    expect(request.method).toBe('POST')
  })

  it('sends POST /api/personal/recurring-incomes/{id}/restart', async () => {
    const fetchMock = stubFetch(noContent())

    await restartRecurringIncome(salaryRecurringIncome.id)

    const request = sentRequest(fetchMock)
    expect(request.url).toBe(`/api/personal/recurring-incomes/${salaryRecurringIncome.id}/restart`)
    expect(request.method).toBe('POST')
  })

  it.each([
    ['RECURRING_INCOME_RESTART_EXPIRED', 400],
    ['RECURRING_INCOME_NOT_STOPPED', 400],
    ['RECURRING_INCOME_NOT_FOUND', 404],
  ])('rethrows the refusal of a restart %s with its error code', async (errorCode, status) => {
    stubFetch(problemResponse(status, errorCode))

    const error = await captureError(restartRecurringIncome(salaryRecurringIncome.id))

    expect(error).toMatchObject({ httpStatus: status, errorCode })
  })

  it('rethrows the not-found refusal of a stop with its error code', async () => {
    stubFetch(problemResponse(404, 'RECURRING_INCOME_NOT_FOUND'))

    const error = await captureError(stopRecurringIncome(salaryRecurringIncome.id))

    expect(error).toMatchObject({ httpStatus: 404, errorCode: 'RECURRING_INCOME_NOT_FOUND' })
  })
})

describe('catchUpRecurringIncomes', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('sends POST /api/personal/recurring-incomes/catch-up with no body', async () => {
    const fetchMock = stubFetch(Response.json({ created: 0 }))

    await catchUpRecurringIncomes()

    const request = sentRequest(fetchMock)
    expect(request.url).toBe('/api/personal/recurring-incomes/catch-up')
    expect(request.method).toBe('POST')
    expect(request.body).toBeUndefined()
  })

  it.each([0, 1, 3])('returns the number of created entries: %i', async (created) => {
    stubFetch(Response.json({ created }))

    expect(await catchUpRecurringIncomes()).toBe(created)
  })

  it('throws an error naming "created" when the answer lacks it', async () => {
    stubFetch(Response.json({}))

    await expect(catchUpRecurringIncomes()).rejects.toThrow('created')
  })

  it.each([-1, 1.5, 'two', null])('throws an error naming "created" when it is %j', async (created) => {
    stubFetch(Response.json({ created }))

    await expect(catchUpRecurringIncomes()).rejects.toThrow('created')
  })

  it('rethrows the guest refusal with its error code', async () => {
    stubFetch(problemResponse(403, 'GUEST_NOT_ALLOWED'))

    const error = await captureError(catchUpRecurringIncomes())

    expect(error).toMatchObject({ httpStatus: 403, errorCode: 'GUEST_NOT_ALLOWED' })
  })
})
