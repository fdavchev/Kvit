import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { errorMessageKey } from '@/core/api/errors'
import { useLanguage } from '@/core/i18n/useLanguage'
import { groupCurrencies } from '@/core/services/groups/groupsService'
import type { GroupMember } from '@/core/services/groups/membersService'
import { KvitChoiceChips } from '@/shared/components/KvitChoiceChips'
import { useKvitSheet } from '@/shared/components/useKvitSheet'
import { currencyLabel, type Currency } from '@/shared/utils/formatMoney'
import { parseMoneyInput } from '@/shared/utils/parseMoneyInput'
import { useSavePayment } from '../hooks/useSavePayment'
import { whenItCountsText } from '../paymentToRecord'
import { PaymentSheet } from './PaymentSheet'

type Direction = 'iPaid' | 'paidMe'

interface RecordPaymentSheetProps {
  groupId: string
  me: GroupMember
  members: readonly GroupMember[]
  defaultCurrency: Currency
  onClose: () => void
}

interface ChosenPayment {
  payerMemberId: string
  payerName: string
  receiverMemberId: string
  receiverName: string
}

export function RecordPaymentSheet({ groupId, me, members, defaultCurrency, onClose }: RecordPaymentSheetProps) {
  const { t } = useTranslation()
  const { language } = useLanguage()
  const sheet = useKvitSheet()
  const savePayment = useSavePayment(groupId, members)
  const [clientRequestId] = useState<string>(() => crypto.randomUUID())
  const [direction, setDirection] = useState<Direction>('iPaid')
  const [personId, setPersonId] = useState<string | null>(null)
  const [currency, setCurrency] = useState<Currency>(defaultCurrency)
  const [amountText, setAmountText] = useState<string>('')
  const others = members.filter((member) => !member.isYou)
  const person = others.find((member) => member.id === personId) ?? null
  const chosen = person === null ? null : chosenPaymentOf(direction, me, person)
  const amountMinor = parseMoneyInput(amountText, currency)

  function chooseDirection(nextDirection: Direction): void {
    setDirection(nextDirection)
    if (others.length === 1) {
      setPersonId(others[0].id)
    }
  }

  function save(): void {
    if (chosen === null || amountMinor === null) {
      throw new Error(`Save was pressed without a person or with an amount that is not valid: "${amountText}"`)
    }
    savePayment.save(
      {
        clientRequestId,
        fromMemberId: chosen.payerMemberId,
        toMemberId: chosen.receiverMemberId,
        amountMinor,
        currency,
      },
      { payerName: chosen.payerName, receiverName: chosen.receiverName },
      sheet.closeIfShown,
    )
  }

  return (
    <PaymentSheet
      title={t('balances.recordAnother')}
      actionsRef={sheet.actionsRef}
      amountText={amountText}
      currency={currency}
      note={
        chosen === null
          ? null
          : whenItCountsText(chosen.payerMemberId, chosen.receiverMemberId, chosen.receiverName, members, t)
      }
      canSubmit={chosen !== null && amountMinor !== null}
      isPending={savePayment.isPending}
      errorMessage={savePayment.error === null ? null : t(errorMessageKey(savePayment.error))}
      choices={
        <>
          <KvitChoiceChips<Direction>
            label={t('settlements.whoPaid')}
            labelClassName="sr-only"
            options={[
              { value: 'iPaid', label: t('settlements.iPaid') },
              { value: 'paidMe', label: t('settlements.someonePaidMe') },
            ]}
            value={direction}
            onChange={chooseDirection}
          />
          <KvitChoiceChips<string>
            label={t('settlements.person')}
            labelClassName="sr-only"
            options={others.map((member) => ({ value: member.id, label: member.displayName }))}
            value={personId ?? ''}
            onChange={setPersonId}
          />
          <KvitChoiceChips<Currency>
            label={t('groupFields.currency')}
            labelClassName="sr-only"
            options={groupCurrencies.map((code) => ({ value: code, label: currencyLabel(code, language) }))}
            value={currency}
            onChange={setCurrency}
          />
        </>
      }
      onAmountTextChange={setAmountText}
      onSubmit={save}
      onDismiss={sheet.close}
      onClose={onClose}
    />
  )
}

function chosenPaymentOf(direction: Direction, me: GroupMember, person: GroupMember): ChosenPayment {
  return direction === 'iPaid'
    ? { payerMemberId: me.id, payerName: me.displayName, receiverMemberId: person.id, receiverName: person.displayName }
    : { payerMemberId: person.id, payerName: person.displayName, receiverMemberId: me.id, receiverName: me.displayName }
}
