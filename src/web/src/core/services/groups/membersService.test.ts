import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '@/core/api/apiClient'
import {
  captureError,
  problemResponse,
  sentRequest,
  stubFetch,
} from '@/test/apiTestHelpers'
import { testGroupId, testInviteToken } from '@/test/groupTestData'
import {
  anaMember,
  bojanRemoved,
  markoMember,
  ownerViewMembers,
  petarMember,
} from '@/test/memberTestData'
import {
  claimName,
  getMembers,
  letBackIn,
  makeOwner,
  removeMember,
  resetInviteLink,
  undoClaim,
  undoResetInviteLink,
} from './membersService'

const memberId = anaMember.id
const membersPath = `/api/groups/${testGroupId}/members`
const memberPath = `${membersPath}/${memberId}`
const newInviteToken = 'Zt7-newTokenFromTheServer_0123456789abcdefghijk'

const listFields = Object.keys(ownerViewMembers)
const memberFields = Object.keys(anaMember)
const removedFields = Object.keys(bojanRemoved)

function withoutField(source: object, field: string): object {
  return Object.fromEntries(Object.entries(source).filter(([name]) => name !== field))
}

function noContent(): Response {
  return new Response(null, { status: 204 })
}

describe('getMembers', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('asks GET /api/groups/{id}/members and returns the members, the removed people and canClaimNames', async () => {
    const fetchMock = stubFetch(Response.json(ownerViewMembers))

    const members = await getMembers(testGroupId)

    const request = sentRequest(fetchMock)
    expect(request.url).toBe(membersPath)
    expect(request.method ?? 'GET').toBe('GET')
    expect(members).toEqual(ownerViewMembers)
  })

  it('keeps null for the account and the claimed name of a plain name', async () => {
    stubFetch(Response.json({ ...ownerViewMembers, members: [markoMember], removed: [] }))

    const members = await getMembers(testGroupId)

    expect(members.members[0]).toMatchObject({ userId: null, claimedName: null, isNameOnly: true })
  })

  it('keeps the claimed name of a person who took a name', async () => {
    stubFetch(Response.json({ ...ownerViewMembers, members: [petarMember], removed: [] }))

    const members = await getMembers(testGroupId)

    expect(members.members[0]).toMatchObject({ displayName: 'Petar', claimedName: 'Darko' })
  })

  it('returns two empty lists when the group has nobody and nobody was removed', async () => {
    stubFetch(Response.json({ members: [], removed: [], canClaimNames: false }))

    const members = await getMembers(testGroupId)

    expect(members).toEqual({ members: [], removed: [], canClaimNames: false })
  })

  it.each([
    ['a server error', new Response(null, { status: 500 }), 500, null],
    ['a group that is not found', problemResponse(404, 'GROUP_NOT_FOUND'), 404, 'GROUP_NOT_FOUND'],
  ])('rethrows the ApiError unchanged for %s', async (_name, response, httpStatus, errorCode) => {
    stubFetch(response)

    const error = await captureError(getMembers(testGroupId))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus, errorCode })
  })

  it('rethrows the ApiError unchanged when the network fails', async () => {
    stubFetch(new TypeError('Failed to fetch'))

    const error = await captureError(getMembers(testGroupId))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus: null })
  })

  it.each(listFields)('throws an error naming "%s" when the answer lacks it', async (field) => {
    stubFetch(Response.json(withoutField(ownerViewMembers, field)))

    await expect(getMembers(testGroupId)).rejects.toThrow(field)
  })

  it.each([
    ['members', 'Ana'],
    ['members', null],
    ['removed', 'Bojan'],
    ['removed', {}],
    ['canClaimNames', 'yes'],
    ['canClaimNames', 1],
    ['canClaimNames', null],
  ])('throws an error naming "%s" when its value is %j', async (field, value) => {
    stubFetch(Response.json({ ...ownerViewMembers, [field]: value }))

    await expect(getMembers(testGroupId)).rejects.toThrow(field)
  })

  it.each(memberFields)('throws an error naming "%s" when a member lacks it', async (field) => {
    stubFetch(
      Response.json({ ...ownerViewMembers, members: [withoutField(anaMember, field)] }),
    )

    await expect(getMembers(testGroupId)).rejects.toThrow(field)
  })

  it.each([
    ['id', 42],
    ['name', null],
    ['displayName', 7],
    ['userId', 5],
    ['userId', true],
    ['isOwner', 'no'],
    ['isOwner', null],
    ['isYou', 0],
    ['isNameOnly', 'false'],
    ['claimedName', 3],
    ['claimedName', false],
  ])('throws an error naming "%s" when its value in a member is %j', async (field, value) => {
    stubFetch(
      Response.json({ ...ownerViewMembers, members: [{ ...anaMember, [field]: value }] }),
    )

    await expect(getMembers(testGroupId)).rejects.toThrow(field)
  })

  it.each(removedFields)('throws an error naming "%s" when a removed person lacks it', async (field) => {
    stubFetch(
      Response.json({ ...ownerViewMembers, removed: [withoutField(bojanRemoved, field)] }),
    )

    await expect(getMembers(testGroupId)).rejects.toThrow(field)
  })

  it.each([
    ['id', 42],
    ['displayName', null],
  ])('throws an error naming "%s" when its value in a removed person is %j', async (field, value) => {
    stubFetch(
      Response.json({ ...ownerViewMembers, removed: [{ ...bojanRemoved, [field]: value }] }),
    )

    await expect(getMembers(testGroupId)).rejects.toThrow(field)
  })

  it.each([
    ['null', null],
    ['a text', 'Greece trip'],
    ['a list', []],
  ])('throws an error saying an object was expected when the answer is %s', async (_name, body) => {
    stubFetch(Response.json(body))

    await expect(getMembers(testGroupId)).rejects.toThrow(/object/)
  })
})

