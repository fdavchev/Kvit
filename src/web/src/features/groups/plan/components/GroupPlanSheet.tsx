import { useQueryClient } from '@tanstack/react-query'
import { useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { errorMessageKey, hasErrorCode } from '@/core/api/errors'
import { useLanguage } from '@/core/i18n/useLanguage'
import type { GroupSpendingPlan } from '@/core/services/groups/groupSpendingService'
import { groupCurrencies, type Group } from '@/core/services/groups/groupsService'
import { useSaveOnce } from '@/features/budget/categories/hooks/useSaveOnce'
import { AmountField } from '@/features/expenses/shared/components/AmountField'
import { KvitButton } from '@/shared/components/KvitButton'
import { KvitChoiceChips } from '@/shared/components/KvitChoiceChips'
import { KvitForm } from '@/shared/components/KvitForm'
import { KvitSheet } from '@/shared/components/KvitSheet'
import { useKvitSheet } from '@/shared/components/useKvitSheet'
import { showDangerToast } from '@/shared/toasts/showUndoToast'
import { currencyLabel, type Currency } from '@/shared/utils/formatMoney'
import { moneyInputText, parseMoneyInput } from '@/shared/utils/parseMoneyInput'
import { refreshWholeGroup } from '../../shared/groupCache'
import { useRemoveGroupPlan } from '../hooks/useRemoveGroupPlan'
import { useSetGroupPlan } from '../hooks/useSetGroupPlan'

export type PlanChange = 'set' | 'removed' | 'none'

interface GroupPlanSheetProps {
  group: Group
  plan: GroupSpendingPlan | null
  opener: Element | null
  onClose: (change: PlanChange) => void
}

const groupChangedCodes: readonly string[] = ['GROUP_NOT_OWNER', 'GROUP_FINISHED', 'GROUP_CLOSING']
const planAmountRefusedCode = 'GROUP_PLAN_AMOUNT_INVALID'

function isGroupChangedRefusal(error: Error): boolean {
  return groupChangedCodes.some((code) => hasErrorCode(error, code))
}

export function GroupPlanSheet({ group, plan, opener, onClose }: GroupPlanSheetProps) {
  const { t } = useTranslation()
  const { language } = useLanguage()
  const queryClient = useQueryClient()
  const sheet = useKvitSheet()
  const oneAtATime = useSaveOnce()
  const setPlan = useSetGroupPlan(group.id, oneAtATime)
  const removePlan = useRemoveGroupPlan(group.id, oneAtATime)
  const [amountText, setAmountText] = useState<string>(() =>
    plan === null ? '' : moneyInputText(plan.amountMinor, plan.currency),
  )
  const [currency, setCurrency] = useState<Currency>(() => plan?.currency ?? group.defaultCurrency)
  const [isAmountChangedSinceSave, setIsAmountChangedSinceSave] = useState<boolean>(false)
  const changeRef = useRef<PlanChange>('none')

  const amountMinor = parseMoneyInput(amountText, currency)
  const isChanged = plan === null || amountMinor !== plan.amountMinor || currency !== plan.currency
  const isBusy = setPlan.isPending || removePlan.isPending
  const shownError = setPlan.error !== null && !isGroupChangedRefusal(setPlan.error) ? setPlan.error : null
  const isAmountRefused = hasErrorCode(shownError, planAmountRefusedCode) && !isAmountChangedSinceSave

  function closeBecauseGroupChanged(error: Error): void {
    toast.error(t(errorMessageKey(error)))
    void refreshWholeGroup(queryClient, group.id)
    sheet.close()
  }

  function save(): void {
    if (amountMinor === null) {
      throw new Error(`Save was pressed without a valid plan amount ("${amountText}" in ${currency})`)
    }
    setIsAmountChangedSinceSave(false)
    setPlan.run(
      { amountMinor, currency },
      () => {
        toast.success(t('groupSettings.saved'))
        changeRef.current = 'set'
        sheet.close()
      },
      (error) => {
        if (isGroupChangedRefusal(error)) {
          closeBecauseGroupChanged(error)
        }
      },
    )
  }

  function remove(): void {
    removePlan.run(
      undefined,
      () => {
        showDangerToast(t('groupPlan.removed'))
        changeRef.current = 'removed'
        sheet.close()
      },
      (error) => {
        toast.error(t(errorMessageKey(error)))
      },
    )
  }

  return (
    <KvitSheet
      title={t('groupPlan.title')}
      onClose={() => onClose(changeRef.current)}
      actionsRef={sheet.actionsRef}
      opener={opener}
    >
      <KvitForm
        submitLabel={t('groupSettings.save')}
        isPending={isBusy}
        canSubmit={amountMinor !== null && isChanged}
        errorMessage={shownError === null ? null : t(errorMessageKey(shownError))}
        onSubmit={save}
        footer={
          plan !== null && (
            <KvitButton variant="danger" disabled={isBusy} onClick={remove}>
              {t('groupPlan.remove')}
            </KvitButton>
          )
        }
      >
        <AmountField
          amountText={amountText}
          currency={currency}
          invalid={isAmountRefused}
          invalidMessage={t(currency === 'MKD' ? 'groupPlan.amountInvalidMkd' : 'groupPlan.amountInvalidEur')}
          onAmountTextChange={(nextAmountText) => {
            setAmountText(nextAmountText)
            setIsAmountChangedSinceSave(true)
          }}
        />
        <KvitChoiceChips<Currency>
          label={t('groupFields.currency')}
          labelClassName="sr-only"
          options={groupCurrencies.map((code) => ({ value: code, label: currencyLabel(code, language) }))}
          value={currency}
          onChange={(nextCurrency) => {
            setCurrency(nextCurrency)
            setIsAmountChangedSinceSave(true)
          }}
        />
        <p className="text-[0.9375rem] text-pretty text-muted-foreground">{t('groupPlan.hint')}</p>
      </KvitForm>
    </KvitSheet>
  )
}
