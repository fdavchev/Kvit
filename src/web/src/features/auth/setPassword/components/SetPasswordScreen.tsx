import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Navigate, useNavigate } from 'react-router'
import { toast } from 'sonner'
import { errorMessageKey } from '@/core/api/errors'
import { useSignedInMe } from '@/core/auth/useSignedInMe'
import { routes } from '@/core/router/routes'
import { newPasswordRefusedCodes } from '@/features/auth/shared/accountFieldRefusals'
import { KvitBackButton } from '@/shared/components/KvitBackButton'
import { KvitForm } from '@/shared/components/KvitForm'
import { KvitPasswordField } from '@/shared/components/KvitPasswordField'
import { KvitScreen } from '@/shared/components/KvitScreen'
import { KvitScreenTitle } from '@/shared/components/KvitScreenTitle'
import { useFieldRefusals } from '@/shared/hooks/useFieldRefusals'
import { useSetPassword } from '../hooks/useSetPassword'

const refusedCodesOf = { newPassword: newPasswordRefusedCodes }

export function SetPasswordScreen() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const { hasPassword } = useSignedInMe()
  const setPassword = useSetPassword()
  const [newPassword, setNewPassword] = useState('')
  const refusals = useFieldRefusals(refusedCodesOf, setPassword.error)

  if (hasPassword && !setPassword.isSuccess) {
    return <Navigate to={routes.changePassword} replace />
  }

  function changeNewPassword(nextPassword: string): void {
    setNewPassword(nextPassword)
    refusals.noteChange('newPassword')
  }

  function submit(): void {
    refusals.noteSave()
    setPassword.mutate(newPassword, {
      onSuccess: () => {
        toast.success(t('auth.setPassword.done'))
        navigate(routes.settings, { replace: true })
      },
    })
  }

  return (
    <KvitScreen>
      <KvitBackButton to={routes.settings} />
      <KvitScreenTitle>{t('auth.setPassword.title')}</KvitScreenTitle>
      <KvitForm
        submitLabel={t('auth.setPassword.submit')}
        isPending={setPassword.isPending}
        errorMessage={
          setPassword.isError ? t(errorMessageKey(setPassword.error)) : null
        }
        onSubmit={submit}
      >
        <KvitPasswordField
          id="set-password-new"
          label={t('auth.changePassword.new')}
          value={newPassword}
          onChange={changeNewPassword}
          autoComplete="new-password"
          enterKeyHint="done"
          hint={t('auth.passwordHint')}
          invalid={refusals.isRefused('newPassword')}
        />
      </KvitForm>
    </KvitScreen>
  )
}
