import { useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { useRouteError } from 'react-router'
import { KvitError } from '@/shared/components/KvitError'

function reloadPage(): void {
  window.location.reload()
}

export function RouteError() {
  const { t } = useTranslation()
  const error: unknown = useRouteError()

  useEffect(() => {
    console.error('A screen crashed while rendering', error)
  }, [error])

  return (
    <main className="mx-auto flex min-h-dvh max-w-md flex-col items-center justify-center px-6">
      <KvitError message={t('errors.generic')} onRetry={reloadPage} />
    </main>
  )
}
