import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useLanguage } from '@/core/i18n/useLanguage'
import type { GroupMember } from '@/core/services/groups/membersService'
import { MarkPaidSheet } from '@/features/settlements/record/components/MarkPaidSheet'
import { RecordPaymentSheet } from '@/features/settlements/record/components/RecordPaymentSheet'
import type { PaymentToRecord } from '@/features/settlements/record/paymentToRecord'
import { KvitButton } from '@/shared/components/KvitButton'
import { KvitEmpty } from '@/shared/components/KvitEmpty'
import type { Currency } from '@/shared/utils/formatMoney'
import type { BalancesView } from '../balancesView'
import { BalanceCard } from './BalanceCard'
import { BetweenOthersSection } from './BetweenOthersSection'
import { EveryoneSection } from './EveryoneSection'
import { NeedsYouSection } from './NeedsYouSection'
import { PaymentsSection } from './PaymentsSection'

interface BalancesOverviewProps {
  groupId: string
  view: BalancesView
  members: readonly GroupMember[]
  defaultCurrency: Currency
  timeZone: string
}

type OpenSheet = { kind: 'markPaid'; payment: PaymentToRecord } | { kind: 'recordAnother' } | null

export function BalancesOverview({ groupId, view, members, defaultCurrency, timeZone }: BalancesOverviewProps) {
  const { t } = useTranslation()
  const { language } = useLanguage()
  const [openSheet, setOpenSheet] = useState<OpenSheet>(null)
  const now = new Date()

  function markPaid(payment: PaymentToRecord): void {
    setOpenSheet({ kind: 'markPaid', payment })
  }

  function cardTitle(currency: Currency): string {
    return view.cards.length > 1 ? t('balances.yourBalanceIn', { currency }) : t('balances.yourBalance')
  }

  const paymentsAndRecordButton = (
    <>
      {view.payments.length > 0 && (
        <PaymentsSection groupId={groupId} payments={view.payments} language={language} timeZone={timeZone} now={now} />
      )}
      {view.hasOtherMembers && (
        <div className="flex justify-center">
          <KvitButton
            variant="underlinedLink"
            className="w-auto px-2"
            onClick={() => setOpenSheet({ kind: 'recordAnother' })}
          >
            {t('balances.recordAnother')}
          </KvitButton>
        </div>
      )}
    </>
  )

  function renderBelowCards() {
    if (view.isEmpty) {
      return <KvitEmpty message={t('balances.empty')} />
    }
    if (view.isKvit) {
      return (
        <>
          <KvitEmpty title={t('balances.kvitTitle')} message={t('balances.kvitHint')} />
          {paymentsAndRecordButton}
        </>
      )
    }
    return (
      <>
        {view.betweenOthers.length > 0 && (
          <BetweenOthersSection items={view.betweenOthers} language={language} onMarkPaid={markPaid} />
        )}
        {view.hasSuggestedPayments && (
          <p className="mx-1 mt-0.5 mb-3 text-[0.8125rem] text-muted-foreground">{t('balances.simplified')}</p>
        )}
        {view.everyone.length > 0 && <EveryoneSection cards={view.everyone} language={language} />}
        {paymentsAndRecordButton}
      </>
    )
  }

  return (
    <div className="flex flex-col pb-6">
      {view.needsYou.length > 0 && (
        <NeedsYouSection groupId={groupId} items={view.needsYou} language={language} timeZone={timeZone} now={now} />
      )}
      {view.cards.map((card) => (
        <BalanceCard
          key={card.currency}
          groupId={groupId}
          title={cardTitle(card.currency)}
          card={card}
          language={language}
          onMarkPaid={markPaid}
        />
      ))}
      {renderBelowCards()}
      {openSheet?.kind === 'markPaid' && (
        <MarkPaidSheet
          groupId={groupId}
          members={members}
          payment={openSheet.payment}
          onClose={() => setOpenSheet(null)}
        />
      )}
      {openSheet?.kind === 'recordAnother' && (
        <RecordPaymentSheet
          groupId={groupId}
          me={view.me}
          members={members}
          defaultCurrency={defaultCurrency}
          onClose={() => setOpenSheet(null)}
        />
      )}
    </div>
  )
}
