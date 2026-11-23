import { useTranslation } from 'react-i18next'
import { Navigate, Outlet, useLocation } from 'react-router'
import { errorMessageKey } from '@/core/api/errors'
import { useIsWaitingAfterWakeVisit } from '@/core/api/useIsWaitingAfterWakeVisit'
import { useMe } from '@/core/auth/useMe'
import { useRecordActive } from '@/features/home/hooks/useRecordActive'
import { TermsNotice } from '@/features/legal/components/TermsNotice'
import { NotFoundScreen } from '@/features/notFound/components/NotFoundScreen'
import { PausedOutbox, SignedInOutbox } from '@/features/outbox/components/SignedInOutbox'
import { KvitError } from '@/shared/components/KvitError'
import { KvitScreen } from '@/shared/components/KvitScreen'
import { useIsAdminOnlyAddress } from './adminOnlyRoutes'
import { DesktopRailLayout } from './DesktopRailLayout'
import { routes } from './routes'
import { SuspendedScreen } from './SuspendedScreen'
import { WakingUp } from './WakingUp'

export function RequireAuth() {
  const { t } = useTranslation()
  const location = useLocation()
  const meQuery = useMe()
  const isWaitingAfterWakeVisit = useIsWaitingAfterWakeVisit()
  const isAdminOnlyAddress = useIsAdminOnlyAddress()

  const me = meQuery.data
  if (me === undefined) {
    if (meQuery.isError && !meQuery.isFetching && !isWaitingAfterWakeVisit) {
      return (
        <KvitScreen className="items-center justify-center">
          <KvitError
            message={t(errorMessageKey(meQuery.error))}
            onRetry={() => void meQuery.refetch()}
          />
        </KvitScreen>
      )
    }
    return <WakingUp />
  }
  if (me === null) {
    return <Navigate to={routes.welcome} replace />
  }
  if (me.isSuspended) {
    return (
      <PausedOutbox>
        <SuspendedScreen />
      </PausedOutbox>
    )
  }
  if (me.mustChangePassword && location.pathname !== routes.changePassword) {
    return <Navigate to={routes.changePassword} replace />
  }
  if (me.mustChangePassword) {
    return <Outlet />
  }
  if (isAdminOnlyAddress && !me.isAdmin) {
    return <NotFoundScreen />
  }
  return <SignedInOutlet />
}

function SignedInOutlet() {
  useRecordActive()
  return (
    <SignedInOutbox>
      <DesktopRailLayout />
      <TermsNotice />
    </SignedInOutbox>
  )
}