describe('removeMember', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('sends DELETE /api/groups/{id}/members/{memberId} with no body and resolves on 204', async () => {
    const fetchMock = stubFetch(noContent())

    const result = await removeMember(testGroupId, memberId)

    expect(result).toBeUndefined()
    expect(sentRequest(fetchMock)).toEqual({
      url: memberPath,
      method: 'DELETE',
      contentType: null,
      body: undefined,
    })
  })

  it.each([
    [400, 'MEMBER_IS_OWNER'],
    [403, 'GROUP_NOT_OWNER'],
    [404, 'MEMBER_NOT_FOUND'],
  ])('rethrows the ApiError unchanged for %i %s', async (httpStatus, errorCode) => {
    stubFetch(problemResponse(httpStatus, errorCode))

    const error = await captureError(removeMember(testGroupId, memberId))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus, errorCode })
  })
})

describe('letBackIn', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('sends POST /api/groups/{id}/members/{memberId}/let-back-in with no body and resolves on 204', async () => {
    const fetchMock = stubFetch(noContent())

    const result = await letBackIn(testGroupId, memberId)

    expect(result).toBeUndefined()
    expect(sentRequest(fetchMock)).toEqual({
      url: `${memberPath}/let-back-in`,
      method: 'POST',
      contentType: null,
      body: undefined,
    })
  })

  it('rethrows the ApiError unchanged when the name is now taken', async () => {
    stubFetch(problemResponse(400, 'MEMBER_NAME_TAKEN'))

    const error = await captureError(letBackIn(testGroupId, memberId))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus: 400, errorCode: 'MEMBER_NAME_TAKEN' })
  })
})

