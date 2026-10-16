import { useTranslation } from 'react-i18next'
import { useIsGuest } from '@/core/auth/useIsGuest'
import { routes } from '@/core/router/routes'
import type { Dashboard } from '@/core/services/dashboard/dashboardService'
import { balancesCardLook } from '@/features/groups/balances/balancesLooks'
import { KvitEmpty } from '@/shared/components/KvitEmpty'
import { KvitLinkButton } from '@/shared/components/KvitLinkButton'
import { useIsDesktop } from '@/shared/hooks/useIsDesktop'
import { cn } from '@/shared/utils/cn'
import type { HomePlusMenuTarget } from '../homePlusItems'
import { HomeActions } from './HomeActions'
import { HomeActivity } from './HomeActivity'
import { HomeGroups } from './HomeGroups'
import { HomeNeedsYou } from './HomeNeedsYou'
import { HomePlusMenu } from './HomePlusMenu'
import { HomeTotals } from './HomeTotals'

interface HomeDashboardProps {
  dashboard: Dashboard
}

export function HomeDashboard({ dashboard }: HomeDashboardProps) {
  const { t } = useTranslation()
  const isDesktop = useIsDesktop()
  const isGuest = useIsGuest()

  if (dashboard.groups.length === 0 && dashboard.recentActivity.length === 0) {
    return (
      <div className="flex flex-col gap-2 pt-6">
        <KvitEmpty
          emoji="👋"
          title={t('home.noGroupsTitle')}
          message={isGuest ? t('home.noGroupsHintGuest') : t('home.noGroupsHint')}
        />
        {!isGuest && <KvitLinkButton to={routes.newGroup}>{t('groups.newButton')}</KvitLinkButton>}
      </div>
    )
  }
  if (dashboard.groups.length === 0) {
    return (
      <>
        <div className={cn(balancesCardLook, 'flex flex-col gap-2.5')}>
          <p className="font-bold">{t('home.noOpenGroupsTitle')}</p>
          {!isGuest && <KvitLinkButton to={routes.newGroup}>{t('groups.newButton')}</KvitLinkButton>}
        </div>
        <HomeActivity rows={dashboard.recentActivity} />
      </>
    )
  }
  const plusTarget = plusMenuTargetOf(dashboard)
  return (
    <>
      <HomeTotals totals={dashboard.totals} />
      {isDesktop && <HomeActions target={plusTarget} />}
      <HomeNeedsYou items={dashboard.needsYou} />
      <div className="contents lg:grid lg:grid-cols-[repeat(auto-fit,minmax(min(100%,26rem),1fr))] lg:items-start lg:gap-x-[clamp(1.5rem,3vw,3rem)]">
        <HomeGroups groups={dashboard.groups} />
        <HomeActivity rows={dashboard.recentActivity} />
      </div>
      {!isDesktop && <HomePlusMenu target={plusTarget} />}
    </>
  )
}

function plusMenuTargetOf(dashboard: Dashboard): HomePlusMenuTarget | null {
  if (dashboard.plusTarget === null) {
    return null
  }
  const { groupId } = dashboard.plusTarget
  const group = dashboard.groups.find((candidate) => candidate.id === groupId)
  if (group === undefined) {
    throw new Error(`The "+" target group ${groupId} is not one of the groups on the dashboard`)
  }
  return { groupId, groupName: group.name }
}
