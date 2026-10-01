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
import { KvitTextField } from '@/shared/components/KvitTextField'
import { useSignUp } from '../hooks/useSignUp'

export function SignUpScreen() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const signUp = useSignUp()
  const [displayName, setDisplayName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')

  function submit(): void {
    signUp.mutate(
      { displayName, email, password },
      { onSuccess: () => navigate(routes.dashboard, { replace: true }) },
    )
  }

  return (
    <KvitScreen>
      <KvitBackButton to={routes.welcome} />
      <KvitScreenTitle>{t('auth.signUp.title')}</KvitScreenTitle>
      <KvitForm
        submitLabel={t('auth.signUp.title')}
        isPending={signUp.isPending}
        errorMessage={signUp.isError ? t(errorMessageKey(signUp.error)) : null}
        onSubmit={submit}
        footer={
          <KvitLinkButton to={routes.logIn} variant="underlinedLink">
            {t('welcome.haveAccount')}
          </KvitLinkButton>
        }
      >
        <KvitTextField
          id="sign-up-name"
          label={t('auth.signUp.name')}
          value={displayName}
          onChange={setDisplayName}
          autoComplete="name"
          autoCapitalize="words"
          enterKeyHint="next"
        />
        <KvitEmailField
          id="sign-up-email"
          label={t('auth.email')}
          value={email}
          onChange={setEmail}
        />
        <KvitPasswordField
          id="sign-up-password"
          label={t('auth.password')}
          value={password}
          onChange={setPassword}
          autoComplete="new-password"
          enterKeyHint="done"
          hint={t('auth.passwordHint')}
        />
      </KvitForm>
    </KvitScreen>
  )
}
