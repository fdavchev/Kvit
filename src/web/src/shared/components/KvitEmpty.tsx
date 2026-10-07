interface KvitEmptyProps {
  message: string
  emoji?: string
  title?: string
}

export function KvitEmpty({ message, emoji, title }: KvitEmptyProps) {
  if (emoji === undefined && title === undefined) {
    return <p className="p-6 text-center text-muted-foreground">{message}</p>
  }
  return (
    <div className="flex flex-col items-center gap-1.5 px-6 pt-4 pb-6 text-center">
      {emoji !== undefined && (
        <span aria-hidden="true" className="text-[2.5rem] leading-none">
          {emoji}
        </span>
      )}
      {title !== undefined && <p className="font-bold">{title}</p>}
      <p className="text-muted-foreground">{message}</p>
    </div>
  )
}
