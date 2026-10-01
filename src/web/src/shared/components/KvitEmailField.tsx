import { KvitTextField } from './KvitTextField'

interface KvitEmailFieldProps {
  id: string
  label: string
  value: string
  onChange: (value: string) => void
}

export function KvitEmailField({ id, label, value, onChange }: KvitEmailFieldProps) {
  return (
    <KvitTextField
      id={id}
      label={label}
      value={value}
      onChange={onChange}
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
