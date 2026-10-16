import { useRef, useState, type ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useMatch } from 'react-router'
import { routes } from '@/core/router/routes'
import type { Group } from '@/core/services/groups/groupsService'
import { AddExpenseButton } from '@/features/expenses/list/components/AddExpenseButton'
import { GroupExpenses } from '@/features/expenses/list/components/GroupExpenses'
import { KvitBackButton } from '@/shared/components/KvitBackButton'
import { KvitEmojiTile } from '@/shared/components/KvitEmojiTile'
import { KvitLoading } from '@/shared/components/KvitLoading'
import { KvitScreen } from '@/shared/components/KvitScreen'
import { kvitChipLook } from '@/shared/components/kvitChipLook'
import { kvitRoundActionLooks } from '@/shared/components/kvitRoundActionLooks'
import { useIsDesktop } from '@/shared/hooks/useIsDesktop'
import { useToastsAboveElement } from '@/shared/hooks/useToastsAboveElement'
import { cn } from '@/shared/utils/cn'
import { GroupActivity } from '../../activity/components/GroupActivity'
import { GroupBalances } from '../../balances/components/GroupBalances'
import { ClosingBar } from '../../closing/components/ClosingBar'
import { GroupStatusBadge } from '../../closing/components/GroupStatusBadge'
import { useClosingBar } from '../../closing/hooks/useClosingBar'
import { GroupLoadError } from '../../shared/components/GroupLoadError'
import { GroupSizeLine } from '../../shared/components/GroupSizeLine'
import { canChangeGroup, canInviteToGroup } from '../../shared/groupAccess'
import { useGroupIdParam } from '../../shared/useGroupIdParam'
import { useGroup } from '../hooks/useGroup'
import { useInviteLinkActions } from '../useInviteLinkActions'
import { AddNameSheet } from './AddNameSheet'
import { AddPeopleButtons } from './AddPeopleButtons'
import { GroupTabs, type GroupTab } from './GroupTabs'

export function GroupScreen() {
  const groupId = useGroupIdParam()
  const isActivityTab: boolean = useMatch(routes.groupActivity(':groupId')) !== null
  const isBalancesTab: boolean = useMatch(routes.groupBalances(':groupId')) !== null
  const tab: GroupTab = currentTabOf(isActivityTab, isBalancesTab)
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
    return <GroupContent group={groupQuery.data} tab={tab} />
  }

  return (
    <KvitScreen width="wide">
      <div className="flex min-h-12">
        <KvitBackButton to={routes.groups} />
      </div>
      {renderContent()}
    </KvitScreen>
  )
}

interface GroupContentProps {
  group: Group
  tab: GroupTab
}

