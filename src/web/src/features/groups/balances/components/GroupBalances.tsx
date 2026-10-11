import { useTranslation } from 'react-i18next'
import { useSignedInMe } from '@/core/auth/useSignedInMe'
import { useLanguage } from '@/core/i18n/useLanguage'
import { useSettlements } from '@/features/settlements/list/hooks/useSettlements'
import { useMembers } from '@/features/groups/members/hooks/useMembers'
import { KvitButton } from '@/shared/components/KvitButton'
import { KvitEmpty } from '@/shared/components/KvitEmpty'
import { KvitLoading } from '@/shared/components/KvitLoading'
import type { Currency } from '@/shared/utils/formatMoney'
import { GroupLoadError } from '../../shared/components/GroupLoadError'
import { balancesView } from '../balancesView'
import { useBalances } from '../hooks/useBalances'
import { BalanceCard } from './BalanceCard'
import { BetweenOthersSection } from './BetweenOthersSection'
import { EveryoneSection } from './EveryoneSection'
import { NeedsYouSection } from './NeedsYouSection'

interface GroupBalancesProps {
  groupId: string
}

export function GroupBalances({ groupId }: GroupBalancesProps) {
  const { t } = useTranslation()
  const { language } = useLanguage()
  const me = useSignedInMe()
  const balancesQuery = useBalances(groupId)
  const settlementsQuery = useSettlements(groupId)
  const membersQuery = useMembers(groupId)

  if (balancesQuery.isError) {
    return <GroupLoadError error={balancesQuery.error} onRetry={() => void balancesQuery.refetch()} />
  }
  if (settlementsQuery.isError) {
    return <GroupLoadError error={settlementsQuery.error} onRetry={() => void settlementsQuery.refetch()} />
  }
  if (membersQuery.isError) {
    return <GroupLoadError error={membersQuery.error} onRetry={() => void membersQuery.refetch()} />
  }
  if (balancesQuery.isPending || settlementsQuery.isPending || membersQuery.isPending) {
    return <KvitLoading />
  }
  const view = balancesView(balancesQuery.data, settlementsQuery.data, membersQuery.data.members)
  const recordAnotherPayment = (
    <div className="flex justify-center">
      <KvitButton variant="underlinedLink" className="w-auto px-2" disabled>
        {t('balances.recordAnother')}
      </KvitButton>
    </div>
  )

  function cardTitle(currency: Currency): string {
    return view.cards.length > 1 ? t('balances.yourBalanceIn', { currency }) : t('balances.yourBalance')
  }

  function renderBelowCards() {
    if (view.isEmpty) {
      return <KvitEmpty message={t('balances.empty')} />
    }
    if (view.isKvit) {
      return (
        <>
          <KvitEmpty title={t('balances.kvitTitle')} message={t('balances.kvitHint')} />
          {recordAnotherPayment}
        </>
      )
    }
    return (
      <>
        {view.betweenOthers.length > 0 && (
          <BetweenOthersSection items={view.betweenOthers} language={language} />
        )}
        {view.hasSuggestedPayments && (
          <p className="mx-1 mt-0.5 mb-3 text-[0.8125rem] text-muted-foreground">{t('balances.simplified')}</p>
        )}
        {view.everyone.length > 0 && <EveryoneSection cards={view.everyone} language={language} />}
        {recordAnotherPayment}
      </>
    )
  }

  return (
    <div className="flex flex-col pb-6">
      {view.needsYou.length > 0 && (
        <NeedsYouSection items={view.needsYou} language={language} timeZone={me.timeZone} now={new Date()} />
      )}
      {view.cards.map((card) => (
        <BalanceCard key={card.currency} title={cardTitle(card.currency)} card={card} language={language} />
      ))}
      {renderBelowCards()}
    </div>
  )
}
