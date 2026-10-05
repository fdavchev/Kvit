import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { errorMessageKey } from '@/core/api/errors'
import { routes } from '@/core/router/routes'
import { KvitEmpty } from '@/shared/components/KvitEmpty'
import { KvitError } from '@/shared/components/KvitError'
import { KvitLinkButton } from '@/shared/components/KvitLinkButton'
import { KvitLoading } from '@/shared/components/KvitLoading'
import { KvitScreen } from '@/shared/components/KvitScreen'
import { KvitScreenTitle } from '@/shared/components/KvitScreenTitle'
import { GroupSizeLine } from '../../shared/components/GroupSizeLine'
import { useGroups } from '../hooks/useGroups'
import { FinishedGroups } from './FinishedGroups'
import { GroupListItem } from './GroupListItem'

export function GroupsScreen() {
  const { t } = useTranslation()
  const groupsQuery = useGroups()

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
    const { groups, finishedGroups } = groupsQuery.data
    return (
      <>
        {groups.length === 0 ? (
          <KvitEmpty message={t('groups.empty')} />
        ) : (
          <div className="flex flex-col gap-2.5">
            {groups.map((group) => (
              <GroupListItem
                key={group.id}
                group={group}
                subtitle={
                  <GroupSizeLine
                    memberCount={group.memberCount}
                    currency={group.defaultCurrency}
                  />
                }
              />
            ))}
          </div>
        )}
        <div className="pt-4">
          <KvitLinkButton to={routes.newGroup}>{t('groups.newButton')}</KvitLinkButton>
        </div>
        {finishedGroups.length > 0 && (
          <div className="pt-5">
            <FinishedGroups groups={finishedGroups} />
          </div>
        )}
        <div className="flex justify-center pt-3">
          <Link
            to={routes.recentlyDeletedGroups}
            className="pressable inline-flex min-h-11 items-center gap-1 rounded-xl px-3 text-[0.9375rem] font-semibold text-link hover:underline"
          >
            {t('groups.recentlyDeletedLink')}
            <span aria-hidden="true">›</span>
          </Link>
        </div>
      </>
    )
  }

  return (
    <KvitScreen>
      <div className="min-h-12" />
      <KvitScreenTitle>{t('groups.title')}</KvitScreenTitle>
      {renderContent()}
    </KvitScreen>
  )
}
