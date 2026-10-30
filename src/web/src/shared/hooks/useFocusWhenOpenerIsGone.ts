import { useEffect, useRef, type RefObject } from 'react'

export interface FocusAfterSheet {
  opener: Element | null
}

export function useFocusWhenOpenerIsGone<T extends HTMLElement>(
  focus: FocusAfterSheet | null,
  isTarget: boolean,
  onFocusHandled: () => void,
): RefObject<T | null> {
  const ref = useRef<T>(null)

  useEffect(() => {
    if (focus === null || !isTarget) {
      return
    }
    if (focus.opener?.isConnected !== true) {
      ref.current?.focus()
    }
    onFocusHandled()
  }, [focus, isTarget, onFocusHandled])
  return ref
}
