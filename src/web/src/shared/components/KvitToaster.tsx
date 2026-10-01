import type { CSSProperties } from 'react'
import { Toaster } from 'sonner'
import { useTheme } from '@/core/theme/useTheme'

const toasterStyle: CSSProperties & Record<`--${string}`, string> = {
  '--normal-bg': 'var(--popover)',
  '--normal-text': 'var(--popover-foreground)',
  '--normal-border': 'var(--border)',
  '--border-radius': 'var(--radius)',
}

export function KvitToaster() {
  const { theme } = useTheme()
  return <Toaster theme={theme} position="bottom-center" style={toasterStyle} />
}
