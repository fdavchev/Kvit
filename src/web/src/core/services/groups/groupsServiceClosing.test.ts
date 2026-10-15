import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '@/core/api/apiClient'
import {
  captureError,
  problemResponse,
  sentRequest,
  stubFetch,
} from '@/test/apiTestHelpers'
import {
  anaClosingRow,
  closingConfirmPath,
  closingEndsAt,
  closingGroup,
  closingGroupAnaConfirmed,
  closingObjectPath,
  closingPath,
  closingStartedAt,
  finishedGroup,
  readyToCloseGroup,
  unlockPath,
} from '@/test/closingTestData'
import { testGroup, testGroupId } from '@/test/groupTestData'
import {
  confirmClosing,
  getGroup,
  objectToClosing,
  startClosing,
  unlockGroup,
} from './groupsService'

const closingFields = [
  'closingStartedAt',
  'closingEndsAt',
  'closingConfirmedBy',
  'closingWaitingFor',
  'canStartClosing',
]

function withoutField(source: object, field: string): object {
  return Object.fromEntries(Object.entries(source).filter(([name]) => name !== field))
}

function noContent(): Response {
  return new Response(null, { status: 204 })
}

describe('getGroup and the closing fields', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('returns an open group with no times, nobody in the lists and canStartClosing as sent', async () => {
    stubFetch(Response.json(readyToCloseGroup))

    const group = await getGroup(testGroupId)

    expect(group).toEqual(readyToCloseGroup)
    expect(group.closingStartedAt).toBeNull()
    expect(group.closingEndsAt).toBeNull()
    expect(group.closingConfirmedBy).toEqual([])
    expect(group.closingWaitingFor).toEqual([])
    expect(group.canStartClosing).toBe(true)
  })

  it('returns a group that is being closed with its times and the people who still have to answer', async () => {
    stubFetch(Response.json(closingGroup))

    const group = await getGroup(testGroupId)

    expect(group.status).toBe('Closing')
    expect(group.closingStartedAt).toBe(closingStartedAt)
    expect(group.closingEndsAt).toBe(closingEndsAt)
    expect(group.closingConfirmedBy).toEqual([])
    expect(group.closingWaitingFor).toHaveLength(2)
    expect(group.closingWaitingFor[0]).toEqual(anaClosingRow)
  })

  it('returns who confirmed and who is still waited for', async () => {
    stubFetch(Response.json(closingGroupAnaConfirmed))

    const group = await getGroup(testGroupId)

    expect(group.closingConfirmedBy).toEqual([anaClosingRow])
    expect(group.closingWaitingFor.map((row) => row.name)).toEqual(['Petar'])
  })

  it('returns a finished group with the people who confirmed and nobody waited for', async () => {
    stubFetch(Response.json(finishedGroup))

    const group = await getGroup(testGroupId)

    expect(group).toEqual(finishedGroup)
    expect(group.status).toBe('Finished')
    expect(group.closingWaitingFor).toEqual([])
  })

  it.each(closingFields)('throws an error naming "%s" when an open group lacks it', async (field) => {
    stubFetch(Response.json(withoutField(testGroup, field)))

    await expect(getGroup(testGroupId)).rejects.toThrow(field)
  })

  it.each(closingFields)('throws an error naming "%s" when a group that is being closed lacks it', async (field) => {
    stubFetch(Response.json(withoutField(closingGroup, field)))

    await expect(getGroup(testGroupId)).rejects.toThrow(field)
  })

  it.each([
    ['closingStartedAt', 5],
    ['closingStartedAt', 'yesterday'],
    ['closingEndsAt', 7],
    ['closingEndsAt', 'soon'],
    ['closingConfirmedBy', 'Ana'],
    ['closingConfirmedBy', null],
    ['closingConfirmedBy', {}],
    ['closingWaitingFor', 'Petar'],
    ['closingWaitingFor', null],
    ['closingWaitingFor', {}],
    ['canStartClosing', 'yes'],
    ['canStartClosing', 1],
    ['canStartClosing', null],
  ])('throws an error naming "%s" when its value is %j', async (field, value) => {
    stubFetch(Response.json({ ...closingGroup, [field]: value }))

    await expect(getGroup(testGroupId)).rejects.toThrow(field)
  })

  it.each(['closingConfirmedBy', 'closingWaitingFor'])('throws an error naming "id" when a person in %s lacks it', async (field) => {
    stubFetch(Response.json({ ...closingGroup, [field]: [{ name: 'Ana' }] }))

    await expect(getGroup(testGroupId)).rejects.toThrow(/"id"/)
  })

  it.each(['closingConfirmedBy', 'closingWaitingFor'])('throws an error naming "name" when a person in %s lacks it', async (field) => {
    stubFetch(Response.json({ ...closingGroup, [field]: [{ id: anaClosingRow.id }] }))

    await expect(getGroup(testGroupId)).rejects.toThrow(/"name"/)
  })

  it.each([
    ['id', 42],
    ['id', null],
    ['name', null],
    ['name', 7],
  ])('throws an error naming "%s" when its value for a person in the lists is %j', async (field, value) => {
    stubFetch(Response.json({ ...closingGroup, closingWaitingFor: [{ ...anaClosingRow, [field]: value }] }))

    await expect(getGroup(testGroupId)).rejects.toThrow(new RegExp(`"${field}"`))
  })

  it.each([
    ['a text', 'Ana'],
    ['null', null],
  ])('throws an error when a person in the list is %s instead of an object', async (_name, row) => {
    stubFetch(Response.json({ ...closingGroup, closingWaitingFor: [row] }))

    await expect(getGroup(testGroupId)).rejects.toThrow(/object/)
  })
})

