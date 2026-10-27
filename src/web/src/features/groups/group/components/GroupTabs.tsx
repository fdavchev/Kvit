import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { routes } from '@/core/router/routes'
import { KvitTabPill } from '@/shared/components/KvitTabPill'

export type GroupTab = 'expenses' | 'balances' | 'activity' | 'totals'

interface GroupTabsProps {
  groupId: string
  currentTab: GroupTab
}

const tabLook =
  'pressable relative flex min-h-11 flex-1 items-center justify-center rounded-full font-bold text-muted-foreground hover:text-foreground aria-[current=page]:text-tab-active-foreground'

export function GroupTabs({ groupId, currentTab }: GroupTabsProps) {
  const { t } = useTranslation()
  const tabs: readonly { tab: GroupTab; to: string; label: string }[] = [
    { tab: 'expenses', to: routes.group(groupId), label: t('expenses.title') },
    { tab: 'balances', to: routes.groupBalances(groupId), label: t('balances.title') },
    { tab: 'activity', to: routes.groupActivity(groupId), label: t('activity.title') },
    { tab: 'totals', to: routes.groupTotals(groupId), label: t('groupTotals.tab') },
  ]

  return (
    <div className="relative mb-4 flex rounded-full bg-card p-1 lg:w-full lg:max-w-[36rem] lg:self-center dark:inset-ring dark:inset-ring-field-border">
      <KvitTabPill
        activeIndex={tabs.findIndex((item) => item.tab === currentTab)}
        tabCount={tabs.length}
        className="bg-tab-active shadow-[0_1px_3px_var(--track-shadow)]"
      />
      {tabs.map((item) => (
        <Link
          key={item.tab}
          to={item.to}
          aria-current={item.tab === currentTab ? 'page' : undefined}
          className={tabLook}
        >
          {item.label}
        </Link>
      ))}
    </div>
  )
}
