import { useTranslation } from 'react-i18next'
import { Navigate, Outlet, useLocation } from 'react-router'
import { errorMessageKey } from '@/core/api/errors'
import { useMe } from '@/core/auth/useMe'
import { KvitError } from '@/shared/components/KvitError'
import { KvitLoading } from '@/shared/components/KvitLoading'
import { routes } from './routes'

export function RequireAuth() {
  const { t } = useTranslation()
  const location = useLocation()
  const meQuery = useMe()

  if (meQuery.isPending) {
    return <KvitLoading />
  }
  if (meQuery.isError) {
    return (
      <KvitError
        message={t(errorMessageKey(meQuery.error))}
        onRetry={() => void meQuery.refetch()}
      />
    )
  }
  const me = meQuery.data
  if (me === null) {
    return <Navigate to={routes.welcome} replace />
  }
  if (me.mustChangePassword && location.pathname !== routes.changePassword) {
    return <Navigate to={routes.changePassword} replace />
  }
  return <Outlet />
}
