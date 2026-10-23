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

function isStillRunning(income: RecurringIncome, today: string): boolean {
  return income.endsOn === null || income.endsOn >= today
}

export function RecurringIncomeListSheet({ incomes, today, onClose }: RecurringIncomeListSheetProps) {
  const { t } = useTranslation()
  const sheet = useKvitSheet()
  const oneAtATime = useSaveOnce()
  const stopper = useStopRecurringIncome(oneAtATime)
  const [editedIncome, setEditedIncome] = useState<RecurringIncome | null>(null)
  const [incomesWhileClosing, setIncomesWhileClosing] = useState<readonly RecurringIncome[] | null>(null)
  const shownIncomes = incomesWhileClosing ?? incomes

  function edit(income: RecurringIncome): void {
    if (editedIncome !== null) {
      return
    }
    setEditedIncome(income)
    sheet.close()
  }

  function stop(income: RecurringIncome): void {
    const isLastRunning = !incomes.some((other) => other.id !== income.id && isStillRunning(other, today))
    if (!isLastRunning) {
      stopper.stop(income.id)
      return
    }
    stopper.stop(income.id, () => {
      setIncomesWhileClosing(incomes)
      sheet.close()
    })
  }

  return (
    <KvitSheet
      title={t('budget.recurring.title')}
      onClose={() => onClose(editedIncome)}
      actionsRef={sheet.actionsRef}
      returnsFocusOnClose={() => editedIncome === null}
    >
      {shownIncomes.length === 0 ? (
        <KvitEmpty message={t('budget.recurring.empty')} />
      ) : (
        <RecurringIncomeList
          incomes={shownIncomes}
          today={today}
          isStopping={stopper.isStopping}
          onEdit={edit}
          onStop={stop}
        />
      )}
    </KvitSheet>
  )
}
