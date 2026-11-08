import { useTranslation } from 'react-i18next'
import { useIsGuest } from '@/core/auth/useIsGuest'
import { routes } from '@/core/router/routes'
import { KvitBackButton } from '@/shared/components/KvitBackButton'
import { KvitLoading } from '@/shared/components/KvitLoading'
import { KvitScreen } from '@/shared/components/KvitScreen'
import { KvitScreenTitle } from '@/shared/components/KvitScreenTitle'
import { useGroup } from '../../group/hooks/useGroup'
import { GroupLoadError } from '../../shared/components/GroupLoadError'
import { canChangeGroup, canLeaveGroup } from '../../shared/groupAccess'
import { useGroupIdParam } from '../../shared/useGroupIdParam'
import { reportGroupLink } from '../reportGroupLink'
import { CountInBudgetSwitch } from './CountInBudgetSwitch'
import { LeaveGroupSection } from './LeaveGroupSection'
import { OwnerGroupSettings } from './OwnerGroupSettings'

export function GroupSettingsScreen() {
  const { t } = useTranslation()
  const groupId = useGroupIdParam()
  const groupQuery = useGroup(groupId)
  const isGuest = useIsGuest()

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
        {!group.isOwner && !canLeaveGroup(group) && (
          <p className="pt-2 text-[0.9375rem] text-pretty text-muted-foreground">{t('closing.settingsFrozen')}</p>
        )}
        {!isGuest && canChangeGroup(group) && (
          <div className="pt-2 pb-6">
            <CountInBudgetSwitch group={group} />
          </div>
        )}
        {group.isOwner && <OwnerGroupSettings group={group} />}
        {(group.isOwner || canLeaveGroup(group)) && (
          <div className={group.isOwner ? 'pt-8' : 'pt-2'}>
            <LeaveGroupSection group={group} />
          </div>
        )}
        <div className="flex flex-col items-start gap-1 pt-6">
          <a
            href={reportGroupLink(t('groupSettings.reportSubject'), t('groupSettings.reportBody', { id: group.id }))}
            className="pressable -ml-3 inline-flex min-h-11 items-center gap-1 rounded-xl px-3 text-[0.9375rem] font-semibold text-link hover:underline"
          >
            {t('groupSettings.report')}
            <span aria-hidden="true">›</span>
          </a>
          <p className="text-[0.875rem] text-pretty text-muted-foreground">{t('groupSettings.reportHint')}</p>
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
