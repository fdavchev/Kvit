import { render } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { useFocusWhenOpenerIsGone, type FocusAfterSheet } from './useFocusWhenOpenerIsGone'

interface TargetProps {
  focus: FocusAfterSheet | null
  isTarget: boolean
  onFocusHandled: () => void
}

function Target({ focus, isTarget, onFocusHandled }: TargetProps) {
  const ref = useFocusWhenOpenerIsGone<HTMLButtonElement>(focus, isTarget, onFocusHandled)
  return (
    <button ref={ref} type="button">
      target
    </button>
  )
}

function connectedOpener(): HTMLButtonElement {
  const opener = document.createElement('button')
  document.body.append(opener)
  return opener
}

function goneOpener(): HTMLButtonElement {
  return document.createElement('button')
}

function targetButton(): HTMLElement {
  const button = document.querySelector<HTMLElement>('button')
  if (button === null) {
    throw new Error('The target button is not on the page')
  }
  return button
}

describe('useFocusWhenOpenerIsGone', () => {
  it('focuses the target when it is the target and the opener is gone, then says the request is handled', () => {
    const onFocusHandled = vi.fn<() => void>()

    render(<Target focus={{ opener: goneOpener() }} isTarget onFocusHandled={onFocusHandled} />)

    expect(document.activeElement).toBe(targetButton())
    expect(onFocusHandled).toHaveBeenCalledOnce()
  })

  it('focuses the target when the request has no opener at all', () => {
    render(<Target focus={{ opener: null }} isTarget onFocusHandled={() => {}} />)

    expect(document.activeElement).toBe(targetButton())
  })

  it('leaves the focus alone when the opener is still on the page, but still says the request is handled', () => {
    const opener = connectedOpener()
    const onFocusHandled = vi.fn<() => void>()

    render(<Target focus={{ opener }} isTarget onFocusHandled={onFocusHandled} />)

    expect(document.activeElement).toBe(document.body)
    expect(onFocusHandled).toHaveBeenCalledOnce()
    opener.remove()
  })

  it('does nothing and keeps the request when it is not the target', () => {
    const onFocusHandled = vi.fn<() => void>()

    render(<Target focus={{ opener: goneOpener() }} isTarget={false} onFocusHandled={onFocusHandled} />)

    expect(document.activeElement).toBe(document.body)
    expect(onFocusHandled).not.toHaveBeenCalled()
  })

  it('does nothing when there is no request', () => {
    const onFocusHandled = vi.fn<() => void>()

    render(<Target focus={null} isTarget onFocusHandled={onFocusHandled} />)

    expect(document.activeElement).toBe(document.body)
    expect(onFocusHandled).not.toHaveBeenCalled()
  })

  it('acts when the part becomes the target later, with the same request', () => {
    const focus = { opener: goneOpener() }
    const onFocusHandled = vi.fn<() => void>()
    const { rerender } = render(<Target focus={focus} isTarget={false} onFocusHandled={onFocusHandled} />)

    rerender(<Target focus={focus} isTarget onFocusHandled={onFocusHandled} />)

    expect(document.activeElement).toBe(targetButton())
    expect(onFocusHandled).toHaveBeenCalledOnce()
  })

  it('does not run again when only a new inline callback arrives', () => {
    const focus = { opener: goneOpener() }
    const firstCallback = vi.fn<() => void>()
    const secondCallback = vi.fn<() => void>()
    const { rerender } = render(<Target focus={focus} isTarget onFocusHandled={firstCallback} />)
    targetButton().blur()

    rerender(<Target focus={focus} isTarget onFocusHandled={secondCallback} />)

    expect(firstCallback).toHaveBeenCalledOnce()
    expect(secondCallback).not.toHaveBeenCalled()
    expect(document.activeElement).toBe(document.body)
  })

  it('calls the latest callback when a new request arrives after a new callback', () => {
    const firstCallback = vi.fn<() => void>()
    const secondCallback = vi.fn<() => void>()
    const { rerender } = render(<Target focus={null} isTarget onFocusHandled={firstCallback} />)

    rerender(<Target focus={{ opener: goneOpener() }} isTarget onFocusHandled={secondCallback} />)

    expect(firstCallback).not.toHaveBeenCalled()
    expect(secondCallback).toHaveBeenCalledOnce()
  })

  it('runs again for a new request', () => {
    const onFocusHandled = vi.fn<() => void>()
    const { rerender } = render(<Target focus={{ opener: goneOpener() }} isTarget onFocusHandled={onFocusHandled} />)

    rerender(<Target focus={{ opener: goneOpener() }} isTarget onFocusHandled={onFocusHandled} />)

    expect(onFocusHandled).toHaveBeenCalledTimes(2)
  })
})
