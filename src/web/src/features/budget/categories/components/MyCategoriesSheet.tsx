import { useId, useState, type ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { errorMessageKey } from '@/core/api/errors'
import type {
  CustomPersonalCategory,
  PersonalCategory,
} from '@/core/services/personalCategories/personalCategoriesService'
import { KvitButton } from '@/shared/components/KvitButton'
import { KvitEmojiTile } from '@/shared/components/KvitEmojiTile'
import { KvitEmpty } from '@/shared/components/KvitEmpty'
import { KvitError } from '@/shared/components/KvitError'
import { KvitLoading } from '@/shared/components/KvitLoading'
import { KvitSheet } from '@/shared/components/KvitSheet'
import { KvitSkeleton } from '@/shared/components/KvitSkeleton'
import { useKvitSheet } from '@/shared/components/useKvitSheet'
import { useListAnimation } from '@/shared/hooks/useListAnimation'
import { showUndoToast } from '@/shared/toasts/showUndoToast'
import { categoryColor } from '@/shared/utils/categoryLooks'
import { cn } from '@/shared/utils/cn'
import { useArchivePersonalCategory } from '../hooks/useArchivePersonalCategory'
import { usePersonalCategoriesWithArchived } from '../hooks/usePersonalCategoriesWithArchived'
import { useUnarchivePersonalCategory } from '../hooks/useUnarchivePersonalCategory'
import { EditCategorySheet } from './EditCategorySheet'

interface MyCategoriesSheetProps {
  onClose: () => void
}

interface CategoryRowProps {
  category: CustomPersonalCategory
  isArchived: boolean
  children: ReactNode
}

const rowButtonLook = 'min-h-11 w-auto min-w-11 flex-none px-2'

function isOwn(category: PersonalCategory): category is CustomPersonalCategory {
  return category.isCustom
}

function CategoryRow({ category, isArchived, children }: CategoryRowProps) {
  return (
    <li className="flex items-center gap-3 py-1">
      <span className={cn('flex-none', isArchived && 'opacity-60')}>
        <KvitEmojiTile emoji={category.emoji} background={categoryColor(category)} />
      </span>
      <span className={cn('min-w-0 flex-1 font-semibold break-words', isArchived && 'text-muted-foreground')}>
        {category.name}
      </span>
      {children}
    </li>
  )
}

export function MyCategoriesSheet({ onClose }: MyCategoriesSheetProps) {
  const { t } = useTranslation()
  const sheet = useKvitSheet()
  const categoriesQuery = usePersonalCategoriesWithArchived()
  const archive = useArchivePersonalCategory()
  const unarchive = useUnarchivePersonalCategory()
  const [editing, setEditing] = useState<CustomPersonalCategory | null>(null)
  const archivedHeadingId = useId()
  const activeListRef = useListAnimation<HTMLUListElement>()
  const archivedListRef = useListAnimation<HTMLUListElement>()
  const isBusy: boolean = archive.isPending || unarchive.isPending

  function showFailure(error: Error): void {
    console.error('A personal category action failed', error)
    toast.error(t(errorMessageKey(error)))
  }

  function bringBack(category: CustomPersonalCategory): void {
    void unarchive.mutateAsync(category.id).catch(showFailure)
  }

  function archiveNow(category: CustomPersonalCategory): void {
    archive.mutate(category.id, {
      onSuccess: () => {
        showUndoToast(t('budget.categories.archivedToast', { name: category.name }), {
          danger: false,
          undoLabel: t('common.undo'),
          onUndo: () => bringBack(category),
        })
      },
      onError: showFailure,
    })
  }

  function renderContent() {
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
          <div className="flex flex-col gap-2">
            <KvitSkeleton className="h-13" />
            <KvitSkeleton className="h-13" />
            <KvitSkeleton className="h-13" />
          </div>
        </KvitLoading>
      )
    }
    const own = categoriesQuery.data.filter(isOwn)
    const active = own.filter((category) => !category.archived)
    const archived = own.filter((category) => category.archived)
    return (
      <>
        {active.length === 0 ? (
          <KvitEmpty message={t('budget.categories.emptyMine')} />
        ) : (
          <ul ref={activeListRef} className="flex flex-col">
            {active.map((category) => (
              <CategoryRow key={category.id} category={category} isArchived={false}>
                <KvitButton
                  variant="smallLink"
                  className={rowButtonLook}
                  disabled={isBusy}
                  onClick={() => setEditing(category)}
                >
                  {t('expense.edit')}
                </KvitButton>
                <KvitButton
                  variant="smallLink"
                  className={rowButtonLook}
                  disabled={isBusy}
                  onClick={() => archiveNow(category)}
                >
                  {t('budget.categories.archive')}
                </KvitButton>
              </CategoryRow>
            ))}
          </ul>
        )}
        {archived.length > 0 && (
          <section aria-labelledby={archivedHeadingId} className="flex flex-col">
            <h3
              id={archivedHeadingId}
              className="mx-1 mt-1.5 mb-1 text-[0.8125rem] font-bold tracking-[0.04em] text-muted-foreground uppercase"
            >
              {t('budget.categories.archivedHeading', { replace: { count: archived.length } })}
            </h3>
            <ul ref={archivedListRef} className="flex flex-col">
              {archived.map((category) => (
                <CategoryRow key={category.id} category={category} isArchived>
                  <KvitButton
                    variant="smallLink"
                    className={rowButtonLook}
                    disabled={isBusy}
                    onClick={() => bringBack(category)}
                  >
                    {t('budget.categories.bringBack')}
                  </KvitButton>
                </CategoryRow>
              ))}
            </ul>
          </section>
        )}
      </>
    )
  }

  return (
    <KvitSheet title={t('budget.categories.mineTitle')} onClose={onClose} actionsRef={sheet.actionsRef}>
      <div className="-mx-1 flex max-h-[60dvh] flex-col gap-3 overflow-y-auto px-1">{renderContent()}</div>
      <KvitButton variant="link" onClick={sheet.close}>
        {t('common.close')}
      </KvitButton>
      {editing !== null && <EditCategorySheet category={editing} onClose={() => setEditing(null)} />}
    </KvitSheet>
  )
}
