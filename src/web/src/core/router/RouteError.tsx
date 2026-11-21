import { useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { useRouteError } from 'react-router'
import { KvitError } from '@/shared/components/KvitError'
import { KvitScreen } from '@/shared/components/KvitScreen'
import { ScreenLoadError } from './lazyScreen'

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
      <KvitError
        message={t(error instanceof ScreenLoadError ? 'errors.pageLoadFailed' : 'errors.generic')}
        onRetry={reloadPage}
      />
    </KvitScreen>
  )
}
