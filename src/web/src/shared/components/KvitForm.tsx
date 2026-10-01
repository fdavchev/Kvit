import type { ReactNode, SubmitEvent } from 'react'
import { KvitButton } from './KvitButton'
import { KvitInlineError } from './KvitInlineError'

interface KvitFormProps {
  submitLabel: string
  isPending: boolean
  errorMessage: string | null
  onSubmit: () => void
  children: ReactNode
  footer?: ReactNode
}

export function KvitForm({
  submitLabel,
  isPending,
  errorMessage,
  onSubmit,
  children,
  footer,
}: KvitFormProps) {
  function handleSubmit(event: SubmitEvent<HTMLFormElement>): void {
    event.preventDefault()
    onSubmit()
  }

  return (
    <form noValidate onSubmit={handleSubmit} className="flex flex-col gap-7">
      <div className="flex flex-col gap-[18px]">{children}</div>
      <div className="flex flex-col gap-3">
        {errorMessage !== null && <KvitInlineError message={errorMessage} />}
        <KvitButton type="submit" disabled={isPending}>
          {submitLabel}
        </KvitButton>
        {footer}
      </div>
    </form>
  )
}
