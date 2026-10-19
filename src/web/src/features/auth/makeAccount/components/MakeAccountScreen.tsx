import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Navigate } from 'react-router'
import { errorMessageKey, hasErrorCode } from '@/core/api/errors'
import { useIsGuest } from '@/core/auth/useIsGuest'
import { GoogleSignInButton } from '@/core/google/GoogleSignInButton'
import { routes } from '@/core/router/routes'
import { GoogleEmailTakenDialog } from '@/features/auth/welcome/components/GoogleEmailTakenDialog'
import { KvitBackButton } from '@/shared/components/KvitBackButton'
import { KvitEmailField } from '@/shared/components/KvitEmailField'
import { KvitForm } from '@/shared/components/KvitForm'
import { KvitOrDivider } from '@/shared/components/KvitOrDivider'
import { KvitPasswordField } from '@/shared/components/KvitPasswordField'
import { KvitScreen } from '@/shared/components/KvitScreen'
import { KvitScreenTitle } from '@/shared/components/KvitScreenTitle'
import { useUpgradeGuest } from '../hooks/useUpgradeGuest'

const emailRefusedCodes = ['AUTH_EMAIL_INVALID', 'AUTH_EMAIL_TAKEN']
const passwordRefusedCodes = ['AUTH_PASSWORD_TOO_WEAK']

export function MakeAccountScreen() {
  const { t } = useTranslation()
  const isGuest = useIsGuest()
  const [takenIdToken, setTakenIdToken] = useState<string | null>(null)
  const upgrade = useUpgradeGuest(setTakenIdToken)
  const [email, setEmail] = useState<string>('')
  const [password, setPassword] = useState<string>('')
  const [isEmailChangedSinceSave, setIsEmailChangedSinceSave] = useState<boolean>(false)
  const [isPasswordChangedSinceSave, setIsPasswordChangedSinceSave] = useState<boolean>(false)
  const { emailError } = upgrade

  if (!isGuest) {
    return <Navigate to={routes.settings} replace />
  }

  const isEmailRefused: boolean =
    emailRefusedCodes.some((code) => hasErrorCode(emailError, code)) && !isEmailChangedSinceSave
  const isPasswordRefused: boolean =
    passwordRefusedCodes.some((code) => hasErrorCode(emailError, code)) && !isPasswordChangedSinceSave

  function changeEmail(nextEmail: string): void {
    setEmail(nextEmail)
    setIsEmailChangedSinceSave(true)
  }

  function changePassword(nextPassword: string): void {
    setPassword(nextPassword)
    setIsPasswordChangedSinceSave(true)
  }

  function save(): void {
    setIsEmailChangedSinceSave(false)
    setIsPasswordChangedSinceSave(false)
    upgrade.upgradeWithEmail({ email, password })
  }

  return (
    <KvitScreen>
      <KvitBackButton to={routes.settings} />
      <KvitScreenTitle>{t('settings.guest.makeAccount')}</KvitScreenTitle>
      <div className="flex flex-col gap-3 pb-4">
        <GoogleSignInButton onCredential={upgrade.upgradeWithGoogle} />
        <KvitOrDivider label={t('welcome.or')} />
      </div>
      <KvitForm
        submitLabel={t('groupSettings.save')}
        isPending={upgrade.isPending}
        errorMessage={emailError === null ? null : t(errorMessageKey(emailError))}
        onSubmit={save}
        footer={
          <p className="text-center text-[0.9375rem] text-pretty text-muted-foreground">
            {t('auth.makeAccount.keeps')}
          </p>
        }
      >
        <KvitEmailField
          id="make-account-email"
          label={t('auth.email')}
          value={email}
          onChange={changeEmail}
          invalid={isEmailRefused}
        />
        <KvitPasswordField
          id="make-account-password"
          label={t('auth.password')}
          value={password}
          onChange={changePassword}
          autoComplete="new-password"
          enterKeyHint="done"
          hint={t('auth.passwordHint')}
          invalid={isPasswordRefused}
        />
      </KvitForm>
      {takenIdToken !== null && (
        <GoogleEmailTakenDialog
          idToken={takenIdToken}
          onCredential={upgrade.upgradeWithGoogle}
          onClose={() => setTakenIdToken(null)}
          offersPasswordLogIn={false}
        />
      )}
    </KvitScreen>
  )
}
