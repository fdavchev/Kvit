import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '@/core/api/apiClient'
import { captureError, problemResponse, sentRequest, stubFetch } from '@/test/apiTestHelpers'
import {
  adminPerson,
  anaPerson,
  closedTripGroup,
  deletedPerson,
  guestPerson,
  logEntries,
  logEntry,
  suspendedPerson,
  tripGroup,
} from '@/test/ownerToolsTestData'
import {
  closeGroup,
  eraseName,
  findGroup,
  findPeople,
  loadLog,
  reopenGroup,
  suspendPerson,
  unsuspendPerson,
} from './ownerToolsService'

const personId = anaPerson.id
const groupId = tripGroup.id
const reason = 'Reported by three people'

function withoutField(source: object, field: string): object {
  return Object.fromEntries(Object.entries(source).filter(([name]) => name !== field))
}

const personFields = Object.keys(anaPerson)
const groupFields = Object.keys(tripGroup)
const logFields = Object.keys(logEntry)

describe('findPeople', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('asks GET /api/admin/tools/people with the query', async () => {
    const fetchMock = stubFetch(Response.json([anaPerson]))

    await findPeople('ana')

    const request = sentRequest(fetchMock)
    expect(request.url).toBe('/api/admin/tools/people?query=ana')
    expect(request.method ?? 'GET').toBe('GET')
  })

  it('returns every person of the answer with every field, in the order of the answer', async () => {
    const people = [anaPerson, suspendedPerson, guestPerson, adminPerson, deletedPerson]
    stubFetch(Response.json(people))

    expect(await findPeople('a')).toEqual(people)
  })

  it('keeps a null email for a guest', async () => {
    stubFetch(Response.json([guestPerson]))

    const [person] = await findPeople('mara')

    expect(person.email).toBeNull()
    expect(person.isGuest).toBe(true)
  })

  it('returns an empty list when nobody was found', async () => {
    stubFetch(Response.json([]))

    expect(await findPeople('zz')).toEqual([])
  })

  it('throws the ApiError with status 403 for a person who is not an admin', async () => {
    stubFetch(new Response(null, { status: 403 }))

    const error = await captureError(findPeople('ana'))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus: 403 })
  })

  it.each([
    ['null', null],
    ['an object', {}],
    ['a text', 'ana'],
  ])('throws an error saying a list was expected when the answer is %s', async (_name, body) => {
    stubFetch(Response.json(body))

    await expect(findPeople('ana')).rejects.toThrow('list')
  })

  it.each(personFields)('throws an error naming "%s" when a person lacks it', async (field) => {
    stubFetch(Response.json([withoutField(anaPerson, field)]))

    await expect(findPeople('ana')).rejects.toThrow(`"${field}"`)
  })

  it.each([
    ['id', 7],
    ['displayName', null],
    ['email', 5],
    ['email', true],
    ['isGuest', 'no'],
    ['isGuest', null],
    ['isSuspended', 1],
    ['isAdmin', 'true'],
    ['deletionPending', null],
    ['isDeleted', {}],
    ['createdAt', 20260901],
    ['createdAt', 'yesterday'],
    ['groupCount', '3'],
    ['groupCount', -1],
    ['groupCount', 1.5],
    ['groupCount', null],
  ])('throws an error naming "%s" when it is %j', async (field, value) => {
    stubFetch(Response.json([{ ...anaPerson, [field]: value }]))

    await expect(findPeople('ana')).rejects.toThrow(`"${field}"`)
  })

  it('throws an error when one person among several is broken', async () => {
    stubFetch(Response.json([anaPerson, { ...suspendedPerson, groupCount: 'x' }]))

    await expect(findPeople('a')).rejects.toThrow('"groupCount"')
  })
})

describe.each([
  ['suspendPerson', suspendPerson, `/api/admin/tools/people/${personId}/suspend`, personId],
  ['unsuspendPerson', unsuspendPerson, `/api/admin/tools/people/${personId}/unsuspend`, personId],
  ['eraseName', eraseName, `/api/admin/tools/people/${personId}/erase-name`, personId],
  ['closeGroup', closeGroup, `/api/admin/tools/groups/${groupId}/close`, groupId],
  ['reopenGroup', reopenGroup, `/api/admin/tools/groups/${groupId}/reopen`, groupId],
])('%s', (_name, act, path, id) => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('sends one POST with the reason as JSON and resolves on 204', async () => {
    const fetchMock = stubFetch(new Response(null, { status: 204 }))

    const result = await act(id, reason)

    expect(result).toBeUndefined()
    expect(fetchMock).toHaveBeenCalledOnce()
    expect(sentRequest(fetchMock)).toMatchObject({
      url: path,
      method: 'POST',
      contentType: 'application/json',
      body: { reason },
    })
  })

  it('sends the reason as it is given, so the screen decides what to trim', async () => {
    const fetchMock = stubFetch(new Response(null, { status: 204 }))

    await act(id, ' Spam ')

    expect(sentRequest(fetchMock).body).toEqual({ reason: ' Spam ' })
  })

  it.each([
    [400, 'OWNER_REASON_INVALID'],
    [403, 'OWNER_CANNOT_ACT_ON_ADMIN'],
    [404, 'OWNER_PERSON_NOT_FOUND'],
    [404, 'OWNER_GROUP_NOT_FOUND'],
  ])('throws the ApiError with its code when the server answers %i %s', async (status, errorCode) => {
    stubFetch(problemResponse(status, errorCode))

    const error = await captureError(act(id, reason))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus: status, errorCode })
  })

  it('throws the ApiError with status null when the network fails', async () => {
    stubFetch(new TypeError('Failed to fetch'))

    const error = await captureError(act(id, reason))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus: null })
  })
})

