import { useTranslation } from 'react-i18next'
import type { PersonalCategory } from '@/core/services/personalCategories/personalCategoriesService'
import { KvitEmojiTile } from '@/shared/components/KvitEmojiTile'
import { categoryColor } from '@/shared/utils/categoryLooks'
import { cn } from '@/shared/utils/cn'
import { personalCategoryName } from '../../categories/personalCategoryName'

interface SpendingCategoryHeadingProps {
  category: PersonalCategory
  invalid: boolean
}

export function SpendingCategoryHeading({ category, invalid }: SpendingCategoryHeadingProps) {
  const { t } = useTranslation()

  return (
    <span
      data-category-heading=""
      data-invalid={invalid ? 'true' : undefined}
      className={cn(
        '-m-1 flex items-center gap-2.5 rounded-[18px] border-2 border-transparent p-1',
        invalid && 'border-destructive',
      )}
    >
      <KvitEmojiTile emoji={category.emoji} background={categoryColor(category)} />
      <span className="min-w-0 wrap-anywhere">{personalCategoryName(category, t)}</span>
    </span>
  )
}
