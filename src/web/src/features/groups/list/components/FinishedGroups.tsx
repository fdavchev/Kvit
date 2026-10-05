import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import type { GroupListRow } from '@/core/services/groups/groupsService'
import { cn } from '@/shared/utils/cn'
import { GroupListItem } from './GroupListItem'

interface FinishedGroupsProps {
  groups: GroupListRow[]
}

export function FinishedGroups({ groups }: FinishedGroupsProps) {
  const { t } = useTranslation()
  const [isOpen, setIsOpen] = useState<boolean>(false)

  return (
    <div className="flex flex-col gap-2.5">
      <button
        type="button"
        aria-expanded={isOpen}
        onClick={() => setIsOpen((wasOpen) => !wasOpen)}
        className="pressable flex min-h-13 items-center gap-3 rounded-2xl bg-card px-3.5 text-left font-semibold text-card-foreground hover:bg-secondary-hover"
      >
        <span className="flex flex-1 items-center gap-2">
          {t('groups.finished')}
          <span className="rounded-full border border-field-border px-2 text-xs font-semibold text-muted-foreground">
            {groups.length}
          </span>
        </span>
        <span
          aria-hidden="true"
          className={cn(
            'text-xl text-muted-foreground transition-[rotate] duration-150 ease-out motion-reduce:transition-none',
            isOpen && 'rotate-90',
          )}
        >
          ›
        </span>
      </button>
      {isOpen &&
        groups.map((group) => (
          <GroupListItem
            key={group.id}
            group={group}
            subtitle={
              <span className="text-[0.8125rem] text-muted-foreground">
                {t('groups.finishedReadOnly')}
              </span>
            }
          />
        ))}
    </div>
  )
}
