import { render } from '@testing-library/react'
import type { ToasterProps } from 'sonner'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { KvitToaster } from './KvitToaster'
import { kvitGlassPanelBlur } from './kvitGlassLooks'

const toasterPropsSeen = vi.hoisted((): { latest: ToasterProps | null } => ({ latest: null }))

vi.mock('sonner', () => ({
  Toaster: (props: ToasterProps) => {
    toasterPropsSeen.latest = props
    return null
  },
}))

const bottomOffsetAboveTheLiftedToasts =
  'min(var(--open-sheet-height, calc(var(--toast-offset-bottom) + var(--toast-lift, 0px))), calc(100dvh - 96px - env(safe-area-inset-top)))'

function toasterProps(): ToasterProps {
  render(<KvitToaster />)
  if (toasterPropsSeen.latest === null) {
    throw new Error('KvitToaster did not render the Sonner Toaster')
  }
  return toasterPropsSeen.latest
}

describe('KvitToaster', () => {
  beforeEach(() => {
    toasterPropsSeen.latest = null
  })

  it('puts the toasts above the open sheet when there is one, else at the bottom offset plus the lift, never higher than 96px below the top', () => {
    expect(toasterProps().offset).toEqual({ bottom: bottomOffsetAboveTheLiftedToasts })
  })

  it('uses the same bottom offset plus the lift on small screens', () => {
    expect(toasterProps().mobileOffset).toEqual({ bottom: bottomOffsetAboveTheLiftedToasts })
  })

  it('shows two or three toasts at once in full, one above the other, so each Undo can be tapped', () => {
    const props = toasterProps()

    expect(props.expand).toBe(true)
    expect(props.visibleToasts).toBe(3)
  })

  it('labels the toast area with its own label and not with the container label that ends with the Alt+T hint', () => {
    const props = toasterProps()

    expect(props.customAriaLabel).toEqual(expect.any(String))
    expect(props.customAriaLabel).not.toBe('')
    expect(props.containerAriaLabel).toBeUndefined()
  })

  it('still shows the toasts at the bottom centre', () => {
    expect(toasterProps().position).toBe('bottom-center')
  })

  it('blurs what is behind every toast with the blur of the glass panel', () => {
    expect(toasterProps().toastOptions?.className).toBe(kvitGlassPanelBlur)
  })

  it('paints the toasts with the see-through glass panel colour and its edge', () => {
    expect(toasterProps().style).toEqual(
      expect.objectContaining({ '--normal-bg': 'var(--glass-panel)', '--normal-border': 'var(--glass-edge)' }),
    )
  })
})
