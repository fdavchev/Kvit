import { createBrowserRouter } from 'react-router'
import { WelcomeScreen } from '@/features/auth/welcome/components/WelcomeScreen'
import { NotFoundScreen } from '@/features/notFound/components/NotFoundScreen'
import { RequireAuth } from './RequireAuth'
import { routes } from './routes'

export const router = createBrowserRouter([
  { path: routes.welcome, element: <WelcomeScreen /> },
  { path: routes.dashboard, element: <RequireAuth /> },
  { path: '*', element: <NotFoundScreen /> },
])
