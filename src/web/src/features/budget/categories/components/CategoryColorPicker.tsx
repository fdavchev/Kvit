import { useId } from 'react'
import { useTranslation } from 'react-i18next'
import {
  personalCategoryColors,
  type PersonalCategoryColor,
} from '@/core/services/personalCategories/personalCategoriesService'
import { categoryColor } from '@/shared/utils/categoryLooks'

interface CategoryColorPickerProps {
  value: PersonalCategoryColor
  invalid: boolean
  onChange: (color: PersonalCategoryColor) => void
}

export function CategoryColorPicker({ value, invalid, onChange }: CategoryColorPickerProps) {
  const { t } = useTranslation()
  const labelId = useId()

  return (
    <div className="flex flex-col">
      <span id={labelId} className="mb-2 text-[0.9375rem] font-bold">
        {t('budget.categories.color')}
      </span>
      <div
        role="group"
        aria-labelledby={labelId}
        aria-invalid={invalid}
        className="grid grid-cols-5 justify-items-center gap-y-1 rounded-[14px] border-2 border-transparent py-1 aria-invalid:border-destructive"
      >
        {personalCategoryColors.map((color) => {
          const isChosen: boolean = color === value
          return (
            <button
              key={color}
              type="button"
              aria-pressed={isChosen}
              aria-label={t(`budget.categories.colors.${color}`)}
              onClick={() => onChange(color)}
              className="pressable group grid size-11 place-items-center rounded-full"
            >
              <span
                aria-hidden="true"
                className="grid size-9 place-items-center rounded-full text-[1.0625rem] leading-none font-extrabold text-white group-aria-pressed:outline-3 group-aria-pressed:outline-offset-2 group-aria-pressed:outline-foreground"
                style={{ backgroundColor: categoryColor({ color }) }}
              >
                {isChosen ? '✓' : ''}
              </span>
            </button>
          )
        })}
      </div>
    </div>
  )
}
