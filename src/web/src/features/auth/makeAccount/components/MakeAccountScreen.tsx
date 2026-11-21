import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Navigate } from 'react-router'
import { errorMessageKey } from '@/core/api/errors'
import { useIsGuest } from '@/core/auth/useIsGuest'
import { GoogleSignInButton } from '@/core/google/GoogleSignInButton'
import { routes } from '@/core/router/routes'
import { emailRefusedCodes, newPasswordRefusedCodes } from '@/features/auth/shared/accountFieldRefusals'
import { GoogleEmailTakenDialog } from '@/features/auth/welcome/components/GoogleEmailTakenDialog'
import { AgreeLine } from '@/features/legal/components/AgreeLine'
import { KvitBackButton } from '@/shared/components/KvitBackButton'
import { KvitEmailField } from '@/shared/components/KvitEmailField'
import { KvitForm } from '@/shared/components/KvitForm'
import { KvitOrDivider } from '@/shared/components/KvitOrDivider'
import { KvitPasswordField } from '@/shared/components/KvitPasswordField'
import { KvitScreen } from '@/shared/components/KvitScreen'
import { KvitScreenTitle } from '@/shared/components/KvitScreenTitle'
import { useFieldRefusals } from '@/shared/hooks/useFieldRefusals'
import { useUpgradeGuest } from '../hooks/useUpgradeGuest'

const refusedCodesOf = { email: emailRefusedCodes, password: newPasswordRefusedCodes }

export function MakeAccountScreen() {
  const { t } = useTranslation()
  const isGuest = useIsGuest()
  const [takenIdToken, setTakenIdToken] = useState<string | null>(null)
  const upgrade = useUpgradeGuest(setTakenIdToken)
  const [email, setEmail] = useState<string>('')
  const [password, setPassword] = useState<string>('')
  const { emailError } = upgrade
  const refusals = useFieldRefusals(refusedCodesOf, emailError)

  if (!isGuest) {
    return <Navigate to={routes.settings} replace />
  }

  function changeEmail(nextEmail: string): void {
    setEmail(nextEmail)
    refusals.noteChange('email')
  }

  function changePassword(nextPassword: string): void {
    setPassword(nextPassword)
    refusals.noteChange('password')
  }

  function save(): void {
    refusals.noteSave()
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
          <>
            <AgreeLine />
            <p className="text-center text-[0.9375rem] text-pretty text-muted-foreground">
              {t('auth.makeAccount.keeps')}
            </p>
          </>
        }
      >
        <KvitEmailField
          id="make-account-email"
          label={t('auth.email')}
          value={email}
          onChange={changeEmail}
          invalid={refusals.isRefused('email')}
        />
        <KvitPasswordField
          id="make-account-password"
          label={t('auth.password')}
          value={password}
          onChange={changePassword}
          autoComplete="new-password"
          enterKeyHint="done"
          hint={t('auth.passwordHint')}
          invalid={refusals.isRefused('password')}
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
