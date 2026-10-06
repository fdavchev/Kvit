import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { routes } from '@/core/router/routes'

export type GroupTab = 'expenses' | 'activity'

interface GroupTabsProps {
  groupId: string
  currentTab: GroupTab
}

const tabLook =
  'pressable flex min-h-11 flex-1 items-center justify-center rounded-full font-bold text-muted-foreground hover:text-foreground aria-[current=page]:bg-pill aria-[current=page]:text-foreground aria-[current=page]:shadow-[0_1px_3px_var(--track-shadow)]'

export function GroupTabs({ groupId, currentTab }: GroupTabsProps) {
  const { t } = useTranslation()
  const tabs: readonly { tab: GroupTab; to: string; label: string }[] = [
    { tab: 'expenses', to: routes.group(groupId), label: t('expenses.title') },
    { tab: 'activity', to: routes.groupActivity(groupId), label: t('activity.title') },
  ]

  return (
    <div className="mb-2.5 flex gap-0.5 rounded-full bg-card p-1">
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
