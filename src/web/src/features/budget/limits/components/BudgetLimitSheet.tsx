import { useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { errorMessageKey, hasErrorCode } from '@/core/api/errors'
import { useLanguage } from '@/core/i18n/useLanguage'
import { AmountField } from '@/features/expenses/shared/components/AmountField'
import { KvitButton } from '@/shared/components/KvitButton'
import { KvitForm } from '@/shared/components/KvitForm'
import { KvitSheet } from '@/shared/components/KvitSheet'
import { useKvitSheet } from '@/shared/components/useKvitSheet'
import { showDangerToast } from '@/shared/toasts/showUndoToast'
import { formatMonthLabel } from '@/shared/utils/calendarMonth'
import { moneyInputText, parseMoneyInput } from '@/shared/utils/parseMoneyInput'
import { useSaveOnce } from '../../categories/hooks/useSaveOnce'
import { personalCategoryName } from '../../categories/personalCategoryName'
import { refreshPersonalEntries } from '../../entries/personalEntryCache'
import type { LimitTarget } from '../budgetLimits'
import { useRemoveLimit } from '../hooks/useRemoveLimit'
import { useSetLimit } from '../hooks/useSetLimit'

interface BudgetLimitSheetProps {
  target: LimitTarget
  viewedMonth: string
  currentMonth: string
  opener?: Element | null
  onClose: () => void
}

const categoryNotFoundCode = 'CATEGORY_NOT_FOUND'
const limitAmountRefusedCode = 'BUDGET_LIMIT_AMOUNT_INVALID'
const noteLook = 'text-[0.9375rem] text-pretty text-muted-foreground'

export function BudgetLimitSheet({ target, viewedMonth, currentMonth, opener, onClose }: BudgetLimitSheetProps) {
  const { t } = useTranslation()
  const { language } = useLanguage()
  const queryClient = useQueryClient()
  const sheet = useKvitSheet()
  const oneAtATime = useSaveOnce()
  const setLimit = useSetLimit(oneAtATime)
  const removeLimit = useRemoveLimit(oneAtATime)
  const [amountText, setAmountText] = useState<string>(() =>
    target.limitMinor === null ? '' : moneyInputText(target.limitMinor, 'MKD'),
  )
  const [isAmountChangedSinceSave, setIsAmountChangedSinceSave] = useState<boolean>(false)

  const categoryId = target.category?.id ?? null
  const name = target.category === null ? t('budget.limits.wholeMonth') : personalCategoryName(target.category, t)
  const amountMinor = parseMoneyInput(amountText, 'MKD')
  const isBusy = setLimit.isPending || removeLimit.isPending
  const shownError =
    setLimit.error !== null && !hasErrorCode(setLimit.error, categoryNotFoundCode) ? setLimit.error : null
  const isAmountRefused = hasErrorCode(shownError, limitAmountRefusedCode) && !isAmountChangedSinceSave

  function closeBecauseGone(error: Error): void {
    toast.error(t(errorMessageKey(error)))
    void refreshPersonalEntries(queryClient)
    sheet.close()
  }

  function save(): void {
    if (amountMinor === null) {
      throw new Error(`Save was pressed without a valid limit ("${amountText}")`)
    }
    setIsAmountChangedSinceSave(false)
    setLimit.run(
      { categoryId, limitMinor: amountMinor },
      () => {
        toast.success(t('groupSettings.saved'))
        sheet.close()
      },
      (error) => {
        if (hasErrorCode(error, categoryNotFoundCode)) {
          closeBecauseGone(error)
        }
      },
    )
  }

  function remove(): void {
    removeLimit.run(
      { categoryId },
      () => {
        showDangerToast(t('budget.limits.removed'))
        sheet.close()
      },
      (error) => {
        toast.error(t(errorMessageKey(error)))
      },
    )
  }

  return (
    <KvitSheet
      title={t('budget.limits.sheetTitle', { name })}
      onClose={onClose}
      actionsRef={sheet.actionsRef}
      opener={opener}
    >
      <KvitForm
        submitLabel={t('groupSettings.save')}
        isPending={isBusy}
        canSubmit={amountMinor !== null && amountMinor !== target.limitMinor}
        errorMessage={shownError === null ? null : t(errorMessageKey(shownError))}
        onSubmit={save}
        footer={
          target.limitMinor !== null && (
            <KvitButton variant="danger" disabled={isBusy} onClick={remove}>
              {t('budget.limits.remove')}
            </KvitButton>
          )
        }
      >
        <AmountField
          amountText={amountText}
          currency="MKD"
          invalid={isAmountRefused}
          onAmountTextChange={(nextAmountText) => {
            setAmountText(nextAmountText)
            setIsAmountChangedSinceSave(true)
          }}
        />
        <p className={noteLook}>{t('budget.limits.appliesFromNow')}</p>
        {viewedMonth !== currentMonth && (
          <p className={noteLook}>
            {t('budget.limits.viewingPast', { month: formatMonthLabel(viewedMonth, currentMonth, language) })}
          </p>
        )}
      </KvitForm>
    </KvitSheet>
  )
}
