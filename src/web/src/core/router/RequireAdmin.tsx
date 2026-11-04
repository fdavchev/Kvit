import { Outlet } from 'react-router'
import { useIsAdmin } from '@/core/auth/useIsAdmin'
import { NotFoundScreen } from '@/features/notFound/components/NotFoundScreen'

export function RequireAdmin() {
  const isAdmin = useIsAdmin()
  if (!isAdmin) {
    return <NotFoundScreen />
  }
  return <Outlet />
}
