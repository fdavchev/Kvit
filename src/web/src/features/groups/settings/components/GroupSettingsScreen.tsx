import { useTranslation } from 'react-i18next'
import { routes } from '@/core/router/routes'
import { KvitBackButton } from '@/shared/components/KvitBackButton'
import { KvitLoading } from '@/shared/components/KvitLoading'
import { KvitScreen } from '@/shared/components/KvitScreen'
import { KvitScreenTitle } from '@/shared/components/KvitScreenTitle'
import { useGroup } from '../../group/hooks/useGroup'
import { GroupLoadError } from '../../shared/components/GroupLoadError'
import { useGroupIdParam } from '../../shared/useGroupIdParam'
import { LeaveGroupSection } from './LeaveGroupSection'
import { OwnerGroupSettings } from './OwnerGroupSettings'

export function GroupSettingsScreen() {
  const { t } = useTranslation()
  const groupId = useGroupIdParam()
  const groupQuery = useGroup(groupId)

  function renderContent() {
    if (groupQuery.isPending) {
      return <KvitLoading />
    }
    if (groupQuery.isError) {
      return (
        <GroupLoadError
          error={groupQuery.error}
          onRetry={() => void groupQuery.refetch()}
        />
      )
    }
    const group = groupQuery.data
    return (
      <>
        <KvitScreenTitle>{t('groupSettings.title')}</KvitScreenTitle>
        {group.isOwner && <OwnerGroupSettings group={group} />}
        <div className={group.isOwner ? 'pt-8' : 'pt-2'}>
          <LeaveGroupSection group={group} />
        </div>
      </>
    )
  }

  return (
    <KvitScreen>
      <div className="flex min-h-12">
        <KvitBackButton to={routes.group(groupId)} />
      </div>
      {renderContent()}
    </KvitScreen>
  )
}
