import { useTranslation } from 'react-i18next'
import { GoogleSignInButton } from '@/core/google/GoogleSignInButton'
import { joinTokenState } from '@/core/invites/joinRoundTrip'
import { routes } from '@/core/router/routes'
import { KvitLinkButton } from '@/shared/components/KvitLinkButton'

interface SignedOutJoinProps {
  token: string
  onGoogleCredential: (idToken: string) => void
}

export function SignedOutJoin({ token, onGoogleCredential }: SignedOutJoinProps) {
  const { t } = useTranslation()
  const state = joinTokenState(token)

  return (
    <>
      <p className="pt-5 text-center text-[0.9375rem] text-pretty text-muted-foreground">
        {t('join.signInHint')}
      </p>
      <div className="mt-auto flex flex-col gap-3 pt-8">
        <GoogleSignInButton onCredential={onGoogleCredential} />
        <p className="my-1 text-center text-[0.8125rem] font-semibold tracking-[0.02em] text-muted-foreground">
          {t('welcome.or')}
        </p>
        <KvitLinkButton to={routes.signUp} state={state} variant="secondary">
          {t('welcome.signUpWithEmail')}
        </KvitLinkButton>
        <KvitLinkButton to={routes.logIn} state={state} variant="link">
          {t('welcome.haveAccount')}
        </KvitLinkButton>
      </div>
    </>
  )
}
