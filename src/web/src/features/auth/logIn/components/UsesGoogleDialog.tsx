import { useTranslation } from 'react-i18next'
import { GoogleSignInButton } from '@/core/google/GoogleSignInButton'
import { KvitButton } from '@/shared/components/KvitButton'
import { KvitDialog } from '@/shared/components/KvitDialog'

interface UsesGoogleDialogProps {
  onCredential: (idToken: string) => void
  onClose: () => void
}

export function UsesGoogleDialog({ onCredential, onClose }: UsesGoogleDialogProps) {
  const { t } = useTranslation()

  return (
    <KvitDialog title={t('auth.googleUses.title')} onClose={onClose}>
      <p className="text-pretty text-muted-foreground">{t('auth.googleUses.body')}</p>
      <div className="flex flex-col gap-2 pt-2">
        <GoogleSignInButton onCredential={onCredential} />
        <KvitButton variant="link" onClick={onClose}>
          {t('common.close')}
        </KvitButton>
      </div>
    </KvitDialog>
  )
}
