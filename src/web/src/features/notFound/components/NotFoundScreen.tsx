import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router'
import { routes } from '@/core/router/routes'
import { KvitButton } from '@/shared/components/KvitButton'
import { KvitScreen } from '@/shared/components/KvitScreen'

export function NotFoundScreen() {
  const { t } = useTranslation()
  const navigate = useNavigate()

  return (
    <KvitScreen className="items-center justify-center gap-4 text-center">
      <h1 className="text-2xl font-bold">{t('notFound.title')}</h1>
      <p className="text-muted-foreground">{t('notFound.message')}</p>
      <KvitButton onClick={() => navigate(routes.dashboard)}>
        {t('notFound.goHome')}
      </KvitButton>
    </KvitScreen>
  )
}
