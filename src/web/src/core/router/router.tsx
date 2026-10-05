import { createBrowserRouter, type RouteObject } from 'react-router'
import { ChangePasswordScreen } from '@/features/auth/changePassword/components/ChangePasswordScreen'
import { GoogleSignUpScreen } from '@/features/auth/googleSignUp/components/GoogleSignUpScreen'
import { LogInScreen } from '@/features/auth/logIn/components/LogInScreen'
import { SetPasswordScreen } from '@/features/auth/setPassword/components/SetPasswordScreen'
import { SignUpScreen } from '@/features/auth/signUp/components/SignUpScreen'
import { WelcomeScreen } from '@/features/auth/welcome/components/WelcomeScreen'
import { GroupScreen } from '@/features/groups/group/components/GroupScreen'
import { JoinScreen } from '@/features/groups/join/components/JoinScreen'
import { GroupsScreen } from '@/features/groups/list/components/GroupsScreen'
import { RecentlyDeletedScreen } from '@/features/groups/list/components/RecentlyDeletedScreen'
import { MembersScreen } from '@/features/groups/members/components/MembersScreen'
import { NewGroupScreen } from '@/features/groups/newGroup/components/NewGroupScreen'
import { GroupSettingsScreen } from '@/features/groups/settings/components/GroupSettingsScreen'
import { HomeScreen } from '@/features/home/components/HomeScreen'
import { NotFoundScreen } from '@/features/notFound/components/NotFoundScreen'
import { PrivacyScreen } from '@/features/privacy/components/PrivacyScreen'
import { SettingsScreen } from '@/features/settings/components/SettingsScreen'
import { BottomBarLayout } from './BottomBarLayout'
import { RequireAuth } from './RequireAuth'
import { RouteError } from './RouteError'
import { routes } from './routes'

const groupIdPattern = ':groupId'
const inviteTokenPattern = ':token'

export const routeObjects: RouteObject[] = [
  {
    errorElement: <RouteError />,
    children: [
      { path: routes.welcome, element: <WelcomeScreen /> },
      { path: routes.signUp, element: <SignUpScreen /> },
      { path: routes.logIn, element: <LogInScreen /> },
      { path: routes.googleSignUp, element: <GoogleSignUpScreen /> },
      { path: routes.privacy, element: <PrivacyScreen /> },
      { path: routes.join(inviteTokenPattern), element: <JoinScreen /> },
      {
        element: <RequireAuth />,
        children: [
          {
            element: <BottomBarLayout />,
            children: [
              { path: routes.dashboard, element: <HomeScreen /> },
              { path: routes.groups, element: <GroupsScreen /> },
              { path: routes.recentlyDeletedGroups, element: <RecentlyDeletedScreen /> },
              { path: routes.settings, element: <SettingsScreen /> },
            ],
          },
          { path: routes.newGroup, element: <NewGroupScreen /> },
          { path: routes.group(groupIdPattern), element: <GroupScreen /> },
          { path: routes.groupSettings(groupIdPattern), element: <GroupSettingsScreen /> },
          { path: routes.groupMembers(groupIdPattern), element: <MembersScreen /> },
          { path: routes.changePassword, element: <ChangePasswordScreen /> },
          { path: routes.setPassword, element: <SetPasswordScreen /> },
        ],
      },
      { path: '*', element: <NotFoundScreen /> },
    ],
  },
]

export const router = createBrowserRouter(routeObjects)
