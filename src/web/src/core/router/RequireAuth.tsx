import { Navigate } from 'react-router'
import { routes } from './routes'

export function RequireAuth() {
  return <Navigate to={routes.welcome} replace />
}
