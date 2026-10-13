import type { ReactNode, RefObject } from 'react'
import { useTranslation } from 'react-i18next'
import { AmountField } from '@/features/expenses/shared/components/AmountField'
import { KvitButton } from '@/shared/components/KvitButton'
import { KvitForm } from '@/shared/components/KvitForm'
import { KvitSheet, type KvitSheetActions } from '@/shared/components/KvitSheet'
import { kvitGlassButtonLook } from '@/shared/components/kvitGlassLooks'
import type { Currency } from '@/shared/utils/formatMoney'

interface PaymentSheetProps {
  title: string
  actionsRef: RefObject<KvitSheetActions | null>
  amountText: string
  currency: Currency
  note: string | null
  canSubmit: boolean
  isPending: boolean
  errorMessage: string | null
  choices?: ReactNode
  onAmountTextChange: (amountText: string) => void
  onSubmit: () => void
  onDismiss: () => void
  onClose: () => void
}

export function PaymentSheet({
  title,
  actionsRef,
  amountText,
  currency,
  note,
  canSubmit,
  isPending,
  errorMessage,
  choices,
  onAmountTextChange,
  onSubmit,
  onDismiss,
  onClose,
}: PaymentSheetProps) {
  const { t } = useTranslation()

  return (
    <KvitSheet title={title} onClose={onClose} actionsRef={actionsRef}>
      <KvitForm
        submitLabel={t('groupSettings.save')}
        submitClassName={kvitGlassButtonLook}
        isPending={isPending}
        canSubmit={canSubmit}
        errorMessage={errorMessage}
        onSubmit={onSubmit}
        footer={
          <KvitButton type="button" variant="link" onClick={onDismiss}>
            {t('settlements.cancel')}
          </KvitButton>
        }
      >
        {choices}
        <AmountField amountText={amountText} currency={currency} onAmountTextChange={onAmountTextChange} />
        {note !== null && <p className="text-[0.8125rem] text-muted-foreground">{note}</p>}
      </KvitForm>
    </KvitSheet>
  )
}
