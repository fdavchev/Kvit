import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '@/core/api/apiClient'
import {
  captureError,
  problemResponse,
  sentRequest,
  stubFetch,
} from '@/test/apiTestHelpers'
import {
  dinnerDeletedRow,
  emptyGroupList,
  flatGroupRow,
  greeceGroupRow,
  groupListOf,
  summerFinishedRow,
  testGroup,
  testGroupId,
} from '@/test/groupTestData'
import {
  addMember,
  createGroup,
  deleteGroup,
  getGroup,
  getGroups,
  leaveGroup,
  restoreGroup,
  updateGroup,
} from './groupsService'

const groupInput = { name: 'Greece trip', emoji: testGroup.emoji, currency: 'MKD' } as const

const fullList = groupListOf({
  groups: [greeceGroupRow, flatGroupRow],
  finishedGroups: [summerFinishedRow],
  recentlyDeleted: [dinnerDeletedRow],
})

const groupFields = Object.keys(testGroup)
const rowFields = Object.keys(greeceGroupRow)
const deletedRowFields = Object.keys(dinnerDeletedRow)
const listFields = Object.keys(emptyGroupList)

function withoutField(source: object, field: string): object {
  return Object.fromEntries(Object.entries(source).filter(([name]) => name !== field))
}

describe('getGroups', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('asks GET /api/groups and returns the three lists', async () => {
    const fetchMock = stubFetch(Response.json(fullList))

    const list = await getGroups()

    const request = sentRequest(fetchMock)
    expect(request.url).toBe('/api/groups')
    expect(request.method ?? 'GET').toBe('GET')
    expect(list).toEqual(fullList)
  })

  it('returns three empty lists when the person has no groups', async () => {
    stubFetch(Response.json(emptyGroupList))

    const list = await getGroups()

    expect(list).toEqual(emptyGroupList)
  })

  it.each([
    ['a server error', new Response(null, { status: 500 }), 500, null],
    ['a signed-out answer', problemResponse(401, 'AUTH_NOT_SIGNED_IN'), 401, 'AUTH_NOT_SIGNED_IN'],
  ])('rethrows the ApiError unchanged for %s', async (_name, response, httpStatus, errorCode) => {
    stubFetch(response)

    const error = await captureError(getGroups())

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus, errorCode })
  })

  it('rethrows the ApiError unchanged when the network fails', async () => {
    stubFetch(new TypeError('Failed to fetch'))

    const error = await captureError(getGroups())

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus: null })
  })

  it.each(listFields)('throws an error naming "%s" when the answer lacks it', async (field) => {
    stubFetch(Response.json(withoutField(fullList, field)))

    await expect(getGroups()).rejects.toThrow(field)
  })

  it.each(listFields)('throws an error naming "%s" when it is not a list', async (field) => {
    stubFetch(Response.json({ ...fullList, [field]: 'Greece trip' }))

    await expect(getGroups()).rejects.toThrow(field)
  })

  it.each(rowFields)('throws an error naming "%s" when an open group lacks it', async (field) => {
    stubFetch(Response.json({ ...emptyGroupList, groups: [withoutField(greeceGroupRow, field)] }))

    await expect(getGroups()).rejects.toThrow(field)
  })

  it.each(rowFields)('throws an error naming "%s" when a finished group lacks it', async (field) => {
    stubFetch(
      Response.json({ ...emptyGroupList, finishedGroups: [withoutField(summerFinishedRow, field)] }),
    )

    await expect(getGroups()).rejects.toThrow(field)
  })

  it.each(deletedRowFields)('throws an error naming "%s" when a deleted group lacks it', async (field) => {
    stubFetch(
      Response.json({ ...emptyGroupList, recentlyDeleted: [withoutField(dinnerDeletedRow, field)] }),
    )

    await expect(getGroups()).rejects.toThrow(field)
  })

  it.each([
    ['id', 42],
    ['name', null],
    ['emoji', 7],
    ['defaultCurrency', 'USD'],
    ['defaultCurrency', 'mkd'],
    ['memberCount', '3'],
    ['memberCount', null],
  ])('throws an error naming "%s" when its value in an open group is %j', async (field, value) => {
    stubFetch(Response.json({ ...emptyGroupList, groups: [{ ...greeceGroupRow, [field]: value }] }))

    await expect(getGroups()).rejects.toThrow(field)
  })

  it.each([
    ['deletedAt', 5],
    ['restorableUntil', null],
    ['restorableUntil', 'next month'],
  ])('throws an error naming "%s" when its value in a deleted group is %j', async (field, value) => {
    stubFetch(
      Response.json({ ...emptyGroupList, recentlyDeleted: [{ ...dinnerDeletedRow, [field]: value }] }),
    )

    await expect(getGroups()).rejects.toThrow(field)
  })

  it.each([
    ['null', null],
    ['a text', 'Healthy'],
  ])('throws an error saying an object was expected when the answer is %s', async (_name, body) => {
    stubFetch(Response.json(body))

    await expect(getGroups()).rejects.toThrow(/object/)
  })
})

