import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { errorMessageKey } from '@/core/api/errors'
import { useLanguage } from '@/core/i18n/useLanguage'
import { routes } from '@/core/router/routes'
import { KvitBackButton } from '@/shared/components/KvitBackButton'
import { KvitButton } from '@/shared/components/KvitButton'
import { KvitEmojiTile } from '@/shared/components/KvitEmojiTile'
import { KvitEmpty } from '@/shared/components/KvitEmpty'
import { KvitError } from '@/shared/components/KvitError'
import { KvitLoading } from '@/shared/components/KvitLoading'
import { KvitScreen } from '@/shared/components/KvitScreen'
import { KvitScreenTitle } from '@/shared/components/KvitScreenTitle'
import { formatDayMonthYear } from '@/shared/utils/formatDate'
import { useGroups } from '../hooks/useGroups'
import { useRestoreGroup } from '../hooks/useRestoreGroup'

export function RecentlyDeletedScreen() {
  const { t } = useTranslation()
  const { language } = useLanguage()
  const groupsQuery = useGroups()
  const restoreGroup = useRestoreGroup()

  function showFailure(error: Error): void {
    console.error('Restoring a group failed', error)
    toast.error(t(errorMessageKey(error)))
  }

  function restore(groupId: string): void {
    restoreGroup.mutate(groupId, {
      onSuccess: () => {
        toast.success(t('recentlyDeleted.restored'))
      },
      onError: showFailure,
    })
  }

  function renderContent() {
    if (groupsQuery.isPending) {
      return <KvitLoading />
    }
    if (groupsQuery.isError) {
      return (
        <KvitError
          message={t(errorMessageKey(groupsQuery.error))}
          onRetry={() => void groupsQuery.refetch()}
        />
      )
    }
    const deletedGroups = groupsQuery.data.recentlyDeleted
    if (deletedGroups.length === 0) {
      return <KvitEmpty message={t('recentlyDeleted.empty')} />
    }
    return (
      <div className="flex flex-col gap-2.5">
        {deletedGroups.map((group) => (
          <div
            key={group.id}
            className="flex min-h-16 items-center gap-3 rounded-2xl bg-card px-3.5 py-3 text-card-foreground"
          >
            <KvitEmojiTile emoji={group.emoji} />
            <span className="flex min-w-0 flex-1 flex-col">
              <span className="truncate font-semibold">{group.name}</span>
              <span className="text-[0.8125rem] text-muted-foreground">
                {t('recentlyDeleted.restorableUntil', {
                  date: formatDayMonthYear(group.restorableUntil, language),
                })}
              </span>
            </span>
            <KvitButton
              className="min-h-11 w-auto rounded-full px-4 text-[0.9375rem] font-semibold"
              disabled={restoreGroup.isPending && restoreGroup.variables === group.id}
              onClick={() => restore(group.id)}
            >
              {t('recentlyDeleted.restore')}
            </KvitButton>
          </div>
        ))}
      </div>
    )
  }

  return (
    <KvitScreen>
      <div className="flex min-h-12">
        <KvitBackButton to={routes.groups} />
      </div>
      <KvitScreenTitle>{t('recentlyDeleted.title')}</KvitScreenTitle>
      <p className="-mt-3 pb-5 text-[0.9375rem] text-pretty text-muted-foreground">
        {t('recentlyDeleted.intro')}
      </p>
      {renderContent()}
    </KvitScreen>
  )
}
