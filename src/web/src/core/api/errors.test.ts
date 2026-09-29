import { describe, expect, it } from 'vitest'
import { ApiError } from './apiClient'
import { errorMessageKey } from './errors'

describe('errorMessageKey', () => {
  it('uses the network message when the server could not be reached', () => {
    const error = new ApiError('GET /api/health could not reach the server', {
      httpStatus: null,
    })

    expect(errorMessageKey(error)).toBe('errors.network')
  })

  it('uses the network message when the proxy answers 502 because the API is unreachable', () => {
    const error = new ApiError('GET /api/health failed with status 502', {
      httpStatus: 502,
    })

    expect(errorMessageKey(error)).toBe('errors.network')
  })

  it('uses the generic message for an error code with no translation', () => {
    const error = new ApiError('GET /api/groups/1 failed with status 404', {
      httpStatus: 404,
      errorCode: 'GROUP_NOT_FOUND',
    })

    expect(errorMessageKey(error)).toBe('errors.generic')
  })

  it('uses the generic message for an error that is not an ApiError', () => {
    expect(errorMessageKey(new Error('boom'))).toBe('errors.generic')
  })
})