function GroupContent({ group, tab }: GroupContentProps) {
  const { t } = useTranslation()
  const inviteLinkActions = useInviteLinkActions()
  const closingBar = useClosingBar(group)
  const isDesktop = useIsDesktop()
  const [isAddingName, setIsAddingName] = useState<boolean>(false)
  const [shownTab, setShownTab] = useState<GroupTab>(tab)
  const [hasSwitchedTab, setHasSwitchedTab] = useState<boolean>(false)
  if (tab !== shownTab) {
    setShownTab(tab)
    setHasSwitchedTab(true)
  }
  const canChange: boolean = canChangeGroup(group)
  const isOneBill: boolean = group.kind === 'OneBill'
  const showsAddPeopleButtons: boolean = group.memberCount === 1 && !isOneBill && canChange
  const showsAddExpense: boolean = tab === 'expenses' && canChange
  const showsFloatingAddExpense: boolean = showsAddExpense && !isDesktop
  const showsInPageAddExpense: boolean = showsAddExpense && isDesktop
  const hasBar: boolean = closingBar !== null
  const expenses = (
    <GroupExpenses
      groupId={group.id}
      actionsWhenEmpty={
        showsAddPeopleButtons ? (
          <AddPeopleButtons
            onAddName={() => setIsAddingName(true)}
            onShareLink={() => void inviteLinkActions.share(group.inviteToken)}
          />
        ) : null
      }
    />
  )
  const balances = (
    <GroupBalances groupId={group.id} defaultCurrency={group.defaultCurrency} canChangePayments={canChange} />
  )

  return (
    <>
      <div className="flex flex-col items-center gap-2.5 pt-2 pb-4 text-center">
        <KvitEmojiTile emoji={group.emoji} size="large" />
        <div className="flex flex-wrap items-center justify-center gap-x-2 gap-y-1">
          <h1 className="min-w-0 text-[1.75rem] leading-[1.15] font-extrabold tracking-[-0.02em] text-balance wrap-anywhere">
            {group.name}
          </h1>
          <GroupStatusBadge status={group.status} />
        </div>
        <GroupSizeLine memberCount={group.memberCount} currency={group.defaultCurrency} />
      </div>
      <div className="flex flex-wrap items-center justify-center gap-2 pb-4">
        {showsInPageAddExpense && (
          <Link to={routes.groupExpenseNew(group.id)} className={kvitRoundActionLooks.main}>
            <span aria-hidden="true">＋</span>
            <span className="wrap-anywhere">{t('expense.addTitle')}</span>
          </Link>
        )}
        <Link to={routes.groupMembers(group.id)} className={kvitChipLook}>
          {t('members.title')}
        </Link>
        <Link to={routes.groupSettings(group.id)} className={kvitChipLook}>
          {t('group.settings')}
        </Link>
        {isOneBill && canInviteToGroup(group) && (
          <button
            type="button"
            className={kvitChipLook}
            onClick={() => void inviteLinkActions.share(group.inviteToken)}
          >
            {t('group.shareLink')}
          </button>
        )}
      </div>
      <GroupTabs groupId={group.id} currentTab={tab} />
      <div
        key={tab}
        data-tab-panel
        className={cn('flex flex-col', hasBar && 'pb-3', hasSwitchedTab && 'motion-safe:animate-tab-panel-in')}
      >
        {tab === 'expenses' && !isDesktop && expenses}
        {tab === 'expenses' && isDesktop && (
          <div className="grid grid-cols-[repeat(auto-fit,minmax(min(100%,24rem),1fr))] items-start gap-x-[clamp(1.5rem,3vw,3rem)]">
            <div className="flex min-w-0 flex-col">{expenses}</div>
            <div className="flex min-w-0 flex-col">{balances}</div>
          </div>
        )}
        {tab === 'balances' && balances}
        {tab === 'activity' && <GroupActivity groupId={group.id} />}
      </div>
      {(showsFloatingAddExpense || hasBar) && (
        <FloatingActions hasBar={hasBar}>
          {showsFloatingAddExpense && (
            <div className={hasBar ? 'px-3' : undefined}>
              <AddExpenseButton groupId={group.id} />
            </div>
          )}
          <ClosingBar group={group} view={closingBar} />
        </FloatingActions>
      )}
      {isAddingName && (
        <AddNameSheet groupId={group.id} onClose={() => setIsAddingName(false)} />
      )}
    </>
  )
}

interface FloatingActionsProps {
  hasBar: boolean
  children: ReactNode
}

function FloatingActions({ hasBar, children }: FloatingActionsProps) {
  const rootRef = useRef<HTMLDivElement>(null)
  useToastsAboveElement(rootRef)

  return (
    <div
      ref={rootRef}
      className={cn(
        'pointer-events-none sticky z-10 mt-auto flex flex-col items-end gap-3 pt-3',
        hasBar ? '-mx-3 bottom-[max(12px,env(safe-area-inset-bottom))]' : 'bottom-[max(24px,env(safe-area-inset-bottom))]',
      )}
    >
      {children}
    </div>
  )
}

function currentTabOf(isActivityTab: boolean, isBalancesTab: boolean): GroupTab {
  if (isActivityTab) {
    return 'activity'
  }
  if (isBalancesTab) {
    return 'balances'
  }
  return 'expenses'
}
