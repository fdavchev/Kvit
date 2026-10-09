import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '@/core/api/apiClient'
import {
  captureError,
  problemResponse,
  sentRequest,
  stubFetch,
} from '@/test/apiTestHelpers'
import {
  activityEventOf,
  editedEventOf,
  expenseAddedEvent,
  type ActivityEventJson,
} from '@/test/activityTestData'
import { testGroupId } from '@/test/groupTestData'
import { getActivity } from './activityService'

const addedEvent: ActivityEventJson = expenseAddedEvent
const createdEvent: ActivityEventJson = activityEventOf({
  id: '0f000000-0000-4000-8000-000000000002',
  type: 'GroupCreated',
  data: { name: 'Greece trip' },
  createdAt: '2026-10-03T08:00:00Z',
})
const editedEvent: ActivityEventJson = editedEventOf([
  { field: 'amount', old: '280000', new: '300000' },
  { field: 'title', old: '', new: 'Hotel' },
])
const events: ActivityEventJson[] = [
  { ...addedEvent, id: '0f000000-0000-4000-8000-000000000003' },
  { ...editedEvent, id: '0f000000-0000-4000-8000-000000000004' },
  createdEvent,
]

const eventFields = Object.keys(activityEventOf({}))

function withoutField(source: object, field: string): object {
  return Object.fromEntries(Object.entries(source).filter(([name]) => name !== field))
}

describe('getActivity', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('asks GET /api/groups/{id}/activity and returns the events in the order of the answer', async () => {
    const fetchMock = stubFetch(Response.json({ events }))

    const result = await getActivity(testGroupId)

    const request = sentRequest(fetchMock)
    expect(request.url).toBe(`/api/groups/${testGroupId}/activity`)
    expect(request.method ?? 'GET').toBe('GET')
    expect(result).toEqual(events)
  })

  it('returns an empty list when nothing has happened yet', async () => {
    stubFetch(Response.json({ events: [] }))

    const result = await getActivity(testGroupId)

    expect(result).toEqual([])
  })

  it('keeps null for the changes and the data of an event that has none', async () => {
    stubFetch(Response.json({ events: [activityEventOf({ type: 'GroupDeleted', changes: null, data: null })] }))

    const result = await getActivity(testGroupId)

    expect(result[0]).toMatchObject({ changes: null, data: null })
  })

  it('keeps null for the expense id and the member id of an event about neither', async () => {
    stubFetch(Response.json({ events: [activityEventOf({ type: 'InviteLinkReset' })] }))

    const result = await getActivity(testGroupId)

    expect(result[0]).toMatchObject({ expenseId: null, memberId: null })
  })

  it('returns the changes of an edit as a list of field, old and new, empty texts included', async () => {
    stubFetch(Response.json({ events: [editedEvent] }))

    const result = await getActivity(testGroupId)

    expect(result[0].changes).toEqual([
      { field: 'amount', old: '280000', new: '300000' },
      { field: 'title', old: '', new: 'Hotel' },
    ])
  })

  it('returns the data of an expense event with its title, amount and currency', async () => {
    stubFetch(Response.json({ events: [addedEvent] }))

    const result = await getActivity(testGroupId)

    expect(result[0].data).toEqual({ title: 'Dinner', amountMinor: 240000, currency: 'MKD' })
  })

  it('returns the data of a member event with its name', async () => {
    const memberEvent = activityEventOf({ type: 'MemberAdded', data: { name: 'Grandma' } })
    stubFetch(Response.json({ events: [memberEvent] }))

    const result = await getActivity(testGroupId)

    expect(result[0].data).toEqual({ name: 'Grandma' })
  })

  it('tolerates a field it does not know in an event, like the other parsers', async () => {
    stubFetch(Response.json({ events: [{ ...createdEvent, somethingNew: 1 }] }))

    const result = await getActivity(testGroupId)

    expect(result[0]).toMatchObject({ id: createdEvent.id, type: 'GroupCreated' })
  })

  it('tolerates a field it does not know in the answer', async () => {
    stubFetch(Response.json({ events, nextPage: null }))

    const result = await getActivity(testGroupId)

    expect(result).toEqual(events)
  })

  it.each([
    ['a server error', new Response(null, { status: 500 }), 500, null],
    ['a group that is not found', problemResponse(404, 'GROUP_NOT_FOUND'), 404, 'GROUP_NOT_FOUND'],
    ['a rate limit', problemResponse(429, 'RATE_LIMITED'), 429, 'RATE_LIMITED'],
  ])('rethrows the ApiError unchanged for %s', async (_name, response, httpStatus, errorCode) => {
    stubFetch(response)

    const error = await captureError(getActivity(testGroupId))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus, errorCode })
  })

  it('rethrows the ApiError unchanged when the network fails', async () => {
    stubFetch(new TypeError('Failed to fetch'))

    const error = await captureError(getActivity(testGroupId))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus: null })
  })

  it('throws an error naming "events" when the answer lacks the list', async () => {
    stubFetch(Response.json({}))

    await expect(getActivity(testGroupId)).rejects.toThrow('events')
  })

  it.each([
    ['a text', 'Created'],
    ['null', null],
    ['an object', {}],
  ])('throws an error naming "events" when the list is %s', async (_name, value) => {
    stubFetch(Response.json({ events: value }))

    await expect(getActivity(testGroupId)).rejects.toThrow('events')
  })

  it.each(eventFields)('throws an error naming "%s" when an event lacks it', async (field) => {
    stubFetch(Response.json({ events: [withoutField(addedEvent, field)] }))

    await expect(getActivity(testGroupId)).rejects.toThrow(field)
  })

  it.each([
    ['id', 42],
    ['id', null],
    ['type', 5],
    ['type', null],
    ['actorUserId', null],
    ['actorName', null],
    ['actorName', 5],
    ['expenseId', 7],
    ['memberId', 7],
    ['changes', 'amount'],
    ['changes', {}],
    ['data', 'Dinner'],
    ['data', []],
    ['data', 5],
    ['createdAt', 'a while ago'],
    ['createdAt', null],
    ['createdAt', 20261006],
  ])('throws an error naming "%s" when its value in an event is %j', async (field, value) => {
    stubFetch(Response.json({ events: [{ ...addedEvent, [field]: value }] }))

    await expect(getActivity(testGroupId)).rejects.toThrow(field)
  })

  it('checks every event, not only the first', async () => {
    stubFetch(Response.json({ events: [addedEvent, { ...createdEvent, actorName: null }] }))

    await expect(getActivity(testGroupId)).rejects.toThrow('actorName')
  })

  it.each(['field', 'old', 'new'])('throws an error naming "%s" when a change lacks it', async (field) => {
    const change = { field: 'amount', old: '280000', new: '300000' }
    stubFetch(Response.json({ events: [{ ...editedEvent, changes: [withoutField(change, field)] }] }))

    await expect(getActivity(testGroupId)).rejects.toThrow(field)
  })

  it.each([
    ['field', 5],
    ['old', 280000],
    ['new', null],
  ])('throws an error naming "%s" when its value in a change is %j', async (field, value) => {
    const change = { field: 'amount', old: '280000', new: '300000', [field]: value }
    stubFetch(Response.json({ events: [{ ...editedEvent, changes: [change] }] }))

    await expect(getActivity(testGroupId)).rejects.toThrow(field)
  })

  it.each([
    ['null', null],
    ['a text', 'Healthy'],
    ['a list', []],
  ])('throws an error saying an object was expected when the answer is %s', async (_name, body) => {
    stubFetch(Response.json(body))

    await expect(getActivity(testGroupId)).rejects.toThrow(/object/)
  })
})
