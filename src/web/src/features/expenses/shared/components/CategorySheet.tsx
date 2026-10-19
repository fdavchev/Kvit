import { useTranslation } from 'react-i18next'
import type { Category } from '@/core/services/categories/categoriesService'
import { KvitCategoryChoice, kvitCategoryGridLook } from '@/shared/components/KvitCategoryChoice'
import { KvitSheet } from '@/shared/components/KvitSheet'
import { useKvitSheet } from '@/shared/components/useKvitSheet'
import { categoryNameKey } from '@/shared/utils/categoryLooks'
import { CategoryTile } from './CategoryTile'

interface CategorySheetProps {
  categories: readonly Category[]
  categoryId: string | null
  onPick: (categoryId: string) => void
  onClose: () => void
}

export function CategorySheet({ categories, categoryId, onPick, onClose }: CategorySheetProps) {
  const { t } = useTranslation()
  const sheet = useKvitSheet()

  function pick(pickedId: string): void {
    onPick(pickedId)
    sheet.close()
  }

  return (
    <KvitSheet title={t('expense.pickCategory')} onClose={onClose} actionsRef={sheet.actionsRef}>
      <div className={kvitCategoryGridLook}>
        {categories.map((category) => (
          <KvitCategoryChoice
            key={category.id}
            label={t(categoryNameKey(category))}
            tile={<CategoryTile category={category} />}
            isPressed={category.id === categoryId}
            onClick={() => pick(category.id)}
          />
        ))}
      </div>
    </KvitSheet>
  )
}
