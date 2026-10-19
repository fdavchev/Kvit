interface KvitOrDividerProps {
  label: string
}

export function KvitOrDivider({ label }: KvitOrDividerProps) {
  return (
    <p className="my-1 text-center text-[0.8125rem] font-semibold tracking-[0.02em] text-muted-foreground">
      {label}
    </p>
  )
}
