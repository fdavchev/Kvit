import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router'
import { toast } from 'sonner'
import { errorMessageKey } from '@/core/api/errors'
import { useSignedInMe } from '@/core/auth/useSignedInMe'
import { routes } from '@/core/router/routes'
import { KvitBackButton } from '@/shared/components/KvitBackButton'
import { KvitForm } from '@/shared/components/KvitForm'
import { KvitPasswordField } from '@/shared/components/KvitPasswordField'
import { KvitScreen } from '@/shared/components/KvitScreen'
import { KvitScreenTitle } from '@/shared/components/KvitScreenTitle'
import { useChangePassword } from '../hooks/useChangePassword'

export function ChangePasswordScreen() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const { mustChangePassword } = useSignedInMe()
  const changePassword = useChangePassword()
  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')

  function submit(): void {
    changePassword.mutate(
      { currentPassword, newPassword },
      {
        onSuccess: () => {
          toast.success(t('auth.changePassword.done'))
          navigate(routes.dashboard, { replace: true })
        },
      },
    )
  }

  return (
    <KvitScreen>
      <div className="flex min-h-12">
        {!mustChangePassword && <KvitBackButton to={routes.settings} />}
      </div>
      <KvitScreenTitle>{t('auth.changePassword.title')}</KvitScreenTitle>
      {mustChangePassword && (
        <p className="-mt-3 pb-7 text-pretty text-muted-foreground">
          {t('auth.changePassword.resetNote')}
        </p>
      )}
      <KvitForm
        submitLabel={t('auth.changePassword.submit')}
        isPending={changePassword.isPending}
        errorMessage={
          changePassword.isError ? t(errorMessageKey(changePassword.error)) : null
        }
        onSubmit={submit}
      >
        <KvitPasswordField
          id="change-password-current"
          label={t('auth.changePassword.current')}
          value={currentPassword}
          onChange={setCurrentPassword}
          autoComplete="current-password"
          enterKeyHint="next"
        />
        <KvitPasswordField
          id="change-password-new"
          label={t('auth.changePassword.new')}
          value={newPassword}
          onChange={setNewPassword}
          autoComplete="new-password"
          enterKeyHint="done"
          hint={t('auth.passwordHint')}
        />
      </KvitForm>
    </KvitScreen>
  )
}
