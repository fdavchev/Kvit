import { Link } from 'react-router'
import { routes } from '@/core/router/routes'

interface NeedsYouCardTextProps {
  groupId: string
  sentence: string
  groupName?: string
}

export function NeedsYouCardText({ groupId, sentence, groupName }: NeedsYouCardTextProps) {
  return (
    <Link to={routes.group(groupId)} className="pressable -m-1 flex min-h-11 flex-col justify-center rounded-xl p-1 hover:bg-secondary-hover">
      <p className="font-bold break-words">{sentence}</p>
      {groupName !== undefined && (
        <p className="text-[0.8125rem] break-words text-muted-foreground">{groupName}</p>
      )}
    </Link>
  )
}
