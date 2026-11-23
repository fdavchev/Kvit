interface KvitInlineErrorProps {
  message: string
  id?: string
}

export function KvitInlineError({ message, id }: KvitInlineErrorProps) {
  return (
    <p id={id} role="alert" className="font-semibold text-pretty text-destructive">
      {message}
    </p>
  )
}
