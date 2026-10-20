import type { Category } from '@/core/services/categories/categoriesService'
import { KvitEmojiTile } from '@/shared/components/KvitEmojiTile'
import { categoryColor } from '@/shared/utils/categoryLooks'

interface CategoryTileProps {
  category: Category | null
  size?: 'regular' | 'large'
}

export function CategoryTile({ category, size = 'regular' }: CategoryTileProps) {
  if (category === null) {
    return <KvitEmojiTile emoji="" size={size} variant="dashed" />
  }
  return <KvitEmojiTile emoji={category.emoji} size={size} background={categoryColor(category)} />
}
