import { useRef } from 'react'

export interface SaveOnce {
  tryStart: () => boolean
  finish: () => void
}

export function useSaveOnce(): SaveOnce {
  const isSavingRef = useRef<boolean>(false)

  return {
    tryStart: () => {
      if (isSavingRef.current) {
        return false
      }
      isSavingRef.current = true
      return true
    },
    finish: () => {
      isSavingRef.current = false
    },
  }
}
