interface KvitEmptyProps {
  message: string
  emoji?: string
}

export function KvitEmpty({ message, emoji }: KvitEmptyProps) {
  if (emoji === undefined) {
    return <p className="p-6 text-center text-muted-foreground">{message}</p>
  }
  return (
    <div className="flex flex-col items-center gap-1.5 px-6 pt-4 pb-6 text-center">
      <span aria-hidden="true" className="text-[2.5rem] leading-none">
        {emoji}
      </span>
      <p className="text-muted-foreground">{message}</p>
    </div>
  )
}
