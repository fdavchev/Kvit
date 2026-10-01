interface KvitInlineErrorProps {
  message: string
}

export function KvitInlineError({ message }: KvitInlineErrorProps) {
  return (
    <p role="alert" className="font-semibold text-pretty text-destructive">
      {message}
    </p>
  )
}
