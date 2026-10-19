import { useState, type RefObject } from 'react'
import { useTranslation } from 'react-i18next'
import { errorMessageKey, hasErrorCode } from '@/core/api/errors'
import type { PersonalCategoryFields } from '@/core/services/personalCategories/personalCategoriesService'
import { KvitChoiceChips } from '@/shared/components/KvitChoiceChips'
import { KvitForm } from '@/shared/components/KvitForm'
import { KvitSheet, type KvitSheetActions } from '@/shared/components/KvitSheet'
import { KvitTextField } from '@/shared/components/KvitTextField'
import { personalCategoryEmojis } from '../personalCategoryEmojis'
import { CategoryColorPicker } from './CategoryColorPicker'

interface CategoryFieldsSheetProps {
  title: string
  initialFields: PersonalCategoryFields
  actionsRef: RefObject<KvitSheetActions | null>
  isPending: boolean
  error: Error | null
  onSubmit: (fields: PersonalCategoryFields) => void
  onClose: () => void
}

type FieldName = keyof PersonalCategoryFields

const nameMaxLength = 30
const emojiMaxLength = 16

const refusedCodesOf: Record<FieldName, readonly string[]> = {
  name: ['CATEGORY_NAME_INVALID', 'CATEGORY_NAME_TAKEN'],
  emoji: ['CATEGORY_EMOJI_INVALID'],
  color: ['CATEGORY_COLOR_INVALID'],
}

const nothingChanged: Record<FieldName, boolean> = { name: false, emoji: false, color: false }

const emojiOptions = personalCategoryEmojis.map((emoji) => ({ value: emoji, label: emoji }))

export function CategoryFieldsSheet({
  title,
  initialFields,
  actionsRef,
  isPending,
  error,
  onSubmit,
  onClose,
}: CategoryFieldsSheetProps) {
  const { t } = useTranslation()
  const [fields, setFields] = useState<PersonalCategoryFields>(initialFields)
  const [changedSinceSave, setChangedSinceSave] = useState<Record<FieldName, boolean>>(nothingChanged)

  function change<Name extends FieldName>(name: Name, value: PersonalCategoryFields[Name]): void {
    setFields((current) => ({ ...current, [name]: value }))
    setChangedSinceSave((current) => ({ ...current, [name]: true }))
  }

  function isRefused(name: FieldName): boolean {
    return refusedCodesOf[name].some((code) => hasErrorCode(error, code)) && !changedSinceSave[name]
  }

  function submit(): void {
    setChangedSinceSave(nothingChanged)
    onSubmit(fields)
  }

  return (
    <KvitSheet title={title} onClose={onClose} actionsRef={actionsRef}>
      <KvitForm
        submitLabel={t('groupSettings.save')}
        isPending={isPending}
        errorMessage={error === null ? null : t(errorMessageKey(error))}
        onSubmit={submit}
      >
        <KvitTextField
          id="personal-category-name"
          label={t('budget.categories.name')}
          value={fields.name}
          onChange={(name) => change('name', name)}
          placeholder={t('budget.categories.namePlaceholder')}
          maxLength={nameMaxLength}
          autoComplete="off"
          enterKeyHint="done"
          autoFocus
          invalid={isRefused('name')}
        />
        <div className="flex flex-col gap-2">
          <KvitTextField
            id="personal-category-emoji"
            label={t('budget.categories.emoji')}
            value={fields.emoji}
            onChange={(emoji) => change('emoji', emoji)}
            maxLength={emojiMaxLength}
            autoComplete="off"
            autoCorrect="off"
            spellCheck={false}
            enterKeyHint="done"
            invalid={isRefused('emoji')}
          />
          <KvitChoiceChips
            label={t('budget.categories.emojiPicks')}
            labelClassName="sr-only"
            options={emojiOptions}
            value={fields.emoji}
            onChange={(emoji) => change('emoji', emoji)}
            chipClassName="min-w-12 flex-none px-2.5 text-[1.3rem]"
            optionsClassName="-mx-1 flex-nowrap overflow-x-auto px-1 pb-1"
          />
        </div>
        <CategoryColorPicker
          value={fields.color}
          invalid={isRefused('color')}
          onChange={(color) => change('color', color)}
        />
      </KvitForm>
    </KvitSheet>
  )
}
