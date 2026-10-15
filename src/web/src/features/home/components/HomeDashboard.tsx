import { useTranslation } from 'react-i18next'
import { routes } from '@/core/router/routes'
import type { Dashboard } from '@/core/services/dashboard/dashboardService'
import { KvitEmpty } from '@/shared/components/KvitEmpty'
import { KvitLinkButton } from '@/shared/components/KvitLinkButton'
import { HomeActivity } from './HomeActivity'
import { HomeGroups } from './HomeGroups'
import { HomeNeedsYou } from './HomeNeedsYou'
import { HomePlusMenu, type HomePlusMenuTarget } from './HomePlusMenu'
import { HomeTotals } from './HomeTotals'

interface HomeDashboardProps {
  dashboard: Dashboard
}

export function HomeDashboard({ dashboard }: HomeDashboardProps) {
  const { t } = useTranslation()

  if (dashboard.groups.length === 0) {
    return (
      <div className="flex flex-col gap-2 pt-6">
        <KvitEmpty emoji="👋" title={t('home.noGroupsTitle')} message={t('home.noGroupsHint')} />
        <KvitLinkButton to={routes.newGroup}>{t('groups.newButton')}</KvitLinkButton>
      </div>
    )
  }
  return (
    <>
      <HomeTotals totals={dashboard.totals} />
      <HomeNeedsYou items={dashboard.needsYou} />
      <HomeGroups groups={dashboard.groups} />
      <HomeActivity rows={dashboard.recentActivity} />
      <HomePlusMenu target={plusMenuTargetOf(dashboard)} />
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
