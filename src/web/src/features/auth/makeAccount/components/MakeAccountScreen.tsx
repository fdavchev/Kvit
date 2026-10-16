import { useTranslation } from 'react-i18next'
import { routes } from '@/core/router/routes'
import { KvitBackButton } from '@/shared/components/KvitBackButton'
import { KvitScreen } from '@/shared/components/KvitScreen'
import { KvitScreenTitle } from '@/shared/components/KvitScreenTitle'

export function MakeAccountScreen() {
  const { t } = useTranslation()

  return (
    <KvitScreen>
      <KvitBackButton to={routes.settings} />
      <KvitScreenTitle>{t('settings.guest.makeAccount')}</KvitScreenTitle>
    </KvitScreen>
  )
}
