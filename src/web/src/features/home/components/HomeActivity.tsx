import { useId } from 'react'
import { useTranslation } from 'react-i18next'
import { useSignedInMe } from '@/core/auth/useSignedInMe'
import { useLanguage } from '@/core/i18n/useLanguage'
import type { DashboardActivityRow } from '@/core/services/dashboard/dashboardService'
import { balancesSectionHeadingLook } from '@/features/groups/balances/balancesLooks'
import { formatHowLongAgo } from '@/shared/utils/formatDate'
import { dashboardActivityLines } from '../dashboardActivityLines'
import { homeSectionLook } from '../homeLooks'
import { HomeActivityRow } from './HomeActivityRow'

interface HomeActivityProps {
  rows: readonly DashboardActivityRow[]
}

const recentActivityLimit = 5

export function HomeActivity({ rows }: HomeActivityProps) {
  const { t } = useTranslation()
  const { language } = useLanguage()
  const me = useSignedInMe()
  const headingId = useId()

  if (rows.length === 0) {
    return null
  }
  const now = new Date()
  const lines = dashboardActivityLines(rows.slice(0, recentActivityLimit), { t, language })
  return (
    <section aria-labelledby={headingId} className={homeSectionLook}>
      <h2 id={headingId} className={balancesSectionHeadingLook}>
        {t('home.recentActivity')}
      </h2>
      <ul className="flex flex-col">
        {lines.map((line) => (
          <HomeActivityRow
            key={line.key}
            groupId={line.groupId}
            groupName={line.groupName}
            sentence={line.sentence}
            when={formatHowLongAgo(line.createdAt, now, me.timeZone, language, t)}
          />
        ))}
      </ul>
    </section>
  )
}
