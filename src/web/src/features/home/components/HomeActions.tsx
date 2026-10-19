import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { useIsGuest } from '@/core/auth/useIsGuest'
import { PersonalSpendingSheet } from '@/features/budget/entries/components/PersonalSpendingSheet'
import { kvitRoundActionLooks } from '@/shared/components/kvitRoundActionLooks'
import { homePlusItemKey, homePlusItems, type HomePlusMenuTarget } from '../homePlusItems'

interface HomeActionsProps {
  target: HomePlusMenuTarget | null
}

export function HomeActions({ target }: HomeActionsProps) {
  const { t } = useTranslation()
  const isGuest = useIsGuest()
  const [isSpendingOpen, setIsSpendingOpen] = useState<boolean>(false)

  return (
    <div className="mt-[clamp(1.25rem,2.5vw,2rem)] flex flex-wrap gap-3">
      {homePlusItems(target, isGuest, t).map((item) => {
        const content = (
          <>
            <span aria-hidden="true">{item.icon}</span>
            <span className="wrap-anywhere">{item.label}</span>
          </>
        )
        const look = item.isMain ? kvitRoundActionLooks.main : kvitRoundActionLooks.other
        return item.kind === 'link' ? (
          <Link key={homePlusItemKey(item)} to={item.to} className={look}>
            {content}
          </Link>
        ) : (
          <button key={homePlusItemKey(item)} type="button" className={look} onClick={() => setIsSpendingOpen(true)}>
            {content}
          </button>
        )
      })}
      {isSpendingOpen && <PersonalSpendingSheet onClose={() => setIsSpendingOpen(false)} />}
    </div>
  )
}
