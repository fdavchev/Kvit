import { useId, useState, type SubmitEvent } from 'react'
import { KvitButton } from '@/shared/components/KvitButton'
import { KvitInlineError } from '@/shared/components/KvitInlineError'
import { KvitTextField } from '@/shared/components/KvitTextField'
import { KvitFormErrorIdContext } from '@/shared/components/kvitFormErrorId'

interface OwnerToolsSearchFormProps {
  id: string
  label: string
  buttonLabel: string
  errorMessage: string | null
  onSearch: (text: string) => void
}

export function OwnerToolsSearchForm({ id, label, buttonLabel, errorMessage, onSearch }: OwnerToolsSearchFormProps) {
  const [text, setText] = useState<string>('')
  const errorId = useId()

  function submit(event: SubmitEvent<HTMLFormElement>): void {
    event.preventDefault()
    onSearch(text)
  }

  return (
    <form noValidate onSubmit={submit} className="flex flex-col gap-3">
      <KvitFormErrorIdContext value={errorMessage === null ? null : errorId}>
        <KvitTextField
          id={id}
          label={label}
          value={text}
          onChange={setText}
          autoComplete="off"
          autoCapitalize="none"
          autoCorrect="off"
          spellCheck={false}
          enterKeyHint="search"
          invalid={errorMessage !== null}
        />
      </KvitFormErrorIdContext>
      {errorMessage !== null && <KvitInlineError id={errorId} message={errorMessage} />}
      <KvitButton type="submit" variant="secondary">
        {buttonLabel}
      </KvitButton>
    </form>
  )
}
