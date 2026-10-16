import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { useLanguage } from '@/core/i18n/useLanguage'
import { routes } from '@/core/router/routes'
import type { DashboardGroup } from '@/core/services/dashboard/dashboardService'
import { amountLookOf } from '@/features/groups/balances/balancesLooks'
import { KvitEmojiTile } from '@/shared/components/KvitEmojiTile'
import { cn } from '@/shared/utils/cn'
import { formatSignedMoney } from '@/shared/utils/formatSignedMoney'

interface HomeGroupCardProps {
  group: DashboardGroup
}

export function HomeGroupCard({ group }: HomeGroupCardProps) {
  const { t } = useTranslation()
  const { language } = useLanguage()

  return (
    <li className="min-w-0">
      <Link
        to={routes.group(group.id)}
        className="pressable flex h-full min-w-0 flex-col items-start gap-2 rounded-[20px] bg-card p-3 text-card-foreground hover:bg-secondary-hover"
      >
        <KvitEmojiTile emoji={group.emoji} />
        <span className="leading-tight font-bold wrap-anywhere">{group.name}</span>
        {group.balances.length === 0 ? (
          <span className="rounded-full bg-pill px-2.5 py-0.5 text-xs font-bold text-foreground">{t('home.kvit')}</span>
        ) : (
          <span className="flex flex-col">
            {group.balances.map((balance) => (
              <span key={balance.currency} className={cn('font-extrabold break-words', amountLookOf(balance.amountMinor))}>
                {formatSignedMoney(balance.amountMinor, balance.currency, language)}
              </span>
            ))}
          </span>
        )}
      </Link>
    </li>
  )
}
