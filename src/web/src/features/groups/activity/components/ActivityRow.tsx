import { Link } from 'react-router'
import { KvitAvatar } from '@/shared/components/KvitAvatar'

interface ActivityRowProps {
  actorName: string
  pictureUrl: string | null
  colorIndex: number
  sentence: string
  expenseTitle: string | null
  when: string
  linkTo: string | null
}

const rowLook = 'flex items-start gap-2.5 border-b border-field-border py-2.5'

export function ActivityRow({
  actorName,
  pictureUrl,
  colorIndex,
  sentence,
  expenseTitle,
  when,
  linkTo,
}: ActivityRowProps) {
  const content = (
    <>
      <KvitAvatar name={actorName} pictureUrl={pictureUrl} colorIndex={colorIndex} />
      <div className="flex min-w-0 flex-1 flex-col">
        <p className="font-semibold break-words">{sentence}</p>
        {expenseTitle !== null && (
          <p className="text-[0.9375rem] break-words text-muted-foreground">{expenseTitle}</p>
        )}
        <p className="text-[0.8125rem] text-muted-foreground">{when}</p>
      </div>
    </>
  )

  return (
    <li>
      {linkTo === null ? (
        <div className={rowLook}>{content}</div>
      ) : (
        <Link to={linkTo} className={`pressable ${rowLook} hover:bg-secondary-hover`}>
          {content}
        </Link>
      )}
    </li>
  )
}
