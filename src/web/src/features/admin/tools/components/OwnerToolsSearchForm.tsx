import { useState, type SubmitEvent } from 'react'
import { KvitButton } from '@/shared/components/KvitButton'
import { KvitTextField } from '@/shared/components/KvitTextField'

interface OwnerToolsSearchFormProps {
  id: string
  label: string
  buttonLabel: string
  onSearch: (text: string) => void
}

export function OwnerToolsSearchForm({ id, label, buttonLabel, onSearch }: OwnerToolsSearchFormProps) {
  const [text, setText] = useState<string>('')

  function submit(event: SubmitEvent<HTMLFormElement>): void {
    event.preventDefault()
    onSearch(text)
  }

  return (
    <form noValidate onSubmit={submit} className="flex flex-col gap-3">
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
      />
      <KvitButton type="submit" variant="secondary">
        {buttonLabel}
      </KvitButton>
    </form>
  )
}
