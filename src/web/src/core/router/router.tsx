import { createBrowserRouter, type RouteObject } from 'react-router'
import { ChangePasswordScreen } from '@/features/auth/changePassword/components/ChangePasswordScreen'
import { GoogleSignUpScreen } from '@/features/auth/googleSignUp/components/GoogleSignUpScreen'
import { LogInScreen } from '@/features/auth/logIn/components/LogInScreen'
import { SetPasswordScreen } from '@/features/auth/setPassword/components/SetPasswordScreen'
import { SignUpScreen } from '@/features/auth/signUp/components/SignUpScreen'
import { WelcomeScreen } from '@/features/auth/welcome/components/WelcomeScreen'
import { HomeScreen } from '@/features/home/components/HomeScreen'
import { NotFoundScreen } from '@/features/notFound/components/NotFoundScreen'
import { PrivacyScreen } from '@/features/privacy/components/PrivacyScreen'
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
      { path: routes.googleSignUp, element: <GoogleSignUpScreen /> },
      { path: routes.privacy, element: <PrivacyScreen /> },
      {
        element: <RequireAuth />,
        children: [
          { path: routes.dashboard, element: <HomeScreen /> },
          { path: routes.settings, element: <SettingsScreen /> },
          { path: routes.changePassword, element: <ChangePasswordScreen /> },
          { path: routes.setPassword, element: <SetPasswordScreen /> },
        ],
      },
      { path: '*', element: <NotFoundScreen /> },
    ],
  },
]

export const router = createBrowserRouter(routeObjects)
