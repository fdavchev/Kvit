import { renderHook } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { askBeforeLeavingWhenTyping, hasUnsavedTyping, useUnsavedTyping } from './useUnsavedTyping'

function mountedForm(initialErrorMessage: string | null = null) {
  return renderHook(({ errorMessage }) => useUnsavedTyping(errorMessage), {
    initialProps: { errorMessage: initialErrorMessage },
  })
}

function leavingThePage(): Event {
  const leaving = new Event('beforeunload', { cancelable: true })
  window.dispatchEvent(leaving)
  return leaving
}

describe('useUnsavedTyping', () => {
  it('has no unsaved typing before anybody typed', () => {
    mountedForm()

    expect(hasUnsavedTyping()).toBe(false)
  })

  it('has unsaved typing once a form notes typing', () => {
    const { result } = mountedForm()

    result.current.noteTyping()

    expect(hasUnsavedTyping()).toBe(true)
  })

  it('has no unsaved typing again when the save starts', () => {
    const { result } = mountedForm()
    result.current.noteTyping()

    result.current.noteSaveStarted()

    expect(hasUnsavedTyping()).toBe(false)
  })

  it('has unsaved typing again when the save comes back with an error', () => {
    const { result, rerender } = mountedForm()
    result.current.noteTyping()
    result.current.noteSaveStarted()

    rerender({ errorMessage: 'This email is already taken.' })

    expect(hasUnsavedTyping()).toBe(true)
  })

  it('has no unsaved typing when the form opens already showing an error and nobody typed', () => {
    mountedForm('This email is already taken.')

    expect(hasUnsavedTyping()).toBe(false)
  })

  it('has no unsaved typing when an error appears in a form where nobody typed', () => {
    const { rerender } = mountedForm()

    rerender({ errorMessage: 'This email is already taken.' })

    expect(hasUnsavedTyping()).toBe(false)
  })

  it('has unsaved typing once somebody types in a form that opened showing an error', () => {
    const { result } = mountedForm('This email is already taken.')

    result.current.noteTyping()

    expect(hasUnsavedTyping()).toBe(true)
  })

  it('has no unsaved typing when the error goes away and a new save starts', () => {
    const { result, rerender } = mountedForm('This email is already taken.')

    rerender({ errorMessage: null })
    result.current.noteSaveStarted()

    expect(hasUnsavedTyping()).toBe(false)
  })

  it('has no unsaved typing after the form unmounted with typing in it', () => {
    const { result, unmount } = mountedForm()
    result.current.noteTyping()

    unmount()

    expect(hasUnsavedTyping()).toBe(false)
  })

  it('keeps the typing of one form when another form starts its save', () => {
    const first = mountedForm()
    const second = mountedForm()
    first.result.current.noteTyping()
    second.result.current.noteTyping()

    second.result.current.noteSaveStarted()

    expect(hasUnsavedTyping()).toBe(true)
  })

  it('has no unsaved typing when every form with typing is gone', () => {
    const first = mountedForm()
    const second = mountedForm()
    first.result.current.noteTyping()
    second.result.current.noteTyping()

    first.unmount()
    second.unmount()

    expect(hasUnsavedTyping()).toBe(false)
  })
})

describe('askBeforeLeavingWhenTyping', () => {
  it('lets the page go without a question when no form has unsaved typing', () => {
    mountedForm()

    askBeforeLeavingWhenTyping()

    expect(leavingThePage().defaultPrevented).toBe(false)
  })

  it('makes the browser ask before the page goes when a form has unsaved typing', () => {
    const { result } = mountedForm()
    result.current.noteTyping()

    askBeforeLeavingWhenTyping()

    expect(leavingThePage().defaultPrevented).toBe(true)
  })

  it('asks only once, so a later leave of the page is not held back', () => {
    const { result } = mountedForm()
    result.current.noteTyping()
    askBeforeLeavingWhenTyping()
    leavingThePage()

    expect(leavingThePage().defaultPrevented).toBe(false)
  })

  it('does not ask when the save started after the typing', () => {
    const { result } = mountedForm()
    result.current.noteTyping()
    result.current.noteSaveStarted()

    askBeforeLeavingWhenTyping()

    expect(leavingThePage().defaultPrevented).toBe(false)
  })

  it('does not ask when the form with the typing is gone', () => {
    const { result, unmount } = mountedForm()
    result.current.noteTyping()
    unmount()

    askBeforeLeavingWhenTyping()

    expect(leavingThePage().defaultPrevented).toBe(false)
  })

  it('asks again when a refused save brings the typing back', () => {
    const { result, rerender } = mountedForm()
    result.current.noteTyping()
    result.current.noteSaveStarted()
    rerender({ errorMessage: 'This email is already taken.' })

    askBeforeLeavingWhenTyping()

    expect(leavingThePage().defaultPrevented).toBe(true)
  })

  it('does not ask for a form that opened showing an error when nobody typed', () => {
    mountedForm('This email is already taken.')

    askBeforeLeavingWhenTyping()

    expect(leavingThePage().defaultPrevented).toBe(false)
  })

  it('does not hold the page back when the question was never asked for', () => {
    const { result } = mountedForm()
    result.current.noteTyping()

    expect(leavingThePage().defaultPrevented).toBe(false)
  })
})