describe('makeOwner', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('sends POST /api/groups/{id}/owner with the member id as JSON and resolves on 204', async () => {
    const fetchMock = stubFetch(noContent())

    const result = await makeOwner(testGroupId, memberId)

    expect(result).toBeUndefined()
    expect(sentRequest(fetchMock)).toEqual({
      url: `/api/groups/${testGroupId}/owner`,
      method: 'POST',
      contentType: 'application/json',
      body: { memberId },
    })
  })

  it.each([
    [400, 'MEMBER_NOT_ACCOUNT'],
    [400, 'MEMBER_ALREADY_OWNER'],
    [403, 'GROUP_NOT_OWNER'],
  ])('rethrows the ApiError unchanged for %i %s', async (httpStatus, errorCode) => {
    stubFetch(problemResponse(httpStatus, errorCode))

    const error = await captureError(makeOwner(testGroupId, memberId))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus, errorCode })
  })
})

describe('claimName', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('sends POST /api/groups/{id}/members/{memberId}/claim with no body and resolves on 204', async () => {
    const fetchMock = stubFetch(noContent())

    const result = await claimName(testGroupId, markoMember.id)

    expect(result).toBeUndefined()
    expect(sentRequest(fetchMock)).toEqual({
      url: `${membersPath}/${markoMember.id}/claim`,
      method: 'POST',
      contentType: null,
      body: undefined,
    })
  })

  it('rethrows the ApiError unchanged when the person cannot take the name', async () => {
    stubFetch(problemResponse(400, 'MEMBER_CANNOT_CLAIM'))

    const error = await captureError(claimName(testGroupId, markoMember.id))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus: 400, errorCode: 'MEMBER_CANNOT_CLAIM' })
  })
})

describe('undoClaim', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('sends POST /api/groups/{id}/members/{memberId}/undo-claim with no body and resolves on 204', async () => {
    const fetchMock = stubFetch(noContent())

    const result = await undoClaim(testGroupId, petarMember.id)

    expect(result).toBeUndefined()
    expect(sentRequest(fetchMock)).toEqual({
      url: `${membersPath}/${petarMember.id}/undo-claim`,
      method: 'POST',
      contentType: null,
      body: undefined,
    })
  })

  it('rethrows the ApiError unchanged when the name was not taken by anyone', async () => {
    stubFetch(problemResponse(400, 'MEMBER_NOT_CLAIMED'))

    const error = await captureError(undoClaim(testGroupId, petarMember.id))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus: 400, errorCode: 'MEMBER_NOT_CLAIMED' })
  })
})

describe.each([
  ['resetInviteLink', resetInviteLink, '/invite/reset', 'INVITE_NOT_FOUND'],
  ['undoResetInviteLink', undoResetInviteLink, '/invite/undo-reset', 'INVITE_NOTHING_TO_UNDO'],
] as const)('%s', (_name, changeLink, pathEnd, refusalCode) => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it(`sends POST /api/groups/{id}${pathEnd} with no body and returns the invite token`, async () => {
    const fetchMock = stubFetch(Response.json({ inviteToken: newInviteToken }))

    const token = await changeLink(testGroupId)

    expect(token).toBe(newInviteToken)
    expect(sentRequest(fetchMock)).toEqual({
      url: `/api/groups/${testGroupId}${pathEnd}`,
      method: 'POST',
      contentType: null,
      body: undefined,
    })
  })

  it('rethrows the ApiError unchanged when the server refuses', async () => {
    stubFetch(problemResponse(400, refusalCode))

    const error = await captureError(changeLink(testGroupId))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus: 400, errorCode: refusalCode })
  })

  it('throws an error naming "inviteToken" when the answer lacks it', async () => {
    stubFetch(Response.json({}))

    await expect(changeLink(testGroupId)).rejects.toThrow('inviteToken')
  })

  it.each([
    ['a number', 12],
    ['null', null],
    ['a list', [testInviteToken]],
  ])('throws an error naming "inviteToken" when its value is %s', async (_value, inviteToken) => {
    stubFetch(Response.json({ inviteToken }))

    await expect(changeLink(testGroupId)).rejects.toThrow('inviteToken')
  })

  it('throws an error saying an object was expected when the answer is a bare text', async () => {
    stubFetch(Response.json(testInviteToken))

    await expect(changeLink(testGroupId)).rejects.toThrow(/object/)
  })
})
