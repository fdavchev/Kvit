import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router'
import { GoogleSignInButton } from '@/core/google/GoogleSignInButton'
import { readGoogleProfile } from '@/core/google/readGoogleProfile'
import { routes } from '@/core/router/routes'
import { KvitButton } from '@/shared/components/KvitButton'
import { KvitDialog } from '@/shared/components/KvitDialog'

interface GoogleEmailTakenDialogProps {
  idToken: string
  onCredential: (idToken: string) => void
  onClose: () => void
}

export function GoogleEmailTakenDialog({
  idToken,
  onCredential,
  onClose,
}: GoogleEmailTakenDialogProps) {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const { email } = readGoogleProfile(idToken)

  return (
    <KvitDialog title={t('auth.googleTaken.title')} onClose={onClose}>
      <div className="flex flex-col gap-2 pt-2">
        <KvitButton onClick={() => navigate(routes.logIn, { state: { email } })}>
          {t('auth.googleTaken.logIn')}
        </KvitButton>
        <p className="pt-3 text-center text-[0.9375rem] font-semibold text-muted-foreground">
          {t('auth.googleTaken.another')}
        </p>
        <GoogleSignInButton onCredential={onCredential} />
        <KvitButton variant="link" onClick={onClose}>
          {t('common.close')}
        </KvitButton>
      </div>
    </KvitDialog>
  )
}
