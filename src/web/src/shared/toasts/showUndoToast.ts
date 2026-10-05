import type { CSSProperties } from 'react'
import { toast } from 'sonner'

interface UndoToastOptions {
  danger: boolean
  undoLabel: string
  onUndo: () => void
}

const toastDurationMs = 4000

const dangerToastStyle: CSSProperties & Record<`--${string}`, string> = {
  '--normal-bg': 'var(--danger-fill)',
  '--normal-border': 'var(--danger-fill)',
  '--normal-text': 'var(--danger-fill-foreground)',
}

export function showUndoToast(
  message: string,
  { danger, undoLabel, onUndo }: UndoToastOptions,
): void {
  toast(message, {
    duration: toastDurationMs,
    style: danger ? dangerToastStyle : undefined,
    action: { label: undoLabel, onClick: onUndo },
  })
}

export function showDangerToast(message: string): void {
  toast(message, { duration: toastDurationMs, style: dangerToastStyle })
}
