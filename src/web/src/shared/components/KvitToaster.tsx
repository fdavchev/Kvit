import type { CSSProperties } from 'react'
import { useTranslation } from 'react-i18next'
import { Toaster } from 'sonner'
import { useTheme } from '@/core/theme/useTheme'
import { kvitGlassPanelBlur } from '@/shared/components/kvitGlassLooks'
import { openSheetHeightProperty } from '@/shared/hooks/useToastsAboveElement'

const toasterStyle: CSSProperties & Record<`--${string}`, string> = {
  '--normal-bg': 'var(--glass-panel)',
  '--normal-text': 'var(--popover-foreground)',
  '--normal-border': 'var(--glass-edge)',
  '--border-radius': 'var(--radius)',
}

const bottomAbovePageBars = 'calc(var(--toast-offset-bottom) + var(--toast-lift, 0px))'
const bottomAboveOpenSheet = `var(${openSheetHeightProperty}, ${bottomAbovePageBars})`
const highestBottomThatKeepsAToastOnScreen = 'calc(100dvh - 96px - env(safe-area-inset-top))'
const bottomOffset = { bottom: `min(${bottomAboveOpenSheet}, ${highestBottomThatKeepsAToastOnScreen})` }
const toastsShownAtOnce = 3

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
      expand
      visibleToasts={toastsShownAtOnce}
      style={toasterStyle}
      offset={bottomOffset}
      mobileOffset={bottomOffset}
      toastOptions={{ className: kvitGlassPanelBlur, actionButtonStyle }}
      customAriaLabel={t('toasts.region')}
    />
  )
}
