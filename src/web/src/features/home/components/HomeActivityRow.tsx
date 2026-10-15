import { Link } from 'react-router'
import { routes } from '@/core/router/routes'

interface HomeActivityRowProps {
  groupId: string
  groupName: string
  sentence: string
  when: string
}

export function HomeActivityRow({ groupId, groupName, sentence, when }: HomeActivityRowProps) {
  return (
    <li>
      <Link
        to={routes.group(groupId)}
        className="pressable flex items-start gap-3 border-b border-field-border py-2.5 hover:bg-secondary-hover"
      >
        <span className="flex min-w-0 flex-1 flex-col">
          <span className="font-semibold break-words">{sentence}</span>
          <span className="text-[0.8125rem] break-words text-muted-foreground">{groupName}</span>
        </span>
        <span className="flex-none text-[0.8125rem] text-muted-foreground">{when}</span>
      </Link>
    </li>
  )
}
