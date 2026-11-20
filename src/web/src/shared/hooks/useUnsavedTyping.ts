import { useEffect, useRef, useState } from 'react'

const formsWithUnsavedTyping = new Set<object>()

export interface UnsavedTyping {
  noteTyping: () => void
  noteSaveStarted: () => void
}

export function useUnsavedTyping(errorMessage: string | null): UnsavedTyping {
  const [form] = useState<object>(() => ({}))
  const hasTyped = useRef<boolean>(false)

  useEffect(() => {
    if (errorMessage !== null && hasTyped.current) {
      formsWithUnsavedTyping.add(form)
    }
  }, [errorMessage, form])

  useEffect(() => () => void formsWithUnsavedTyping.delete(form), [form])

  return {
    noteTyping: () => {
      hasTyped.current = true
      formsWithUnsavedTyping.add(form)
    },
    noteSaveStarted: () => formsWithUnsavedTyping.delete(form),
  }
}

export function hasUnsavedTyping(): boolean {
  return formsWithUnsavedTyping.size > 0
}

export function askBeforeLeavingWhenTyping(): void {
  if (hasUnsavedTyping()) {
    window.addEventListener('beforeunload', askTheBrowserToConfirmLeaving, { once: true })
  }
}

function askTheBrowserToConfirmLeaving(event: BeforeUnloadEvent): void {
  event.preventDefault()
}
