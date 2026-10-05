import type { CSSProperties } from 'react'
import { Toaster } from 'sonner'
import { useTheme } from '@/core/theme/useTheme'

const toasterStyle: CSSProperties & Record<`--${string}`, string> = {
  '--normal-bg': 'var(--popover)',
  '--normal-text': 'var(--popover-foreground)',
  '--normal-border': 'var(--border)',
  '--border-radius': 'var(--radius)',
}

const bottomOffset = { bottom: 'var(--toast-offset-bottom)' }

const actionButtonStyle: CSSProperties = {
  height: 'auto',
  minHeight: '44px',
  padding: '0 8px',
  background: 'transparent',
  color: 'var(--normal-text)',
  fontSize: 'inherit',
  fontWeight: 800,
  textDecoration: 'underline',
}

export function KvitToaster() {
  const { theme } = useTheme()
  return (
    <Toaster
      theme={theme}
      position="bottom-center"
      style={toasterStyle}
      offset={bottomOffset}
      mobileOffset={bottomOffset}
      toastOptions={{ actionButtonStyle }}
    />
  )
}
