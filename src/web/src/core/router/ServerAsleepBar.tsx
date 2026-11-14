import { useTranslation } from 'react-i18next'
import { wakeServer } from '@/core/api/serverWake'
import { useOfferedWakeUrl } from '@/core/api/useOfferedWakeUrl'
import { KvitButton } from '@/shared/components/KvitButton'
import { kvitGlassPanelLook } from '@/shared/components/kvitGlassLooks'
import { cn } from '@/shared/utils/cn'

export function ServerAsleepBar() {
  const { t } = useTranslation()
  const wakeUrl = useOfferedWakeUrl()
  if (wakeUrl === null) {
    return null
  }

  return (
    <div
      data-server-asleep-bar
      role="status"
      className="pointer-events-none fixed inset-x-0 top-[max(0.5rem,env(safe-area-inset-top))] z-30 mx-auto w-full max-w-md px-4 motion-safe:animate-tab-panel-in"
    >
      <div
        className={cn(
          'pointer-events-auto flex min-h-12 items-center gap-2 rounded-2xl px-4 py-1 text-[0.9375rem] leading-tight font-semibold',
          kvitGlassPanelLook,
        )}
      >
        <span className="min-w-0 flex-1">{t('wakingUp.asleep')}</span>
        <KvitButton variant="smallLink" className="w-auto min-w-11 flex-none px-2" onClick={() => wakeServer(wakeUrl)}>
          {t('wakingUp.wake')}
        </KvitButton>
      </div>
    </div>
  )
}
