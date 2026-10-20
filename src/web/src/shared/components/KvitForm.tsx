import type { ReactNode, SubmitEvent } from 'react'
import { KvitButton } from './KvitButton'
import { KvitInlineError } from './KvitInlineError'

interface KvitFormProps {
  submitLabel: string
  isPending: boolean
  canSubmit?: boolean
  submitClassName?: string
  errorMessage: string | null
  onSubmit: () => void
  children: ReactNode
  footer?: ReactNode
}

export function KvitForm({
  submitLabel,
  isPending,
  canSubmit = true,
  submitClassName,
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
    <form noValidate onSubmit={handleSubmit} className="flex flex-col gap-7 in-data-kvit-sheet-body:min-h-0">
      <div className="flex flex-col gap-[18px] in-data-kvit-sheet-body:-mx-5 in-data-kvit-sheet-body:-my-1.5 in-data-kvit-sheet-body:min-h-0 in-data-kvit-sheet-body:overflow-y-auto in-data-kvit-sheet-body:overscroll-contain in-data-kvit-sheet-body:px-5 in-data-kvit-sheet-body:py-1.5">
        {children}
      </div>
      <div className="flex flex-none flex-col gap-3">
        {errorMessage !== null && <KvitInlineError message={errorMessage} />}
        <KvitButton type="submit" className={submitClassName} disabled={isPending || !canSubmit}>
          {submitLabel}
        </KvitButton>
        {footer}
      </div>
    </form>
  )
}
