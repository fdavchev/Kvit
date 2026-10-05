import type { ReactNode } from 'react'
import { Link } from 'react-router'
import { routes } from '@/core/router/routes'
import type { GroupListRow } from '@/core/services/groups/groupsService'
import { KvitEmojiTile } from '@/shared/components/KvitEmojiTile'

interface GroupListItemProps {
  group: GroupListRow
  subtitle: ReactNode
}

export function GroupListItem({ group, subtitle }: GroupListItemProps) {
  return (
    <Link
      to={routes.group(group.id)}
      className="pressable flex min-h-16 items-center gap-3 rounded-2xl bg-card px-3.5 py-3 text-card-foreground hover:bg-secondary-hover"
    >
      <KvitEmojiTile emoji={group.emoji} />
      <span className="flex min-w-0 flex-1 flex-col">
        <span className="truncate font-semibold">{group.name}</span>
        {subtitle}
      </span>
      <span aria-hidden="true" className="text-xl text-muted-foreground">
        ›
      </span>
    </Link>
  )
}
