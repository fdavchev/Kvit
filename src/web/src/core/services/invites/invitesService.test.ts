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
  alreadyMemberInvitePreview,
  openInvitePreview,
  openInvitePreviewWithNames,
  removedInvitePreview,
} from '@/test/inviteTestData'
import { joinGroup, previewInvite } from './invitesService'

const previewFields = Object.keys(openInvitePreview)
const unclaimedFields = Object.keys(openInvitePreviewWithNames.unclaimedNames[0])
const claimMemberId = openInvitePreviewWithNames.unclaimedNames[0].id

function withoutField(source: object, field: string): object {
  return Object.fromEntries(Object.entries(source).filter(([name]) => name !== field))
}

describe('previewInvite', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('sends POST /api/invites/preview with the token in a JSON body and never in the address', async () => {
    const fetchMock = stubFetch(Response.json(openInvitePreview))

    await previewInvite(testInviteToken)

    const request = sentRequest(fetchMock)
    expect(request).toEqual({
      url: '/api/invites/preview',
      method: 'POST',
      contentType: 'application/json',
      body: { token: testInviteToken },
    })
    expect(request.url).not.toContain(testInviteToken)
  })

  it.each([
    ['an open invite', openInvitePreview],
    ['an open invite with names to claim', openInvitePreviewWithNames],
    ['an invite to a group the person is already in', alreadyMemberInvitePreview],
    ['an invite to a group the person was removed from', removedInvitePreview],
  ])('returns the answer for %s', async (_name, preview) => {
    stubFetch(Response.json(preview))

    const result = await previewInvite(testInviteToken)

    expect(result).toEqual(preview)
  })

  it('keeps the group id as null for an open invite and as the id when the person is already in', async () => {
    stubFetch(Response.json(openInvitePreview), Response.json(alreadyMemberInvitePreview))

    const open = await previewInvite(testInviteToken)
    const alreadyIn = await previewInvite(testInviteToken)

    expect(open.groupId).toBeNull()
    expect(alreadyIn.groupId).toBe(testGroupId)
  })

  it.each([
    ['an unknown invite link', problemResponse(404, 'INVITE_NOT_FOUND'), 404, 'INVITE_NOT_FOUND'],
    ['too many tries', problemResponse(429, 'RATE_LIMITED'), 429, 'RATE_LIMITED'],
    ['a server error', new Response(null, { status: 500 }), 500, null],
  ])('rethrows the ApiError unchanged for %s', async (_name, response, httpStatus, errorCode) => {
    stubFetch(response)

    const error = await captureError(previewInvite(testInviteToken))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus, errorCode })
  })

  it('rethrows the ApiError unchanged when the network fails', async () => {
    stubFetch(new TypeError('Failed to fetch'))

    const error = await captureError(previewInvite(testInviteToken))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus: null })
  })

  it.each(previewFields)('throws an error naming "%s" when the answer lacks it', async (field) => {
    stubFetch(Response.json(withoutField(openInvitePreviewWithNames, field)))

    await expect(previewInvite(testInviteToken)).rejects.toThrow(field)
  })

  it.each([
    ['status', 'Closed'],
    ['status', 'open'],
    ['status', null],
    ['status', 1],
    ['groupId', 5],
    ['groupId', false],
    ['name', null],
    ['name', 12],
    ['emoji', 7],
    ['emoji', null],
    ['memberNames', 'Filip'],
    ['memberNames', null],
    ['memberNames', ['Filip', 3]],
    ['unclaimedNames', 'Marko'],
    ['unclaimedNames', null],
  ])('throws an error naming "%s" when its value is %j', async (field, value) => {
    stubFetch(Response.json({ ...openInvitePreviewWithNames, [field]: value }))

    await expect(previewInvite(testInviteToken)).rejects.toThrow(field)
  })

  it.each(unclaimedFields)('throws an error naming "%s" when a name to claim lacks it', async (field) => {
    stubFetch(
      Response.json({
        ...openInvitePreviewWithNames,
        unclaimedNames: [withoutField(openInvitePreviewWithNames.unclaimedNames[0], field)],
      }),
    )

    await expect(previewInvite(testInviteToken)).rejects.toThrow(field)
  })

  it.each([
    ['id', 9],
    ['name', null],
  ])('throws an error naming "%s" when its value in a name to claim is %j', async (field, value) => {
    stubFetch(
      Response.json({
        ...openInvitePreviewWithNames,
        unclaimedNames: [{ ...openInvitePreviewWithNames.unclaimedNames[0], [field]: value }],
      }),
    )

    await expect(previewInvite(testInviteToken)).rejects.toThrow(field)
  })

  it.each([
    ['null', null],
    ['a text', 'Open'],
    ['a list', []],
  ])('throws an error saying an object was expected when the answer is %s', async (_name, body) => {
    stubFetch(Response.json(body))

    await expect(previewInvite(testInviteToken)).rejects.toThrow(/object/)
  })
})

