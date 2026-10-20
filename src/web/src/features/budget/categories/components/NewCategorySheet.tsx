import { useRef } from 'react'
import { useTranslation } from 'react-i18next'
import {
  personalCategoryColors,
  type PersonalCategory,
  type PersonalCategoryFields,
} from '@/core/services/personalCategories/personalCategoriesService'
import { useKvitSheet } from '@/shared/components/useKvitSheet'
import { useCreatePersonalCategory } from '../hooks/useCreatePersonalCategory'
import { defaultPersonalCategoryEmoji } from '../personalCategoryEmojis'
import { CategoryFieldsSheet } from './CategoryFieldsSheet'

interface NewCategorySheetProps {
  onCreated: (category: PersonalCategory) => void
  onClose: () => void
}

const newCategoryFields: PersonalCategoryFields = {
  name: '',
  emoji: defaultPersonalCategoryEmoji,
  color: personalCategoryColors[0],
}

export function NewCategorySheet({ onCreated, onClose }: NewCategorySheetProps) {
  const { t } = useTranslation()
  const sheet = useKvitSheet()
  const create = useCreatePersonalCategory()
  const hasHandedOnRef = useRef<boolean>(false)

  function submit(fields: PersonalCategoryFields): void {
    create.save(fields, (category) => {
      hasHandedOnRef.current = true
      sheet.close()
      onCreated(category)
    })
  }

  return (
    <CategoryFieldsSheet
      title={t('budget.categories.newTitle')}
      initialFields={newCategoryFields}
      actionsRef={sheet.actionsRef}
      isPending={create.isPending}
      error={create.error}
      onSubmit={submit}
      onClose={onClose}
      returnsFocusOnClose={() => !hasHandedOnRef.current}
    />
  )
}
