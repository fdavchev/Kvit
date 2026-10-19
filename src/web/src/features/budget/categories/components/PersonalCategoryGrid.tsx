import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { errorMessageKey } from '@/core/api/errors'
import type { PersonalCategory } from '@/core/services/personalCategories/personalCategoriesService'
import { KvitButton } from '@/shared/components/KvitButton'
import { KvitCategoryChoice, kvitCategoryGridLook } from '@/shared/components/KvitCategoryChoice'
import { KvitEmojiTile } from '@/shared/components/KvitEmojiTile'
import { KvitError } from '@/shared/components/KvitError'
import { KvitLoading } from '@/shared/components/KvitLoading'
import { KvitSkeleton } from '@/shared/components/KvitSkeleton'
import { categoryColor } from '@/shared/utils/categoryLooks'
import { usePersonalCategories } from '../hooks/usePersonalCategories'
import { personalCategoryName } from '../personalCategoryName'
import { MyCategoriesSheet } from './MyCategoriesSheet'
import { NewCategorySheet } from './NewCategorySheet'

interface PersonalCategoryGridProps {
  onPick: (category: PersonalCategory) => void
}

type OpenSheet = 'new' | 'mine' | null

const skeletonTileCount = 8

export function PersonalCategoryGrid({ onPick }: PersonalCategoryGridProps) {
  const { t } = useTranslation()
  const categoriesQuery = usePersonalCategories()
  const [openSheet, setOpenSheet] = useState<OpenSheet>(null)

  function renderGrid() {
    if (categoriesQuery.isError) {
      return (
        <KvitError
          message={t(errorMessageKey(categoriesQuery.error))}
          onRetry={() => void categoriesQuery.refetch()}
        />
      )
    }
    if (categoriesQuery.isPending) {
      return (
        <KvitLoading>
          <div className={kvitCategoryGridLook}>
            {Array.from({ length: skeletonTileCount }, (_, index) => (
              <div key={index} className="flex flex-col items-center gap-1 py-1">
                <KvitSkeleton className="size-11 rounded-[14px]" />
                <KvitSkeleton className="h-3 w-12 rounded-full" />
              </div>
            ))}
          </div>
        </KvitLoading>
      )
    }
    return (
      <>
        <div className={kvitCategoryGridLook}>
          {categoriesQuery.data.map((category) => (
            <KvitCategoryChoice
              key={category.id}
              label={personalCategoryName(category, t)}
              tile={<KvitEmojiTile emoji={category.emoji} background={categoryColor(category)} />}
              onClick={() => onPick(category)}
            />
          ))}
          <KvitCategoryChoice
            label={t('budget.categories.new')}
            tile={<KvitEmojiTile emoji="+" variant="dashed" />}
            onClick={() => setOpenSheet('new')}
          />
        </div>
        <div className="flex justify-center">
          <KvitButton variant="smallLink" className="w-auto min-w-11 px-3" onClick={() => setOpenSheet('mine')}>
            {t('budget.categories.editMine')}
          </KvitButton>
        </div>
      </>
    )
  }

  return (
    <div className="flex flex-col gap-2">
      {renderGrid()}
      {openSheet === 'new' && <NewCategorySheet onCreated={onPick} onClose={() => setOpenSheet(null)} />}
      {openSheet === 'mine' && <MyCategoriesSheet onClose={() => setOpenSheet(null)} />}
    </div>
  )
}