describe('joinGroup', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('sends POST /api/invites/join with only the token in a JSON body and returns the group id', async () => {
    const fetchMock = stubFetch(Response.json({ groupId: testGroupId }))

    const groupId = await joinGroup(testInviteToken)

    expect(groupId).toBe(testGroupId)
    const request = sentRequest(fetchMock)
    expect(request).toEqual({
      url: '/api/invites/join',
      method: 'POST',
      contentType: 'application/json',
      body: { token: testInviteToken },
    })
    expect(request.url).not.toContain(testInviteToken)
  })

  it('leaves claimMemberId out of the body when no name is claimed', async () => {
    const fetchMock = stubFetch(Response.json({ groupId: testGroupId }))

    await joinGroup(testInviteToken)

    expect(Object.keys(sentRequest(fetchMock).body as object)).toEqual(['token'])
  })

  it('sends the claimed member id with the token when a name is claimed', async () => {
    const fetchMock = stubFetch(Response.json({ groupId: testGroupId }))

    const groupId = await joinGroup(testInviteToken, claimMemberId)

    expect(groupId).toBe(testGroupId)
    expect(sentRequest(fetchMock).body).toEqual({ token: testInviteToken, claimMemberId })
  })

  it.each([
    ['an unknown invite link', problemResponse(404, 'INVITE_NOT_FOUND'), 404, 'INVITE_NOT_FOUND'],
    ['a removed person', problemResponse(403, 'INVITE_REMOVED'), 403, 'INVITE_REMOVED'],
    ['a name that cannot be claimed', problemResponse(400, 'MEMBER_CANNOT_CLAIM'), 400, 'MEMBER_CANNOT_CLAIM'],
    ['a name that is gone', problemResponse(404, 'MEMBER_NOT_FOUND'), 404, 'MEMBER_NOT_FOUND'],
    ['too many tries', problemResponse(429, 'RATE_LIMITED'), 429, 'RATE_LIMITED'],
    ['a signed-out visitor', problemResponse(401, 'AUTH_NOT_SIGNED_IN'), 401, 'AUTH_NOT_SIGNED_IN'],
  ])('rethrows the ApiError unchanged for %s', async (_name, response, httpStatus, errorCode) => {
    stubFetch(response)

    const error = await captureError(joinGroup(testInviteToken))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus, errorCode })
  })

  it('rethrows the ApiError unchanged when the network fails', async () => {
    stubFetch(new TypeError('Failed to fetch'))

    const error = await captureError(joinGroup(testInviteToken))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus: null })
  })

  it('throws an error naming "groupId" when the answer lacks it', async () => {
    stubFetch(Response.json({}))

    await expect(joinGroup(testInviteToken)).rejects.toThrow('groupId')
  })

  it.each([
    ['a number', 4],
    ['null', null],
  ])('throws an error naming "groupId" when its value is %s', async (_name, groupId) => {
    stubFetch(Response.json({ groupId }))

    await expect(joinGroup(testInviteToken)).rejects.toThrow('groupId')
  })

  it.each([
    ['null', null],
    ['a text', testGroupId],
  ])('throws an error saying an object was expected when the answer is %s', async (_name, body) => {
    stubFetch(Response.json(body))

    await expect(joinGroup(testInviteToken)).rejects.toThrow(/object/)
  })
})
