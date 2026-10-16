import { useId } from 'react'
import { useTranslation } from 'react-i18next'
import type { DashboardGroup } from '@/core/services/dashboard/dashboardService'
import { balancesSectionHeadingLook } from '@/features/groups/balances/balancesLooks'
import { homeSectionLook } from '../homeLooks'
import { HomeGroupCard } from './HomeGroupCard'

interface HomeGroupsProps {
  groups: readonly DashboardGroup[]
}

export function HomeGroups({ groups }: HomeGroupsProps) {
  const { t } = useTranslation()
  const headingId = useId()

  return (
    <section aria-labelledby={headingId} className={homeSectionLook}>
      <h2 id={headingId} className={balancesSectionHeadingLook}>
        {t('home.groups')}
      </h2>
      <ul className="grid grid-cols-2 gap-2.5">
        {groups.map((group) => (
          <HomeGroupCard key={group.id} group={group} />
        ))}
      </ul>
    </section>
  )
}
