import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from './apiClient'
import { errorMessageKey } from './errors'

describe('errorMessageKey', () => {
  afterEach(() => {
    vi.restoreAllMocks()
  })

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
    vi.spyOn(console, 'error').mockImplementation(() => {})
    const error = new ApiError('GET /api/groups/1 failed with status 404', {
      httpStatus: 404,
      errorCode: 'GROUP_NOT_FOUND',
    })

    expect(errorMessageKey(error)).toBe('errors.generic')
  })

  it('logs an error code that has no translation key, naming the code', () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {})
    const error = new ApiError('GET /api/groups/1 failed with status 403', {
      httpStatus: 403,
      errorCode: 'GROUP_NOT_OWNER',
    })

    errorMessageKey(error)

    expect(consoleError).toHaveBeenCalledWith(
      'No translation key is mapped for API error code "GROUP_NOT_OWNER"',
      error,
    )
  })

  it('does not log when the answer carries no error code', () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {})

    errorMessageKey(new ApiError('GET /api/x failed with status 500', { httpStatus: 500 }))

    expect(consoleError).not.toHaveBeenCalled()
  })

  it('uses the generic message for an error that is not an ApiError', () => {
    expect(errorMessageKey(new Error('boom'))).toBe('errors.generic')
  })
})
