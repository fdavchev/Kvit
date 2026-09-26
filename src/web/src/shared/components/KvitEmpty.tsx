interface KvitEmptyProps {
  message: string
}

export function KvitEmpty({ message }: KvitEmptyProps) {
  return <p className="p-6 text-center text-muted-foreground">{message}</p>
}
