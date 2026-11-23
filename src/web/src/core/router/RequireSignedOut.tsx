import { useState } from 'react'
import { Navigate, Outlet, useLocation } from 'react-router'
import { useCachedMe } from '@/core/auth/useCachedMe'
import { pathAfterSignIn, readJoinToken } from '@/core/invites/joinRoundTrip'

export function RequireSignedOut() {
  const location = useLocation()
  const me = useCachedMe().data
  const [wasSignedInOnArrival] = useState<boolean>(me !== undefined && me !== null)

  if (wasSignedInOnArrival) {
    return <Navigate to={pathAfterSignIn(readJoinToken(location.state))} replace />
  }
  return <Outlet />
}
