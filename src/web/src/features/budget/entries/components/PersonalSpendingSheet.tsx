import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import type { PersonalCategory } from '@/core/services/personalCategories/personalCategoriesService'
import type { PersonalEntry } from '@/core/services/personalEntries/personalEntriesService'
import { KvitSheet } from '@/shared/components/KvitSheet'
import { PersonalCategoryGrid } from '../../categories/components/PersonalCategoryGrid'
import { usePersonalEntryForm } from '../hooks/usePersonalEntryForm'
import { personalEntryChipLook } from '../personalEntryChipLook'
import { PersonalEntryForm } from './PersonalEntryForm'
import { SpendingCategoryHeading } from './SpendingCategoryHeading'

interface PersonalSpendingSheetProps {
  entry?: PersonalEntry
  onClose: () => void
}

export function PersonalSpendingSheet({ entry, onClose }: PersonalSpendingSheetProps) {
  const { t } = useTranslation()
  const form = usePersonalEntryForm(entry)
  const [category, setCategory] = useState<PersonalCategory | null>(entry?.category ?? null)
  const [isPickingCategory, setIsPickingCategory] = useState<boolean>(false)

  function pickCategory(picked: PersonalCategory): void {
    setCategory(picked)
    setIsPickingCategory(false)
    form.markChanged('category')
  }

  function submit(): void {
    if (category === null) {
      throw new Error('Save was pressed without a category')
    }
    form.save({ kind: 'Spending', categoryId: category.id })
  }

  const isShowingGrid = category === null || isPickingCategory

  return (
    <KvitSheet
      title={
        isShowingGrid ? (
          t('budget.entries.pickCategory')
        ) : (
          <SpendingCategoryHeading category={category} invalid={form.isRefused('category') && entry === undefined} />
        )
      }
      onClose={onClose}
      actionsRef={form.sheet.actionsRef}
    >
      {isShowingGrid ? (
        <PersonalCategoryGrid onPick={pickCategory} />
      ) : (
        <PersonalEntryForm
          form={form}
          onSubmit={submit}
          extraChip={
            entry !== undefined && (
              <button
                type="button"
                aria-invalid={form.isRefused('category')}
                disabled={form.isBusy}
                className={personalEntryChipLook}
                onClick={() => setIsPickingCategory(true)}
              >
                <span aria-hidden="true">🏷️</span>
                {t('expense.category')}
              </button>
            )
          }
        />
      )}
    </KvitSheet>
  )
}
