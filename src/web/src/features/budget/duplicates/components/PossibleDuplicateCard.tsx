import { useQueryClient } from '@tanstack/react-query'
import { useId, type RefObject } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { errorMessageKey, hasErrorCode } from '@/core/api/errors'
import { useLanguage } from '@/core/i18n/useLanguage'
import type {
  DuplicateAnswer,
  PersonalEntry,
  PossibleDuplicate,
} from '@/core/services/personalEntries/personalEntriesService'
import { expenseDayText } from '@/features/expenses/shared/expenseDayText'
import { balancesCardLook } from '@/features/groups/balances/balancesLooks'
import { KvitButton } from '@/shared/components/KvitButton'
import { cn } from '@/shared/utils/cn'
import { formatMoney } from '@/shared/utils/formatMoney'
import { useSaveOnce } from '../../categories/hooks/useSaveOnce'
import { personalCategoryName } from '../../categories/personalCategoryName'
import { refreshPersonalEntries } from '../../entries/personalEntryCache'
import { useAnswerDuplicate } from '../hooks/useAnswerDuplicate'
import { groupExpenseName, spendingCategoryOf } from '../possibleDuplicates'

const stalePairCodes = ['EXPENSE_NOT_FOUND', 'PERSONAL_ENTRY_NOT_FOUND', 'DUPLICATE_ALREADY_MERGED']

const answerButtonLook = 'min-h-11 flex-1 basis-36 text-base data-disabled:opacity-50'

interface PossibleDuplicateCardProps {
  pair: PossibleDuplicate
  entry: PersonalEntry
  today: string
  sameButtonRef: RefObject<HTMLButtonElement | null>
  onPairDone: () => void
}

export function PossibleDuplicateCard({ pair, entry, today, sameButtonRef, onPairDone }: PossibleDuplicateCardProps) {
  const { t } = useTranslation()
  const { language } = useLanguage()
  const queryClient = useQueryClient()
  const headingId = useId()
  const oneAtATime = useSaveOnce()
  const answer = useAnswerDuplicate(oneAtATime)
  const category = spendingCategoryOf(entry)

  function send(choice: DuplicateAnswer): void {
    answer.run({ pair, answer: choice }, onPairDone, (error) => {
      toast.error(t(errorMessageKey(error)))
      if (stalePairCodes.some((code) => hasErrorCode(error, code))) {
        void refreshPersonalEntries(queryClient)
        onPairDone()
      }
    })
  }

  return (
    <section
      aria-labelledby={headingId}
      className={cn(balancesCardLook, 'mt-5 flex flex-col gap-3 motion-safe:animate-tab-panel-in')}
    >
      <h2 id={headingId} className="text-[1.0625rem] font-bold">
        {t('budget.duplicates.title')}
      </h2>
      <p className="text-[0.9375rem] break-words">
        {t('budget.duplicates.body', {
          entry: pair.entryNote ?? personalCategoryName(category, t),
          entryAmount: formatMoney(pair.entryAmountMinor, pair.currency, language),
          entryDay: expenseDayText(pair.entryDate, today, language, t),
          expense: groupExpenseName(pair.expenseTitle, category, t),
          group: pair.groupName,
          shareAmount: formatMoney(pair.shareMinor, pair.currency, language),
          expenseDay: expenseDayText(pair.expenseDate, today, language, t),
        })}
      </p>
      <div className="flex flex-wrap gap-2">
        <KvitButton
          ref={sameButtonRef}
          focusableWhenDisabled
          disabled={answer.isPending}
          className={answerButtonLook}
          onClick={() => send('Same')}
        >
          {t('budget.duplicates.same')}
        </KvitButton>
        <KvitButton
          variant="secondary"
          focusableWhenDisabled
          disabled={answer.isPending}
          className={answerButtonLook}
          onClick={() => send('New')}
        >
          {t('budget.duplicates.new')}
        </KvitButton>
      </div>
    </section>
  )
}
