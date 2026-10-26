import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { errorMessageKey } from '@/core/api/errors'
import type { PersonalCategory } from '@/core/services/personalCategories/personalCategoriesService'
import type { MonthLimits } from '@/core/services/personalEntries/personalEntriesService'
import { KvitCategoryChoice, kvitCategoryGridLook } from '@/shared/components/KvitCategoryChoice'
import { KvitEmojiTile } from '@/shared/components/KvitEmojiTile'
import { KvitError } from '@/shared/components/KvitError'
import { KvitLoading } from '@/shared/components/KvitLoading'
import { KvitSheet } from '@/shared/components/KvitSheet'
import { useKvitSheet } from '@/shared/components/useKvitSheet'
import { categoryColor } from '@/shared/utils/categoryLooks'
import { usePersonalCategories } from '../../categories/hooks/usePersonalCategories'
import { personalCategoryName } from '../../categories/personalCategoryName'
import { limitMinorOf, wholeMonthEmoji, type LimitTarget } from '../budgetLimits'

interface BudgetLimitChoiceSheetProps {
  limits: MonthLimits
  onClose: (chosen: LimitTarget | null) => void
}

export function BudgetLimitChoiceSheet({ limits, onClose }: BudgetLimitChoiceSheetProps) {
  const { t } = useTranslation()
  const sheet = useKvitSheet()
  const categoriesQuery = usePersonalCategories()
  const [chosen, setChosen] = useState<LimitTarget | null>(null)

  function choose(category: PersonalCategory | null): void {
    if (chosen !== null) {
      return
    }
    setChosen({ category, limitMinor: limitMinorOf(limits, category?.id ?? null) })
    sheet.close()
  }

  function renderChoices() {
    if (categoriesQuery.isError) {
      return (
        <KvitError message={t(errorMessageKey(categoriesQuery.error))} onRetry={() => void categoriesQuery.refetch()} />
      )
    }
    if (categoriesQuery.isPending) {
      return <KvitLoading />
    }
    return (
      <div className={kvitCategoryGridLook}>
        <KvitCategoryChoice
          label={t('budget.limits.wholeMonth')}
          tile={<KvitEmojiTile emoji={wholeMonthEmoji} />}
          onClick={() => choose(null)}
        />
        {categoriesQuery.data.map((category) => (
          <KvitCategoryChoice
            key={category.id}
            label={personalCategoryName(category, t)}
            tile={<KvitEmojiTile emoji={category.emoji} background={categoryColor(category)} />}
            onClick={() => choose(category)}
          />
        ))}
      </div>
    )
  }

  return (
    <KvitSheet
      title={t('budget.limits.chooseTitle')}
      onClose={() => onClose(chosen)}
      actionsRef={sheet.actionsRef}
      returnsFocusOnClose={() => chosen === null}
    >
      {renderChoices()}
    </KvitSheet>
  )
}