describe('getGroup', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('asks GET /api/groups/{id} and returns the group', async () => {
    const fetchMock = stubFetch(Response.json(testGroup))

    const group = await getGroup(testGroupId)

    const request = sentRequest(fetchMock)
    expect(request.url).toBe(`/api/groups/${testGroupId}`)
    expect(request.method ?? 'GET').toBe('GET')
    expect(group).toEqual(testGroup)
  })

  it.each([
    ['kind', 'OneBill'],
    ['status', 'Closing'],
    ['status', 'Finished'],
    ['defaultCurrency', 'EUR'],
    ['isOwner', false],
  ])('accepts %s with the value %j', async (field, value) => {
    stubFetch(Response.json({ ...testGroup, [field]: value }))

    const group = await getGroup(testGroupId)

    expect(group).toMatchObject({ [field]: value })
  })

  it('rethrows the ApiError unchanged when the group is not found', async () => {
    stubFetch(problemResponse(404, 'GROUP_NOT_FOUND'))

    const error = await captureError(getGroup(testGroupId))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus: 404, errorCode: 'GROUP_NOT_FOUND' })
  })

  it.each(groupFields)('throws an error naming "%s" when the answer lacks it', async (field) => {
    stubFetch(Response.json(withoutField(testGroup, field)))

    await expect(getGroup(testGroupId)).rejects.toThrow(field)
  })

  it.each([
    ['id', 42],
    ['kind', 'Other'],
    ['kind', null],
    ['name', null],
    ['emoji', 5],
    ['defaultCurrency', 'USD'],
    ['status', 'Weird'],
    ['status', 2],
    ['ownerUserId', 7],
    ['isOwner', 'yes'],
    ['isOwner', 1],
    ['memberCount', '3'],
    ['memberCount', null],
    ['inviteToken', null],
    ['inviteToken', 12],
  ])('throws an error naming "%s" when its value is %j', async (field, value) => {
    stubFetch(Response.json({ ...testGroup, [field]: value }))

    await expect(getGroup(testGroupId)).rejects.toThrow(field)
  })

  it.each([
    ['null', null],
    ['a text', 'Greece trip'],
  ])('throws an error saying an object was expected when the answer is %s', async (_name, body) => {
    stubFetch(Response.json(body))

    await expect(getGroup(testGroupId)).rejects.toThrow(/object/)
  })
})

describe('createGroup', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('sends POST /api/groups with the name, emoji and currency as JSON and returns the new group', async () => {
    const fetchMock = stubFetch(Response.json(testGroup))

    const group = await createGroup(groupInput)

    expect(group).toEqual(testGroup)
    expect(sentRequest(fetchMock)).toEqual({
      url: '/api/groups',
      method: 'POST',
      contentType: 'application/json',
      body: groupInput,
    })
  })

  it('rethrows the ApiError unchanged when the server refuses the name', async () => {
    stubFetch(problemResponse(400, 'GROUP_NAME_INVALID'))

    const error = await captureError(createGroup(groupInput))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus: 400, errorCode: 'GROUP_NAME_INVALID' })
  })

  it('throws an error naming "id" when the answer lacks the id of the new group', async () => {
    stubFetch(Response.json(withoutField(testGroup, 'id')))

    await expect(createGroup(groupInput)).rejects.toThrow('id')
  })
})

