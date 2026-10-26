import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '@/core/api/apiClient'
import {
  captureError,
  problemResponse,
  sentRequest,
  stubFetch,
} from '@/test/apiTestHelpers'
import { testGroupId } from '@/test/groupTestData'
import { changeCountsInBudget } from './groupsService'

describe('changeCountsInBudget', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it.each([true, false])('sends PUT /api/groups/{id}/members/me/counts-in-budget with countsInBudget %j as JSON and resolves on 204', async (countsInBudget) => {
    const fetchMock = stubFetch(new Response(null, { status: 204 }))

    const result = await changeCountsInBudget(testGroupId, countsInBudget)

    expect(result).toBeUndefined()
    expect(sentRequest(fetchMock)).toEqual({
      url: `/api/groups/${testGroupId}/members/me/counts-in-budget`,
      method: 'PUT',
      contentType: 'application/json',
      body: { countsInBudget },
    })
  })

  it.each([
    ['the group is finished', 400, 'GROUP_FINISHED'],
    ['the group is closing and the person is not the owner', 400, 'GROUP_CLOSING'],
    ['the person is not in the group', 404, 'GROUP_NOT_FOUND'],
    ['the person is a guest', 403, 'GUEST_NOT_ALLOWED'],
  ])('rethrows the ApiError unchanged when %s', async (_name, httpStatus, errorCode) => {
    stubFetch(problemResponse(httpStatus, errorCode))

    const error = await captureError(changeCountsInBudget(testGroupId, false))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus, errorCode })
  })

  it('rethrows the ApiError unchanged when the network fails', async () => {
    stubFetch(new TypeError('Failed to fetch'))

    const error = await captureError(changeCountsInBudget(testGroupId, false))

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ httpStatus: null })
  })
})
