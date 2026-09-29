import { createBrowserRouter, type RouteObject } from 'react-router'
import { WelcomeScreen } from '@/features/auth/welcome/components/WelcomeScreen'
import { NotFoundScreen } from '@/features/notFound/components/NotFoundScreen'
import { RequireAuth } from './RequireAuth'
import { RouteError } from './RouteError'
import { routes } from './routes'

export const routeObjects: RouteObject[] = [
  {
    errorElement: <RouteError />,
    children: [
      { path: routes.welcome, element: <WelcomeScreen /> },
      { path: routes.dashboard, element: <RequireAuth /> },
      { path: '*', element: <NotFoundScreen /> },
    ],
  },
]

export const router = createBrowserRouter(routeObjects)
