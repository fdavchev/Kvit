import { beforeEach, describe, expect, it, vi } from 'vitest'
import {
  actionOf,
  pressToastAction,
  stylingOf,
  toastShownWithText,
  toastsShownWithOptions,
} from '@/test/toastTestHelpers'
import { showDangerToast, showUndoToast } from './showUndoToast'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

function undoToastOf(danger: boolean, onUndo: () => void = () => {}) {
  showUndoToast('Group deleted', { danger, undoLabel: 'Undo', onUndo })
  return toastShownWithText('Group deleted')
}

describe('showUndoToast', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it.each([true, false])('shows exactly one toast with the message when danger is %s', (danger) => {
    undoToastOf(danger)

    expect(toastsShownWithOptions()).toHaveLength(1)
  })

  it.each([true, false])('keeps the toast on screen for 4 seconds when danger is %s', (danger) => {
    const shown = undoToastOf(danger)

    expect(shown.options.duration).toBe(4000)
  })

  it.each([true, false])('puts the undo label on the action button when danger is %s', (danger) => {
    const shown = undoToastOf(danger)

    expect(actionOf(shown).label).toBe('Undo')
  })

  it('runs onUndo once when the action button is pressed', () => {
    const onUndo = vi.fn<() => void>()
    const shown = undoToastOf(false, onUndo)

    pressToastAction(shown)

    expect(onUndo).toHaveBeenCalledOnce()
  })

  it('does not run onUndo before the action button is pressed', () => {
    const onUndo = vi.fn<() => void>()

    undoToastOf(true, onUndo)

    expect(onUndo).not.toHaveBeenCalled()
  })

  it('marks a danger toast with a style that a normal toast does not have', () => {
    const danger = stylingOf(undoToastOf(true))
    vi.clearAllMocks()
    const normal = stylingOf(undoToastOf(false))

    expect(danger).not.toEqual(normal)
  })
})

describe('showDangerToast', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('shows exactly one toast with the message', () => {
    showDangerToast('You left Greece trip')

    expect(toastsShownWithOptions().map((shown) => shown.message)).toEqual(['You left Greece trip'])
  })

  it('keeps the toast on screen for 4 seconds', () => {
    showDangerToast('You left Greece trip')

    expect(toastShownWithText('You left Greece trip').options.duration).toBe(4000)
  })

  it('has no action button', () => {
    showDangerToast('You left Greece trip')

    expect(toastShownWithText('You left Greece trip').options.action).toBeUndefined()
  })

  it('has the same danger style as a danger undo toast', () => {
    const undoStyling = stylingOf(undoToastOf(true))
    vi.clearAllMocks()
    showDangerToast('You left Greece trip')

    expect(stylingOf(toastShownWithText('You left Greece trip'))).toEqual(undoStyling)
  })

  it('has a different style from a normal undo toast', () => {
    const normalStyling = stylingOf(undoToastOf(false))
    vi.clearAllMocks()
    showDangerToast('You left Greece trip')

    expect(stylingOf(toastShownWithText('You left Greece trip'))).not.toEqual(normalStyling)
  })
})