describe('updateGroup', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('sends PUT /api/groups/{id} with the name, emoji and currency as JSON and resolves on 204', async () => {
    const fetchMock = stubFetch(new Response(null, { status: 204 }))
    const input = { name: 'Flat 4B', emoji: '\u{1F3E0}', currency: 'EUR' } as const

    const result = await updateGroup(testGroupId, input)

    expect(result).toBeUndefined()
    expect(sentRequest(fetchMock)).toEqual({
      url: `/api/groups/${testGroupId}`,
      method: 'PUT',
      contentType: 'application/json',
      body: input,
    })
  })

  it('rethrows the ApiError unchanged when the person is not the owner', async () => {
    stubFetch(problemResponse(403, 'GROUP_NOT_OWNER'))

    const error = await captureError(updateGroup(testGroupId, groupInput))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus: 403, errorCode: 'GROUP_NOT_OWNER' })
  })
})

describe('deleteGroup', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('sends DELETE /api/groups/{id} with no body and resolves on 204', async () => {
    const fetchMock = stubFetch(new Response(null, { status: 204 }))

    const result = await deleteGroup(testGroupId)

    expect(result).toBeUndefined()
    expect(sentRequest(fetchMock)).toEqual({
      url: `/api/groups/${testGroupId}`,
      method: 'DELETE',
      contentType: null,
      body: undefined,
    })
  })

  it('rethrows the ApiError unchanged when the person is not the owner', async () => {
    stubFetch(problemResponse(403, 'GROUP_NOT_OWNER'))

    const error = await captureError(deleteGroup(testGroupId))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus: 403, errorCode: 'GROUP_NOT_OWNER' })
  })
})

describe('restoreGroup', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('sends POST /api/groups/{id}/restore with no body and resolves on 204', async () => {
    const fetchMock = stubFetch(new Response(null, { status: 204 }))

    const result = await restoreGroup(testGroupId)

    expect(result).toBeUndefined()
    expect(sentRequest(fetchMock)).toEqual({
      url: `/api/groups/${testGroupId}/restore`,
      method: 'POST',
      contentType: null,
      body: undefined,
    })
  })

  it('rethrows the ApiError unchanged when the restore time has passed', async () => {
    stubFetch(problemResponse(400, 'GROUP_RESTORE_EXPIRED'))

    const error = await captureError(restoreGroup(testGroupId))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus: 400, errorCode: 'GROUP_RESTORE_EXPIRED' })
  })
})

describe('leaveGroup', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('sends POST /api/groups/{id}/leave with no body and resolves on 204', async () => {
    const fetchMock = stubFetch(new Response(null, { status: 204 }))

    const result = await leaveGroup(testGroupId)

    expect(result).toBeUndefined()
    expect(sentRequest(fetchMock)).toEqual({
      url: `/api/groups/${testGroupId}/leave`,
      method: 'POST',
      contentType: null,
      body: undefined,
    })
  })

  it('rethrows the ApiError unchanged when the owner tries to leave', async () => {
    stubFetch(problemResponse(400, 'MEMBER_OWNER_CANNOT_LEAVE'))

    const error = await captureError(leaveGroup(testGroupId))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus: 400, errorCode: 'MEMBER_OWNER_CANNOT_LEAVE' })
  })
})

describe('addMember', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('sends POST /api/groups/{id}/members with the name as JSON and resolves with nothing', async () => {
    const fetchMock = stubFetch(
      Response.json({
        id: '1f2e3d4c-5b6a-4789-8a9b-0c1d2e3f4a5b',
        name: 'Grandma',
        displayName: 'Grandma',
        userId: null,
        isOwner: false,
        isYou: false,
        isNameOnly: true,
        claimedName: null,
      }),
    )

    const result = await addMember(testGroupId, 'Grandma')

    expect(result).toBeUndefined()
    expect(sentRequest(fetchMock)).toEqual({
      url: `/api/groups/${testGroupId}/members`,
      method: 'POST',
      contentType: 'application/json',
      body: { name: 'Grandma' },
    })
  })

  it('rethrows the ApiError unchanged when the name is already in the group', async () => {
    stubFetch(problemResponse(400, 'MEMBER_NAME_TAKEN'))

    const error = await captureError(addMember(testGroupId, 'Marko'))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus: 400, errorCode: 'MEMBER_NAME_TAKEN' })
  })
})
