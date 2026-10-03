import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router'
import { errorMessageKey } from '@/core/api/errors'
import { readGoogleProfile } from '@/core/google/readGoogleProfile'
import { routes } from '@/core/router/routes'
import { KvitBackButton } from '@/shared/components/KvitBackButton'
import { KvitForm } from '@/shared/components/KvitForm'
import { KvitScreen } from '@/shared/components/KvitScreen'
import { KvitScreenTitle } from '@/shared/components/KvitScreenTitle'
import { KvitTextField } from '@/shared/components/KvitTextField'
import { useGoogleSignUp } from '../hooks/useGoogleSignUp'

interface GoogleSignUpFormProps {
  idToken: string
}

export function GoogleSignUpForm({ idToken }: GoogleSignUpFormProps) {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const signUp = useGoogleSignUp()
  const [displayName, setDisplayName] = useState<string>(
    () => readGoogleProfile(idToken).name,
  )

  function submit(): void {
    signUp.mutate(
      { idToken, displayName },
      { onSuccess: () => navigate(routes.dashboard, { replace: true }) },
    )
  }

  return (
    <KvitScreen>
      <KvitBackButton to={routes.welcome} />
      <KvitScreenTitle>{t('auth.googleSignUp.title')}</KvitScreenTitle>
      <KvitForm
        submitLabel={t('auth.googleSignUp.submit')}
        isPending={signUp.isPending}
        errorMessage={signUp.isError ? t(errorMessageKey(signUp.error)) : null}
        onSubmit={submit}
      >
        <KvitTextField
          id="google-sign-up-name"
          label={t('auth.signUp.name')}
          value={displayName}
          onChange={setDisplayName}
          autoComplete="name"
          autoCapitalize="words"
          enterKeyHint="done"
          hint={t('auth.googleSignUp.hint')}
        />
      </KvitForm>
    </KvitScreen>
  )
}
