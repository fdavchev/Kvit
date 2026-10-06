import { useTranslation } from 'react-i18next'
import type { Category } from '@/core/services/categories/categoriesService'
import { KvitSheet } from '@/shared/components/KvitSheet'
import { useKvitSheet } from '@/shared/components/useKvitSheet'
import { categoryNameKey } from '../expenseCategories'
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
      <div className="grid grid-cols-5 gap-x-1.5 gap-y-2.5">
        {categories.map((category) => (
          <button
            key={category.id}
            type="button"
            aria-pressed={category.id === categoryId}
            onClick={() => pick(category.id)}
            className="pressable flex flex-col items-center gap-1 rounded-[14px] border-2 border-transparent py-1 text-[0.6875rem] leading-tight font-semibold hover:bg-secondary-hover aria-pressed:border-primary"
          >
            <CategoryTile category={category} />
            <span className="text-center break-words">{t(categoryNameKey(category))}</span>
          </button>
        ))}
      </div>
    </KvitSheet>
  )
}