describe('findGroup', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('asks GET /api/admin/tools/groups/{id}', async () => {
    const fetchMock = stubFetch(Response.json(tripGroup))

    await findGroup(groupId)

    const request = sentRequest(fetchMock)
    expect(request.url).toBe(`/api/admin/tools/groups/${groupId}`)
    expect(request.method ?? 'GET').toBe('GET')
  })

  it('returns every field of the answer', async () => {
    stubFetch(Response.json(tripGroup))

    expect(await findGroup(groupId)).toEqual(tripGroup)
  })

  it('keeps the time at which the owner of Kvit closed the group', async () => {
    stubFetch(Response.json(closedTripGroup))

    const group = await findGroup(groupId)

    expect(group.closedByAdminAt).toBe(closedTripGroup.closedByAdminAt)
  })

  it.each(['Open', 'Closing', 'Finished'])('accepts the status %s', async (status) => {
    stubFetch(Response.json({ ...tripGroup, status }))

    expect(await findGroup(groupId)).toMatchObject({ status })
  })

  it('keeps isDeleted true for a deleted group', async () => {
    stubFetch(Response.json({ ...tripGroup, isDeleted: true }))

    expect(await findGroup(groupId)).toMatchObject({ isDeleted: true })
  })

  it('throws the ApiError with its code when the group does not exist', async () => {
    stubFetch(problemResponse(404, 'OWNER_GROUP_NOT_FOUND'))

    const error = await captureError(findGroup(groupId))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus: 404, errorCode: 'OWNER_GROUP_NOT_FOUND' })
  })

  it('throws an error saying an object was expected when the answer is null', async () => {
    stubFetch(Response.json(null))

    await expect(findGroup(groupId)).rejects.toThrow('JSON object')
  })

  it.each(groupFields)('throws an error naming "%s" when the answer lacks it', async (field) => {
    stubFetch(Response.json(withoutField(tripGroup, field)))

    await expect(findGroup(groupId)).rejects.toThrow(`"${field}"`)
  })

  it.each([
    ['id', 7],
    ['name', null],
    ['emoji', 5],
    ['status', 'Archived'],
    ['status', 'open'],
    ['status', 2],
    ['memberCount', '4'],
    ['memberCount', -1],
    ['ownerName', null],
    ['createdAt', 'yesterday'],
    ['isDeleted', 'no'],
    ['closedByAdminAt', 5],
    ['closedByAdminAt', 'yesterday'],
    ['closedByAdminAt', true],
  ])('throws an error naming "%s" when it is %j', async (field, value) => {
    stubFetch(Response.json({ ...tripGroup, [field]: value }))

    await expect(findGroup(groupId)).rejects.toThrow(`"${field}"`)
  })
})

describe('loadLog', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('asks GET /api/admin/tools/log with a limit of 50', async () => {
    const fetchMock = stubFetch(Response.json(logEntries))

    await loadLog()

    const request = sentRequest(fetchMock)
    expect(request.url).toBe('/api/admin/tools/log?limit=50')
    expect(request.method ?? 'GET').toBe('GET')
  })

  it('returns every entry with every field, newest first as the server sent them', async () => {
    stubFetch(Response.json(logEntries))

    expect(await loadLog()).toEqual(logEntries)
  })

  it('returns an empty list when nothing was done yet', async () => {
    stubFetch(Response.json([]))

    expect(await loadLog()).toEqual([])
  })

  it.each(['Suspended', 'Unsuspended', 'GroupClosed', 'GroupReopened', 'NameErased'])('accepts the action %s', async (action) => {
    stubFetch(Response.json([{ ...logEntry, action }]))

    expect(await loadLog()).toEqual([{ ...logEntry, action }])
  })

  it.each(['person', 'group'])('accepts the target kind %s', async (targetKind) => {
    stubFetch(Response.json([{ ...logEntry, targetKind }]))

    expect(await loadLog()).toEqual([{ ...logEntry, targetKind }])
  })

  it('throws the ApiError with status 403 for a person who is not an admin', async () => {
    stubFetch(new Response(null, { status: 403 }))

    const error = await captureError(loadLog())

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus: 403 })
  })

  it.each([
    ['null', null],
    ['an object', {}],
  ])('throws an error saying a list was expected when the answer is %s', async (_name, body) => {
    stubFetch(Response.json(body))

    await expect(loadLog()).rejects.toThrow('list')
  })

  it.each(logFields)('throws an error naming "%s" when an entry lacks it', async (field) => {
    stubFetch(Response.json([withoutField(logEntry, field)]))

    await expect(loadLog()).rejects.toThrow(`"${field}"`)
  })

  it.each([
    ['id', 3],
    ['createdAt', 'yesterday'],
    ['createdAt', 20261104],
    ['adminName', null],
    ['action', 'Deleted'],
    ['action', 'suspended'],
    ['action', 1],
    ['targetKind', 'user'],
    ['targetKind', null],
    ['targetLabel', 7],
    ['reason', null],
    ['reason', 5],
  ])('throws an error naming "%s" when it is %j', async (field, value) => {
    stubFetch(Response.json([{ ...logEntry, [field]: value }]))

    await expect(loadLog()).rejects.toThrow(`"${field}"`)
  })
})
