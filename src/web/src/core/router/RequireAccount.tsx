import { Navigate, Outlet } from 'react-router'
import { useIsGuest } from '@/core/auth/useIsGuest'
import { routes } from './routes'

export function RequireAccount() {
  const isGuest = useIsGuest()
  if (isGuest) {
    return <Navigate to={routes.settings} replace />
  }
  return <Outlet />
}
