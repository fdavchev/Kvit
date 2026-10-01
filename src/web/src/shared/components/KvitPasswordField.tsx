import { useState, type ComponentProps } from 'react'
import { KvitPasswordToggle } from './KvitPasswordToggle'
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
  const [isShown, setIsShown] = useState<boolean>(false)
  return (
    <KvitTextField
      {...props}
      type={isShown ? 'text' : 'password'}
      autoCapitalize="none"
      autoCorrect="off"
      spellCheck={false}
      trailing={
        <KvitPasswordToggle
          isShown={isShown}
          onToggle={() => setIsShown((wasShown) => !wasShown)}
        />
      }
    />
  )
}
