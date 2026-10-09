import type { Category } from '@/core/services/categories/categoriesService'
import { KvitEmojiTile } from '@/shared/components/KvitEmojiTile'
import { cn } from '@/shared/utils/cn'
import { categoryColor } from '../expenseCategories'

interface CategoryTileProps {
  category: Category | null
  size?: 'regular' | 'large'
}

export function CategoryTile({ category, size = 'regular' }: CategoryTileProps) {
  if (category === null) {
    return (
      <span
        aria-hidden="true"
        className={cn(
          'flex-none border-2 border-dashed border-field-border bg-track',
          size === 'large' ? 'size-18 rounded-[22px]' : 'size-11 rounded-[14px]',
        )}
      />
    )
  }
  return <KvitEmojiTile emoji={category.emoji} size={size} background={categoryColor(category)} />
}
