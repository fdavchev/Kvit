import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useLocation, useNavigate } from 'react-router'
import { errorMessageKey, hasErrorCode } from '@/core/api/errors'
import {
  joinTokenState,
  pathAfterSignIn,
  pathBeforeSignIn,
  readJoinToken,
} from '@/core/invites/joinRoundTrip'
import { readRouterStateText } from '@/core/router/readRouterStateText'
import { routes } from '@/core/router/routes'
import { useGoogleSignIn } from '@/features/auth/google/hooks/useGoogleSignIn'
import { KvitBackButton } from '@/shared/components/KvitBackButton'
import { KvitEmailField } from '@/shared/components/KvitEmailField'
import { KvitForm } from '@/shared/components/KvitForm'
import { KvitLinkButton } from '@/shared/components/KvitLinkButton'
import { KvitPasswordField } from '@/shared/components/KvitPasswordField'
import { KvitScreen } from '@/shared/components/KvitScreen'
import { KvitScreenTitle } from '@/shared/components/KvitScreenTitle'
import { useLogIn } from '../hooks/useLogIn'
import { UsesGoogleDialog } from './UsesGoogleDialog'

export function LogInScreen() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const location = useLocation()
  const joinToken = readJoinToken(location.state)
  const logIn = useLogIn()
  const signInWithGoogle = useGoogleSignIn(joinToken)
  const [email, setEmail] = useState<string>(
    () => readRouterStateText(location.state, 'email') ?? '',
  )
  const [password, setPassword] = useState('')
  const usesGoogle = hasErrorCode(logIn.error, 'AUTH_USES_GOOGLE')

  function submit(): void {
    logIn.mutate(
      { email, password },
      { onSuccess: () => navigate(pathAfterSignIn(joinToken), { replace: true }) },
    )
  }

  return (
    <KvitScreen>
      <KvitBackButton to={pathBeforeSignIn(joinToken)} />
      <div className="flex flex-1 flex-col justify-center pb-12">
        <KvitScreenTitle>{t('auth.logIn.title')}</KvitScreenTitle>
        <KvitForm
          submitLabel={t('auth.logIn.title')}
          isPending={logIn.isPending}
          errorMessage={
            logIn.isError && !usesGoogle ? t(errorMessageKey(logIn.error)) : null
          }
          onSubmit={submit}
          footer={
            <>
              <p className="text-center text-[0.9375rem] text-pretty text-muted-foreground">
                {t('auth.logIn.forgot')}
              </p>
              <KvitLinkButton
                to={routes.signUp}
                state={joinTokenState(joinToken)}
                variant="underlinedLink"
              >
                {t('auth.logIn.noAccount')}
              </KvitLinkButton>
            </>
          }
        >
          <KvitEmailField
            id="log-in-email"
            label={t('auth.email')}
            value={email}
            onChange={setEmail}
          />
          <KvitPasswordField
            id="log-in-password"
            label={t('auth.password')}
            value={password}
            onChange={setPassword}
            autoComplete="current-password"
            enterKeyHint="go"
          />
        </KvitForm>
      </div>
      {usesGoogle && (
        <UsesGoogleDialog
          onCredential={signInWithGoogle}
          onClose={() => logIn.reset()}
        />
      )}
    </KvitScreen>
  )
}
