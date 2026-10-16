import { useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { errorMessageKey } from '@/core/api/errors'
import { useLanguage } from '@/core/i18n/useLanguage'
import type { DashboardPaymentItem } from '@/core/services/dashboard/dashboardService'
import { balancesActionButtonLook } from '@/features/groups/balances/balancesLooks'
import { useAnswerSettlement, type SettlementAnswer } from '@/features/settlements/pending/hooks/useAnswerSettlement'
import { KvitButton } from '@/shared/components/KvitButton'
import { KvitInlineError } from '@/shared/components/KvitInlineError'
import { kvitGlassButtonLook, kvitGlassDangerButtonLook } from '@/shared/components/kvitGlassLooks'
import { cn } from '@/shared/utils/cn'
import { formatMoney } from '@/shared/utils/formatMoney'
import { needsYouCardLook } from '../homeLooks'
import { invalidateDashboard } from '../shared/dashboardCache'
import { NeedsYouCardText } from './NeedsYouCardText'

interface HomePaymentCardProps {
  item: DashboardPaymentItem
}

export function HomePaymentCard({ item }: HomePaymentCardProps) {
  const { t } = useTranslation()
  const { language } = useLanguage()
  const queryClient = useQueryClient()
  const answerSettlement = useAnswerSettlement(item.groupId, item.settlementId)
  const sentence = t('balances.saysPaidYou', {
    from: item.fromName,
    amount: formatMoney(item.amountMinor, item.currency, language),
  })

  function answer(choice: SettlementAnswer) {
    answerSettlement.mutate(choice, { onError: () => void invalidateDashboard(queryClient) })
  }

  return (
    <li className={needsYouCardLook}>
      <NeedsYouCardText groupId={item.groupId} sentence={sentence} groupName={item.groupName} />
      <div className="flex gap-2.5">
        <KvitButton
          type="button"
          className={cn(balancesActionButtonLook, kvitGlassDangerButtonLook, 'flex-1')}
          disabled={answerSettlement.isPending}
          onClick={() => answer('reject')}
        >
          {t('settlements.reject')}
        </KvitButton>
        <KvitButton
          type="button"
          className={cn(balancesActionButtonLook, kvitGlassButtonLook, 'flex-1')}
          disabled={answerSettlement.isPending}
          onClick={() => answer('confirm')}
        >
          {t('settlements.confirm')}
        </KvitButton>
      </div>
      {answerSettlement.isError && <KvitInlineError message={t(errorMessageKey(answerSettlement.error))} />}
    </li>
  )
}
