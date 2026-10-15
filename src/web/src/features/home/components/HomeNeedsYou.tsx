import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { countedText } from '@/core/i18n/pluralForm'
import { useLanguage } from '@/core/i18n/useLanguage'
import type { DashboardNeedsYouItem } from '@/core/services/dashboard/dashboardService'
import { balancesCardLook } from '@/features/groups/balances/balancesLooks'
import { ObjectSheet } from '@/features/groups/closing/components/ObjectSheet'
import { cn } from '@/shared/utils/cn'
import { HomeClosingCard } from './HomeClosingCard'
import { HomePaymentCard } from './HomePaymentCard'

interface HomeNeedsYouProps {
  items: readonly DashboardNeedsYouItem[]
}

export function HomeNeedsYou({ items }: HomeNeedsYouProps) {
  const { t } = useTranslation()
  const { language } = useLanguage()
  const [isOpen, setIsOpen] = useState<boolean>(false)
  const [objectingGroupId, setObjectingGroupId] = useState<string | null>(null)

  return (
    <div className="mt-3 flex flex-col gap-2.5">
      {items.length === 0 ? (
        <p className={cn(balancesCardLook, 'font-semibold')}>{t('home.nothingNeedsYou')}</p>
      ) : (
        <>
          <button
            type="button"
            aria-expanded={isOpen}
            className="pressable flex min-h-13 w-full items-center gap-2.5 rounded-[20px] bg-primary px-4 py-3 text-left font-extrabold text-primary-foreground hover:bg-primary-hover"
            onClick={() => setIsOpen(!isOpen)}
          >
            <span className="grid h-7 min-w-7 place-items-center rounded-full bg-primary-foreground px-1.5 text-primary">
              {items.length}
            </span>
            <span className="flex-1">{countedText(t, 'home.thingsNeedYou', items.length, language)}</span>
            <span aria-hidden="true">{isOpen ? '▴' : '▾'}</span>
          </button>
          {isOpen && (
            <ul className="flex flex-col gap-2.5">
              {items.map((item) =>
                item.type === 'payment' ? (
                  <HomePaymentCard key={`payment-${item.settlementId}`} item={item} />
                ) : (
                  <HomeClosingCard
                    key={`closing-${item.groupId}`}
                    item={item}
                    onObject={() => setObjectingGroupId(item.groupId)}
                  />
                ),
              )}
            </ul>
          )}
        </>
      )}
      {objectingGroupId !== null && (
        <ObjectSheet groupId={objectingGroupId} onClose={() => setObjectingGroupId(null)} />
      )}
    </div>
  )
}
