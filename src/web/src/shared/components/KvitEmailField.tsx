import { KvitTextField } from './KvitTextField'

interface KvitEmailFieldProps {
  id: string
  label: string
  value: string
  onChange: (value: string) => void
  invalid?: boolean
}

export function KvitEmailField({ id, label, value, onChange, invalid }: KvitEmailFieldProps) {
  return (
    <KvitTextField
      id={id}
      label={label}
      value={value}
      onChange={onChange}
      invalid={invalid}
      type="email"
      inputMode="email"
      autoComplete="email"
      autoCapitalize="none"
      autoCorrect="off"
      spellCheck={false}
      enterKeyHint="next"
    />
  )
}
