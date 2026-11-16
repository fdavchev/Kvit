import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router'
import { errorMessageKey } from '@/core/api/errors'
import { readGoogleProfile } from '@/core/google/readGoogleProfile'
import { pathAfterSignIn, pathBeforeSignIn } from '@/core/invites/joinRoundTrip'
import { displayNameRefusedCodes } from '@/features/auth/shared/accountFieldRefusals'
import { AgreeLine } from '@/features/legal/components/AgreeLine'
import { KvitBackButton } from '@/shared/components/KvitBackButton'
import { KvitForm } from '@/shared/components/KvitForm'
import { KvitScreen } from '@/shared/components/KvitScreen'
import { KvitScreenTitle } from '@/shared/components/KvitScreenTitle'
import { KvitTextField } from '@/shared/components/KvitTextField'
import { useFieldRefusals } from '@/shared/hooks/useFieldRefusals'
import { useGoogleSignUp } from '../hooks/useGoogleSignUp'

const refusedCodesOf = { displayName: displayNameRefusedCodes }

interface GoogleSignUpFormProps {
  idToken: string
  joinToken: string | null
}

export function GoogleSignUpForm({ idToken, joinToken }: GoogleSignUpFormProps) {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const signUp = useGoogleSignUp()
  const [displayName, setDisplayName] = useState<string>(
    () => readGoogleProfile(idToken).name,
  )
  const refusals = useFieldRefusals(refusedCodesOf, signUp.error)

  function changeDisplayName(nextDisplayName: string): void {
    setDisplayName(nextDisplayName)
    refusals.noteChange('displayName')
  }

  function submit(): void {
    refusals.noteSave()
    signUp.mutate(
      { idToken, displayName },
      { onSuccess: () => navigate(pathAfterSignIn(joinToken), { replace: true }) },
    )
  }

  return (
    <KvitScreen>
      <KvitBackButton to={pathBeforeSignIn(joinToken)} />
      <KvitScreenTitle>{t('auth.googleSignUp.title')}</KvitScreenTitle>
      <KvitForm
        submitLabel={t('auth.googleSignUp.submit')}
        isPending={signUp.isPending}
        errorMessage={signUp.isError ? t(errorMessageKey(signUp.error)) : null}
        onSubmit={submit}
        footer={<AgreeLine />}
      >
        <KvitTextField
          id="google-sign-up-name"
          label={t('auth.signUp.name')}
          value={displayName}
          onChange={changeDisplayName}
          invalid={refusals.isRefused('displayName')}
          autoComplete="name"
          autoCapitalize="words"
          enterKeyHint="done"
          hint={t('auth.googleSignUp.hint')}
        />
      </KvitForm>
    </KvitScreen>
  )
}
