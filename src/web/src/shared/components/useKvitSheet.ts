import { useRef, type RefObject } from 'react'
import type { KvitSheetActions } from './KvitSheet'

interface KvitSheetControl {
  actionsRef: RefObject<KvitSheetActions | null>
  close: () => void
}

export function useKvitSheet(): KvitSheetControl {
  const actionsRef = useRef<KvitSheetActions | null>(null)

  function close(): void {
    const actions = actionsRef.current
    if (actions === null) {
      throw new Error('The sheet cannot close because it is not rendered')
    }
    actions.close()
  }

  return { actionsRef, close }
}
