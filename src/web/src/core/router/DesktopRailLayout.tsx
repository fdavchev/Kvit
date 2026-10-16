import { useTranslation } from 'react-i18next'
import { Outlet } from 'react-router'
import { useLoadedDashboardGroups } from '@/features/home/hooks/useLoadedDashboardGroups'
import { KvitRail, type KvitRailItem } from '@/shared/components/KvitRail'
import { useIsDesktop } from '@/shared/hooks/useIsDesktop'
import { routes } from './routes'
import { useMainNavItems } from './useMainNavItems'

export function DesktopRailLayout() {
  const isDesktop = useIsDesktop()

  return (
    <div className="contents lg:grid lg:min-h-dvh lg:grid-cols-[clamp(11rem,17vw,16rem)_minmax(0,1fr)]">
      {isDesktop && <SignedInRail />}
      <div className="contents lg:relative lg:block lg:min-w-0">
        <Outlet />
      </div>
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
