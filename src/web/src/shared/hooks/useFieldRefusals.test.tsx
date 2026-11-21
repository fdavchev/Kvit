import { act, renderHook } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { ApiError } from '@/core/api/apiClient'
import { useFieldRefusals } from './useFieldRefusals'

const refusedCodesOf = {
  email: ['AUTH_EMAIL_INVALID', 'AUTH_EMAIL_TAKEN'],
  password: ['AUTH_PASSWORD_TOO_WEAK'],
}

function refusedWith(errorCode: string): ApiError {
  return new ApiError(`POST /api/auth/register failed with status 400`, { httpStatus: 400, errorCode })
}

function mountedWith(error: unknown) {
  return renderHook(({ currentError }) => useFieldRefusals(refusedCodesOf, currentError), {
    initialProps: { currentError: error },
  })
}

describe('useFieldRefusals', () => {
  it('refuses no field while there is no error', () => {
    const { result } = mountedWith(null)

    expect(result.current.isRefused('email')).toBe(false)
    expect(result.current.isRefused('password')).toBe(false)
  })

  it.each(['AUTH_EMAIL_INVALID', 'AUTH_EMAIL_TAKEN'])('refuses the email field for %s and not the password field', (code) => {
    const { result } = mountedWith(refusedWith(code))

    expect(result.current.isRefused('email')).toBe(true)
    expect(result.current.isRefused('password')).toBe(false)
  })

  it('refuses the password field for a password code and not the email field', () => {
    const { result } = mountedWith(refusedWith('AUTH_PASSWORD_TOO_WEAK'))

    expect(result.current.isRefused('password')).toBe(true)
    expect(result.current.isRefused('email')).toBe(false)
  })

  it('refuses no field for a code that belongs to none of them', () => {
    const { result } = mountedWith(refusedWith('RATE_LIMITED'))

    expect(result.current.isRefused('email')).toBe(false)
    expect(result.current.isRefused('password')).toBe(false)
  })

  it('refuses no field for an error that is not an answer of the server', () => {
    const { result } = mountedWith(new TypeError('Failed to fetch'))

    expect(result.current.isRefused('email')).toBe(false)
  })

  it('stops refusing a field once it is changed', () => {
    const { result, rerender } = mountedWith(refusedWith('AUTH_EMAIL_TAKEN'))

    act(() => result.current.noteChange('email'))
    rerender({ currentError: refusedWith('AUTH_EMAIL_TAKEN') })

    expect(result.current.isRefused('email')).toBe(false)
  })

  it('keeps refusing a field when only another field is changed', () => {
    const { result } = mountedWith(refusedWith('AUTH_EMAIL_TAKEN'))

    act(() => result.current.noteChange('password'))

    expect(result.current.isRefused('email')).toBe(true)
  })

  it('refuses the field again on the next refused save, even after it was changed', () => {
    const { result, rerender } = mountedWith(refusedWith('AUTH_EMAIL_TAKEN'))
    act(() => result.current.noteChange('email'))

    act(() => result.current.noteSave())
    rerender({ currentError: refusedWith('AUTH_EMAIL_TAKEN') })

    expect(result.current.isRefused('email')).toBe(true)
  })

  it('marks a field that was changed twice as unchanged again after a save', () => {
    const { result } = mountedWith(refusedWith('AUTH_PASSWORD_TOO_WEAK'))
    act(() => result.current.noteChange('password'))
    act(() => result.current.noteChange('password'))

    act(() => result.current.noteSave())

    expect(result.current.isRefused('password')).toBe(true)
  })

  it('stops refusing every field when the error goes away', () => {
    const { result, rerender } = mountedWith(refusedWith('AUTH_EMAIL_TAKEN'))

    rerender({ currentError: null })

    expect(result.current.isRefused('email')).toBe(false)
  })
})
