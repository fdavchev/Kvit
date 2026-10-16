import { useId } from 'react'
import { useTranslation } from 'react-i18next'
import type { DashboardGroup } from '@/core/services/dashboard/dashboardService'
import { balancesSectionHeadingLook } from '@/features/groups/balances/balancesLooks'
import { useListAnimation } from '@/shared/hooks/useListAnimation'
import { homeSectionLook } from '../homeLooks'
import { HomeGroupCard } from './HomeGroupCard'

interface HomeGroupsProps {
  groups: readonly DashboardGroup[]
}

export function HomeGroups({ groups }: HomeGroupsProps) {
  const { t } = useTranslation()
  const headingId = useId()
  const listRef = useListAnimation<HTMLUListElement>()

  return (
    <section aria-labelledby={headingId} className={homeSectionLook}>
      <h2 id={headingId} className={balancesSectionHeadingLook}>
        {t('home.groups')}
      </h2>
      <ul
        ref={listRef}
        className="grid grid-cols-2 gap-2.5 lg:grid-cols-[repeat(auto-fill,minmax(min(100%,12rem),1fr))] lg:gap-[clamp(0.7rem,1.4vw,1.2rem)]"
      >
        {groups.map((group) => (
          <HomeGroupCard key={group.id} group={group} />
        ))}
      </ul>
    </section>
  )
}
