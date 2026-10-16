import type { CSSProperties } from 'react'
import { useTranslation } from 'react-i18next'
import { Toaster } from 'sonner'
import { useTheme } from '@/core/theme/useTheme'
import { kvitGlassPanelBlur } from '@/shared/components/kvitGlassLooks'

const toasterStyle: CSSProperties & Record<`--${string}`, string> = {
  '--normal-bg': 'var(--glass-panel)',
  '--normal-text': 'var(--popover-foreground)',
  '--normal-border': 'var(--glass-edge)',
  '--border-radius': 'var(--radius)',
}

const bottomOffset = { bottom: 'calc(var(--toast-offset-bottom) + var(--toast-lift, 0px))' }

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
  const { t } = useTranslation()
  const { theme } = useTheme()
  return (
    <Toaster
      theme={theme}
      position="bottom-center"
      style={toasterStyle}
      offset={bottomOffset}
      mobileOffset={bottomOffset}
      toastOptions={{ className: kvitGlassPanelBlur, actionButtonStyle }}
      containerAriaLabel={t('toasts.region')}
    />
  )
}
