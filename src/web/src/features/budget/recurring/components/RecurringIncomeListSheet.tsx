import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import type { RecurringIncome } from '@/core/services/recurringIncomes/recurringIncomesService'
import { KvitEmpty } from '@/shared/components/KvitEmpty'
import { KvitSheet } from '@/shared/components/KvitSheet'
import { useKvitSheet } from '@/shared/components/useKvitSheet'
import { useSaveOnce } from '../../categories/hooks/useSaveOnce'
import { useStopRecurringIncome } from '../hooks/useStopRecurringIncome'
import { RecurringIncomeList } from './RecurringIncomeList'

interface RecurringIncomeListSheetProps {
  incomes: readonly RecurringIncome[]
  today: string
  onClose: (editedIncome: RecurringIncome | null) => void
}

export function RecurringIncomeListSheet({ incomes, today, onClose }: RecurringIncomeListSheetProps) {
  const { t } = useTranslation()
  const sheet = useKvitSheet()
  const oneAtATime = useSaveOnce()
  const stopper = useStopRecurringIncome(oneAtATime)
  const [editedIncome, setEditedIncome] = useState<RecurringIncome | null>(null)

  function edit(income: RecurringIncome): void {
    if (editedIncome !== null) {
      return
    }
    setEditedIncome(income)
    sheet.close()
  }

  return (
    <KvitSheet title={t('budget.recurring.title')} onClose={() => onClose(editedIncome)} actionsRef={sheet.actionsRef}>
      {incomes.length === 0 ? (
        <KvitEmpty message={t('budget.recurring.empty')} />
      ) : (
        <RecurringIncomeList
          incomes={incomes}
          today={today}
          isStopping={stopper.isStopping}
          onEdit={edit}
          onStop={(income) => stopper.stop(income.id)}
        />
      )}
    </KvitSheet>
  )
}
