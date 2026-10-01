import { useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { useRouteError } from 'react-router'
import { KvitError } from '@/shared/components/KvitError'
import { KvitScreen } from '@/shared/components/KvitScreen'

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
    <KvitScreen className="items-center justify-center">
      <KvitError message={t('errors.generic')} onRetry={reloadPage} />
    </KvitScreen>
  )
}
