import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router'
import { GoogleSignInButton } from '@/core/google/GoogleSignInButton'
import { joinTokenState } from '@/core/invites/joinRoundTrip'
import { routes } from '@/core/router/routes'
import { KvitButton } from '@/shared/components/KvitButton'
import { KvitSheet } from '@/shared/components/KvitSheet'
import { useKvitSheet } from '@/shared/components/useKvitSheet'

interface JoinLogInSheetProps {
  token: string
  onGoogleCredential: (idToken: string) => void
  onClose: () => void
}

export function JoinLogInSheet({ token, onGoogleCredential, onClose }: JoinLogInSheetProps) {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const sheet = useKvitSheet()

  return (
    <KvitSheet title={t('auth.logIn.title')} onClose={onClose} actionsRef={sheet.actionsRef}>
      <div className="flex flex-col gap-2">
        <GoogleSignInButton onCredential={onGoogleCredential} />
        <KvitButton
          variant="secondary"
          className="bg-field"
          onClick={() => navigate(routes.logIn, { state: joinTokenState(token) })}
        >
          {t('join.guest.logInWithEmail')}
        </KvitButton>
        <KvitButton variant="link" onClick={sheet.close}>
          {t('settlements.cancel')}
        </KvitButton>
        <p className="text-center text-[0.9375rem] text-pretty text-muted-foreground">
          {t('join.guest.logInNote')}
        </p>
      </div>
    </KvitSheet>
  )
}
