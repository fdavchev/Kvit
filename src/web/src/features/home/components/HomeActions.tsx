import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { useIsGuest } from '@/core/auth/useIsGuest'
import { kvitRoundActionLooks } from '@/shared/components/kvitRoundActionLooks'
import { homePlusItems, type HomePlusMenuTarget } from '../homePlusItems'

interface HomeActionsProps {
  target: HomePlusMenuTarget | null
}

export function HomeActions({ target }: HomeActionsProps) {
  const { t } = useTranslation()
  const isGuest = useIsGuest()

  return (
    <div className="mt-[clamp(1.25rem,2.5vw,2rem)] flex flex-wrap gap-3">
      {homePlusItems(target, isGuest, t).map((item) => (
        <Link key={item.to} to={item.to} className={item.isMain ? kvitRoundActionLooks.main : kvitRoundActionLooks.other}>
          <span aria-hidden="true">{item.icon}</span>
          <span className="wrap-anywhere">{item.label}</span>
        </Link>
      ))}
    </div>
  )
}
