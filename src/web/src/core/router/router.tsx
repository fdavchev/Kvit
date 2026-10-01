import { createBrowserRouter, type RouteObject } from 'react-router'
import { ChangePasswordScreen } from '@/features/auth/changePassword/components/ChangePasswordScreen'
import { LogInScreen } from '@/features/auth/logIn/components/LogInScreen'
import { SignUpScreen } from '@/features/auth/signUp/components/SignUpScreen'
import { WelcomeScreen } from '@/features/auth/welcome/components/WelcomeScreen'
import { HomeScreen } from '@/features/home/components/HomeScreen'
import { NotFoundScreen } from '@/features/notFound/components/NotFoundScreen'
import { SettingsScreen } from '@/features/settings/components/SettingsScreen'
import { RequireAuth } from './RequireAuth'
import { RouteError } from './RouteError'
import { routes } from './routes'

export const routeObjects: RouteObject[] = [
  {
    errorElement: <RouteError />,
    children: [
      { path: routes.welcome, element: <WelcomeScreen /> },
      { path: routes.signUp, element: <SignUpScreen /> },
      { path: routes.logIn, element: <LogInScreen /> },
      {
        element: <RequireAuth />,
        children: [
          { path: routes.dashboard, element: <HomeScreen /> },
          { path: routes.settings, element: <SettingsScreen /> },
          { path: routes.changePassword, element: <ChangePasswordScreen /> },
        ],
      },
      { path: '*', element: <NotFoundScreen /> },
    ],
  },
]

export const router = createBrowserRouter(routeObjects)
