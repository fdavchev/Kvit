import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router'
import { errorMessageKey } from '@/core/api/errors'
import { routes } from '@/core/router/routes'
import { KvitBackButton } from '@/shared/components/KvitBackButton'
import { KvitEmailField } from '@/shared/components/KvitEmailField'
import { KvitForm } from '@/shared/components/KvitForm'
import { KvitLinkButton } from '@/shared/components/KvitLinkButton'
import { KvitPasswordField } from '@/shared/components/KvitPasswordField'
import { KvitScreen } from '@/shared/components/KvitScreen'
import { KvitScreenTitle } from '@/shared/components/KvitScreenTitle'
import { useLogIn } from '../hooks/useLogIn'

export function LogInScreen() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const logIn = useLogIn()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')

  function submit(): void {
    logIn.mutate(
      { email, password },
      { onSuccess: () => navigate(routes.dashboard, { replace: true }) },
    )
  }

  return (
    <KvitScreen>
      <KvitBackButton to={routes.welcome} />
      <div className="flex flex-1 flex-col justify-center pb-12">
        <KvitScreenTitle>{t('auth.logIn.title')}</KvitScreenTitle>
        <KvitForm
          submitLabel={t('auth.logIn.title')}
          isPending={logIn.isPending}
          errorMessage={logIn.isError ? t(errorMessageKey(logIn.error)) : null}
          onSubmit={submit}
          footer={
            <>
              <p className="text-center text-[0.9375rem] text-pretty text-muted-foreground">
                {t('auth.logIn.forgot')}
              </p>
              <KvitLinkButton to={routes.signUp} variant="underlinedLink">
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
    </KvitScreen>
  )
}
