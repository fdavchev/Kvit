import { useTranslation } from 'react-i18next'
import type {
  CustomPersonalCategory,
  PersonalCategoryFields,
} from '@/core/services/personalCategories/personalCategoriesService'
import { useKvitSheet } from '@/shared/components/useKvitSheet'
import { useUpdatePersonalCategory } from '../hooks/useUpdatePersonalCategory'
import { CategoryFieldsSheet } from './CategoryFieldsSheet'

interface EditCategorySheetProps {
  category: CustomPersonalCategory
  onClose: () => void
}

export function EditCategorySheet({ category, onClose }: EditCategorySheetProps) {
  const { t } = useTranslation()
  const sheet = useKvitSheet()
  const update = useUpdatePersonalCategory(category.id)

  function submit(fields: PersonalCategoryFields): void {
    update.save(fields, sheet.close)
  }

  return (
    <CategoryFieldsSheet
      title={t('budget.categories.editTitle')}
      initialFields={{ name: category.name, emoji: category.emoji, color: category.color }}
      actionsRef={sheet.actionsRef}
      isPending={update.isPending}
      error={update.error}
      onSubmit={submit}
      onClose={onClose}
    />
  )
}
