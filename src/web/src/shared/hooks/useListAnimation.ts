import autoAnimate from '@formkit/auto-animate'
import { useCallback, type RefCallback } from 'react'

export const listAnimationDurationMs = 200

export function useListAnimation<T extends HTMLElement>(): RefCallback<T> {
  return useCallback((list: T | null) => {
    if (list === null) {
      return
    }
    const controller = autoAnimate(list, { duration: listAnimationDurationMs })
    return () => controller.destroy?.()
  }, [])
}