describe('startClosing', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('sends POST /api/groups/{id}/closing with no body and resolves on 204', async () => {
    const fetchMock = stubFetch(noContent())

    const result = await startClosing(testGroupId)

    expect(result).toBeUndefined()
    expect(sentRequest(fetchMock)).toEqual({
      url: closingPath,
      method: 'POST',
      contentType: null,
      body: undefined,
    })
  })

  it.each([
    ['not kvit yet', 400, 'GROUP_NOT_KVIT'],
    ['not the owner', 403, 'GROUP_NOT_OWNER'],
    ['a One bill', 400, 'GROUP_IS_ONE_BILL'],
    ['already finished', 400, 'GROUP_FINISHED'],
  ])('rethrows the ApiError unchanged when the group is %s', async (_name, httpStatus, errorCode) => {
    stubFetch(problemResponse(httpStatus, errorCode))

    const error = await captureError(startClosing(testGroupId))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus, errorCode })
  })

  it('rethrows the ApiError unchanged when the network fails', async () => {
    stubFetch(new TypeError('Failed to fetch'))

    const error = await captureError(startClosing(testGroupId))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus: null })
  })
})

describe('confirmClosing', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('sends POST /api/groups/{id}/closing/confirm with no body and resolves on 204', async () => {
    const fetchMock = stubFetch(noContent())

    const result = await confirmClosing(testGroupId)

    expect(result).toBeUndefined()
    expect(sentRequest(fetchMock)).toEqual({
      url: closingConfirmPath,
      method: 'POST',
      contentType: null,
      body: undefined,
    })
  })

  it.each([
    ['already answered', 400, 'CLOSING_ALREADY_ANSWERED'],
    ['not being closed any more', 400, 'GROUP_NOT_CLOSING'],
    ['asked of the owner', 403, 'CLOSING_OWNER_CANNOT_ANSWER'],
  ])('rethrows the ApiError unchanged when the group is %s', async (_name, httpStatus, errorCode) => {
    stubFetch(problemResponse(httpStatus, errorCode))

    const error = await captureError(confirmClosing(testGroupId))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus, errorCode })
  })
})

describe('objectToClosing', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('sends POST /api/groups/{id}/closing/object with the reason as JSON and resolves on 204', async () => {
    const fetchMock = stubFetch(noContent())

    const result = await objectToClosing(testGroupId, 'Marko still owes me for the taxi')

    expect(result).toBeUndefined()
    expect(sentRequest(fetchMock)).toEqual({
      url: closingObjectPath,
      method: 'POST',
      contentType: 'application/json',
      body: { reason: 'Marko still owes me for the taxi' },
    })
  })

  it('sends a JSON object body without a reason when there is no reason, because the API reads a body', async () => {
    const fetchMock = stubFetch(noContent())

    await objectToClosing(testGroupId)

    const request = sentRequest(fetchMock)
    expect(request.url).toBe(closingObjectPath)
    expect(request.method).toBe('POST')
    expect(request.contentType).toBe('application/json')
    expect(request.body).toEqual(expect.any(Object))
    expect((request.body as { reason?: unknown }).reason ?? null).toBeNull()
  })

  it.each([
    ['too long a reason', 400, 'CLOSING_REASON_TOO_LONG'],
    ['not being closed any more', 400, 'GROUP_NOT_CLOSING'],
    ['asked of the owner', 403, 'CLOSING_OWNER_CANNOT_ANSWER'],
  ])('rethrows the ApiError unchanged for %s', async (_name, httpStatus, errorCode) => {
    stubFetch(problemResponse(httpStatus, errorCode))

    const error = await captureError(objectToClosing(testGroupId, 'No'))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus, errorCode })
  })
})

describe('unlockGroup', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('sends POST /api/groups/{id}/unlock with no body and resolves on 204', async () => {
    const fetchMock = stubFetch(noContent())

    const result = await unlockGroup(testGroupId)

    expect(result).toBeUndefined()
    expect(sentRequest(fetchMock)).toEqual({
      url: unlockPath,
      method: 'POST',
      contentType: null,
      body: undefined,
    })
  })

  it.each([
    ['not finished', 400, 'GROUP_NOT_FINISHED'],
    ['not the owner', 403, 'GROUP_NOT_OWNER'],
  ])('rethrows the ApiError unchanged when the group is %s', async (_name, httpStatus, errorCode) => {
    stubFetch(problemResponse(httpStatus, errorCode))

    const error = await captureError(unlockGroup(testGroupId))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus, errorCode })
  })
})
