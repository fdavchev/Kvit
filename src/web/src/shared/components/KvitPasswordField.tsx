import type { ComponentProps } from 'react'
import { KvitTextField } from './KvitTextField'

interface KvitPasswordFieldProps {
  id: string
  label: string
  value: string
  onChange: (value: string) => void
  autoComplete: 'current-password' | 'new-password'
  enterKeyHint: ComponentProps<'input'>['enterKeyHint']
  hint?: string
}

export function KvitPasswordField(props: KvitPasswordFieldProps) {
  return (
    <KvitTextField
      {...props}
      type="password"
      autoCapitalize="none"
      autoCorrect="off"
      spellCheck={false}
    />
  )
}
