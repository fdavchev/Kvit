import { useTranslation } from 'react-i18next'
import { Outlet } from 'react-router'
import { useIsRefreshingSavedData } from '@/core/api/useIsRefreshingSavedData'
import { useOfferedWakeUrl } from '@/core/api/useOfferedWakeUrl'
import { useLoadedDashboardGroups } from '@/features/home/hooks/useLoadedDashboardGroups'
import { KvitRail, type KvitRailItem } from '@/shared/components/KvitRail'
import { KvitUpdatingNote } from '@/shared/components/KvitUpdatingNote'
import { useIsDesktop } from '@/shared/hooks/useIsDesktop'
import { routes } from './routes'
import { useMainNavItems } from './useMainNavItems'

export function DesktopRailLayout() {
  const isDesktop = useIsDesktop()

  return (
    <div className="contents lg:grid lg:min-h-dvh lg:grid-cols-[var(--rail-width)_minmax(0,1fr)]">
      {isDesktop && <SignedInRail />}
      <div className="contents lg:relative lg:block lg:min-w-0">
        <UpdatingNoteSlot />
        <Outlet />
      </div>
    </div>
  )
}

function UpdatingNoteSlot() {
  const isRefreshingSavedData = useIsRefreshingSavedData()
  const isWakeOffered = useOfferedWakeUrl() !== null

  return (
    <div
      data-updating-note-slot
      className="pointer-events-none absolute inset-x-0 top-[calc(max(12px,env(safe-area-inset-top))-10px)] z-10 flex justify-center"
    >
      {isRefreshingSavedData && !isWakeOffered && <KvitUpdatingNote />}
    </div>
  )
}

function SignedInRail() {
  const { t } = useTranslation()
  const mainItems = useMainNavItems()
  const groups = useLoadedDashboardGroups()
  const items: KvitRailItem[] = mainItems.map((item) =>
    item.to === routes.groups
      ? {
          ...item,
          subItems: groups.map((group) => ({ to: routes.group(group.id), label: group.name, emoji: group.emoji })),
        }
      : item,
  )

  return <KvitRail brand={t('app.name')} items={items} />
}
