import { act, render, screen } from '@testing-library/react'
import { StrictMode } from 'react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { stubDeviceMotion } from '@/test/matchMediaTestHelpers'
import { listAnimationDurationMs, useListAnimation } from './useListAnimation'

interface AnimateCall {
  rowText: string | null
  duration: number | undefined
  finish: () => void
}

const animateCalls: AnimateCall[] = []

function fakeAnimate(this: Element, _keyframes: Keyframe[], options: KeyframeAnimationOptions): Animation {
  const finishListeners: (() => void)[] = []
  animateCalls.push({
    rowText: this.textContent,
    duration: options.duration as number | undefined,
    finish: () => finishListeners.forEach((listener) => listener()),
  })
  return {
    playState: 'running',
    finished: Promise.resolve(),
    cancel: () => {},
    addEventListener: (type: string, listener: () => void) => {
      if (type === 'finish') {
        finishListeners.push(listener)
      }
    },
  } as unknown as Animation
}

function AnimatedList({ rows }: { rows: readonly string[] }) {
  const listRef = useListAnimation<HTMLUListElement>()
  return (
    <ul ref={listRef}>
      {rows.map((row) => (
        <li key={row}>{row}</li>
      ))}
    </ul>
  )
}

function renderList(rows: readonly string[]) {
  const view = render(
    <StrictMode>
      <AnimatedList rows={rows} />
    </StrictMode>,
  )
  return {
    showRows: (nextRows: readonly string[]) =>
      view.rerender(
        <StrictMode>
          <AnimatedList rows={nextRows} />
        </StrictMode>,
      ),
  }
}

function callsFor(rowText: string): AnimateCall[] {
  return animateCalls.filter((call) => call.rowText === rowText)
}

async function letTheListMeasureItsRows(): Promise<void> {
  await act(async () => {
    await vi.advanceTimersByTimeAsync(listAnimationDurationMs + 50)
  })
}

describe('useListAnimation', () => {
  beforeEach(() => {
    animateCalls.length = 0
    Object.defineProperty(Element.prototype, 'animate', { configurable: true, value: fakeAnimate })
  })

  afterEach(() => {
    Reflect.deleteProperty(Element.prototype, 'animate')
    vi.unstubAllGlobals()
    vi.useRealTimers()
  })

  describe('when the device asks for reduced motion', () => {
    beforeEach(() => {
      stubDeviceMotion('reduced')
    })

    it('shows a new row at once, without an animation', async () => {
      const list = renderList(['Rent'])

      await act(async () => list.showRows(['Rent', 'Pizza']))

      expect(screen.getByText('Pizza')).toBeTruthy()
      expect(animateCalls).toEqual([])
    })

    it('takes a removed row away at once, without an animation', async () => {
      const list = renderList(['Rent', 'Pizza'])

      await act(async () => list.showRows(['Rent']))

      expect(screen.queryByText('Pizza')).toBeNull()
      expect(animateCalls).toEqual([])
    })
  })

  describe('when the device allows motion', () => {
    beforeEach(() => {
      vi.useFakeTimers()
      stubDeviceMotion('allowed')
    })

    it('fades a new row in once, also when React mounts the list twice in strict mode', async () => {
      const list = renderList(['Rent'])
      await letTheListMeasureItsRows()

      await act(async () => list.showRows(['Rent', 'Pizza']))

      expect(callsFor('Pizza')).toHaveLength(1)
    })

    it('fades a removed row out in 200 ms, and the fading row lets taps through', async () => {
      const list = renderList(['Rent', 'Pizza'])
      await letTheListMeasureItsRows()

      await act(async () => list.showRows(['Rent']))

      const fadingRow = screen.getByText('Pizza')
      expect(fadingRow.style.pointerEvents).toBe('none')
      expect(callsFor('Pizza').map((call) => call.duration)).toEqual([200])
    })

    it('takes the removed row out of the page when its fade ends', async () => {
      const list = renderList(['Rent', 'Pizza'])
      await letTheListMeasureItsRows()
      await act(async () => list.showRows(['Rent']))

      act(() => callsFor('Pizza')[0].finish())

      expect(screen.queryByText('Pizza')).toBeNull()
    })
  })
})
