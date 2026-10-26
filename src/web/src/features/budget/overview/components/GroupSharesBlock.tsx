import { useId } from 'react'
import { useTranslation } from 'react-i18next'
import type { PersonalCategory } from '@/core/services/personalCategories/personalCategoriesService'
import type { GroupShare } from '@/core/services/personalEntries/personalEntriesService'
import { useListAnimation } from '@/shared/hooks/useListAnimation'
import { groupShareCategory, newestGroupSharesFirst } from '../groupShareTotals'
import { GroupShareListItem } from './GroupShareListItem'

interface GroupSharesBlockProps {
  groupShares: readonly GroupShare[]
  categories: readonly PersonalCategory[]
  today: string
}

export function GroupSharesBlock({ groupShares, categories, today }: GroupSharesBlockProps) {
  const { t } = useTranslation()
  const headingId = useId()
  const listRef = useListAnimation<HTMLUListElement>()

  return (
    <section aria-labelledby={headingId} className="mt-5 flex flex-col gap-1.5">
      <h2 id={headingId} className="mx-1 text-[0.8125rem] font-bold text-muted-foreground">
        {t('budget.groupShares.title')}
      </h2>
      <ul
        ref={listRef}
        className="flex flex-col gap-0.5 rounded-[20px] bg-card p-1.5 text-card-foreground"
      >
        {newestGroupSharesFirst(groupShares).map((share, index) => (
          <GroupShareListItem
            key={`${share.expenseId}-${index}`}
            share={share}
            category={groupShareCategory(share, categories)}
            today={today}
          />
        ))}
      </ul>
    </section>
  )
}
