import { Navigate, useLocation } from 'react-router'
import { readRouterStateText } from '@/core/router/readRouterStateText'
import { routes } from '@/core/router/routes'
import { GoogleSignUpForm } from './GoogleSignUpForm'

export function GoogleSignUpScreen() {
  const location = useLocation()
  const idToken = readRouterStateText(location.state, 'idToken')

  if (idToken === null) {
    return <Navigate to={routes.welcome} replace />
  }
  return <GoogleSignUpForm idToken={idToken} />
}
