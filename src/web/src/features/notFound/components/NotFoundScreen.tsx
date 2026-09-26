import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router'
import { routes } from '@/core/router/routes'
import { KvitButton } from '@/shared/components/KvitButton'

export function NotFoundScreen() {
  const { t } = useTranslation()
  const navigate = useNavigate()

  return (
    <main className="mx-auto flex min-h-dvh max-w-md flex-col items-center justify-center gap-4 px-6 text-center">
      <h1 className="text-2xl font-bold">{t('notFound.title')}</h1>
      <p className="text-muted-foreground">{t('notFound.message')}</p>
      <KvitButton className="w-full" onClick={() => navigate(routes.dashboard)}>
        {t('notFound.goHome')}
      </KvitButton>
    </main>
  )
}
