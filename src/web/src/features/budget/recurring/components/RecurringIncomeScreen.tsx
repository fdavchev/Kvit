import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { errorMessageKey } from '@/core/api/errors'
import { useSignedInMe } from '@/core/auth/useSignedInMe'
import { routes } from '@/core/router/routes'
import type { RecurringIncome } from '@/core/services/recurringIncomes/recurringIncomesService'
import { KvitBackButton } from '@/shared/components/KvitBackButton'
import { KvitButton } from '@/shared/components/KvitButton'
import { KvitEmpty } from '@/shared/components/KvitEmpty'
import { KvitError } from '@/shared/components/KvitError'
import { KvitLoading } from '@/shared/components/KvitLoading'
import { KvitScreen } from '@/shared/components/KvitScreen'
import { KvitScreenTitle } from '@/shared/components/KvitScreenTitle'
import { todayInTimeZone } from '@/shared/utils/formatDate'
import { useSaveOnce } from '../../categories/hooks/useSaveOnce'
import { PersonalIncomeSheet } from '../../entries/components/PersonalIncomeSheet'
import { useRecurringIncomes } from '../hooks/useRecurringIncomes'
import { useStopRecurringIncome } from '../hooks/useStopRecurringIncome'
import { RecurringIncomeList } from './RecurringIncomeList'

type OpenedSheet = { kind: 'new' } | { kind: 'edit'; income: RecurringIncome }

const repeatEmoji = '\u{1F501}'

export function RecurringIncomeScreen() {
  const { t } = useTranslation()
  const me = useSignedInMe()
  const incomesQuery = useRecurringIncomes()
  const oneAtATime = useSaveOnce()
  const stopper = useStopRecurringIncome(oneAtATime)
  const [openedSheet, setOpenedSheet] = useState<OpenedSheet | null>(null)
  const today = todayInTimeZone(me.timeZone, new Date())

  function renderContent() {
    if (incomesQuery.isPending) {
      return <KvitLoading />
    }
    if (incomesQuery.isError) {
      return (
        <KvitError message={t(errorMessageKey(incomesQuery.error))} onRetry={() => void incomesQuery.refetch()} />
      )
    }
    return (
      <>
        {incomesQuery.data.length === 0 ? (
          <KvitEmpty emoji={repeatEmoji} message={t('budget.recurring.empty')} />
        ) : (
          <RecurringIncomeList
            incomes={incomesQuery.data}
            today={today}
            className="lg:grid lg:grid-cols-2"
            isStopping={stopper.isStopping}
            onEdit={(income) => setOpenedSheet({ kind: 'edit', income })}
            onStop={(income) => stopper.stop(income.id)}
          />
        )}
        <KvitButton className="mt-5 lg:max-w-sm" onClick={() => setOpenedSheet({ kind: 'new' })}>
          {t('budget.recurring.newButton')}
        </KvitButton>
      </>
    )
  }

  return (
    <KvitScreen width="wide">
      <div className="flex min-h-12">
        <KvitBackButton to={routes.budget} />
      </div>
      <KvitScreenTitle>{t('budget.recurring.title')}</KvitScreenTitle>
      {renderContent()}
      {openedSheet?.kind === 'new' && (
        <PersonalIncomeSheet initialFrequency="Monthly" onClose={() => setOpenedSheet(null)} />
      )}
      {openedSheet?.kind === 'edit' && (
        <PersonalIncomeSheet
          key={openedSheet.income.id}
          recurringIncome={openedSheet.income}
          onClose={() => setOpenedSheet(null)}
        />
      )}
    </KvitScreen>
  )
}
