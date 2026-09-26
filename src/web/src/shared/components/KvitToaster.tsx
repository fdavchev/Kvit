import type { CSSProperties } from 'react'
import { Toaster } from 'sonner'

const toasterStyle: CSSProperties & Record<`--${string}`, string> = {
  '--normal-bg': 'var(--popover)',
  '--normal-text': 'var(--popover-foreground)',
  '--normal-border': 'var(--border)',
  '--border-radius': 'var(--radius)',
}

export function KvitToaster() {
  return <Toaster theme="system" position="bottom-center" style={toasterStyle} />
}
