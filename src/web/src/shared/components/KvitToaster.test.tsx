import { render } from '@testing-library/react'
import type { ToasterProps } from 'sonner'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { KvitToaster } from './KvitToaster'

const toasterPropsSeen = vi.hoisted((): { latest: ToasterProps | null } => ({ latest: null }))

vi.mock('sonner', () => ({
  Toaster: (props: ToasterProps) => {
    toasterPropsSeen.latest = props
    return null
  },
}))

const bottomOffsetAboveTheLiftedToasts = 'calc(var(--toast-offset-bottom) + var(--toast-lift, 0px))'

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

  it('puts the toasts at the bottom offset plus the lift, so a bar above them can lift them', () => {
    expect(toasterProps().offset).toEqual({ bottom: bottomOffsetAboveTheLiftedToasts })
  })

  it('uses the same bottom offset plus the lift on small screens', () => {
    expect(toasterProps().mobileOffset).toEqual({ bottom: bottomOffsetAboveTheLiftedToasts })
  })

  it('still shows the toasts at the bottom centre', () => {
    expect(toasterProps().position).toBe('bottom-center')
  })
})
