import { createBrowserRouter, type RouteObject } from 'react-router'
import { LogInScreen } from '@/features/auth/logIn/components/LogInScreen'
import { SignUpScreen } from '@/features/auth/signUp/components/SignUpScreen'
import { WelcomeScreen } from '@/features/auth/welcome/components/WelcomeScreen'
import { HomeScreen } from '@/features/home/components/HomeScreen'
import { NotFoundScreen } from '@/features/notFound/components/NotFoundScreen'
import { adminOnlyHandle } from './adminOnlyRoutes'
import { BottomBarLayout } from './BottomBarLayout'
import { lazyScreen, type LazyScreen } from './lazyScreen'
import { RequireAccount } from './RequireAccount'
import { RequireAuth } from './RequireAuth'
import { RequireSignedOut } from './RequireSignedOut'
import { RouteError } from './RouteError'
import { routes } from './routes'

const AdminStatsScreen = lazyScreen(
  async () => (await import('@/features/admin/stats/components/AdminStatsScreen')).AdminStatsScreen,
)
const OwnerToolsScreen = lazyScreen(
  async () => (await import('@/features/admin/tools/components/OwnerToolsScreen')).OwnerToolsScreen,
)
const ChangePasswordScreen = lazyScreen(
  async () => (await import('@/features/auth/changePassword/components/ChangePasswordScreen')).ChangePasswordScreen,
)
const GoogleSignUpScreen = lazyScreen(
  async () => (await import('@/features/auth/googleSignUp/components/GoogleSignUpScreen')).GoogleSignUpScreen,
)
const MakeAccountScreen = lazyScreen(
  async () => (await import('@/features/auth/makeAccount/components/MakeAccountScreen')).MakeAccountScreen,
)
const SetPasswordScreen = lazyScreen(
  async () => (await import('@/features/auth/setPassword/components/SetPasswordScreen')).SetPasswordScreen,
)
const BudgetScreen = lazyScreen(
  async () => (await import('@/features/budget/overview/components/BudgetScreen')).BudgetScreen,
)
const RecurringIncomeScreen = lazyScreen(
  async () => (await import('@/features/budget/recurring/components/RecurringIncomeScreen')).RecurringIncomeScreen,
)
const BudgetSummaryScreen = lazyScreen(
  async () => (await import('@/features/budget/summary/components/BudgetSummaryScreen')).BudgetSummaryScreen,
)
const AddExpenseScreen = lazyScreen(
  async () => (await import('@/features/expenses/add/components/AddExpenseScreen')).AddExpenseScreen,
)
const RecentlyDeletedExpensesScreen = lazyScreen(
  async () => (await import('@/features/expenses/deleted/components/RecentlyDeletedExpensesScreen')).RecentlyDeletedExpensesScreen,
)
const ExpenseDetailScreen = lazyScreen(
  async () => (await import('@/features/expenses/detail/components/ExpenseDetailScreen')).ExpenseDetailScreen,
)
const EditExpenseScreen = lazyScreen(
  async () => (await import('@/features/expenses/edit/components/EditExpenseScreen')).EditExpenseScreen,
)
const OneBillScreen = lazyScreen(
  async () => (await import('@/features/expenses/oneBill/components/OneBillScreen')).OneBillScreen,
)
const GroupScreen = lazyScreen(
  async () => (await import('@/features/groups/group/components/GroupScreen')).GroupScreen,
)
const JoinScreen = lazyScreen(
  async () => (await import('@/features/groups/join/components/JoinScreen')).JoinScreen,
)
const GroupsScreen = lazyScreen(
  async () => (await import('@/features/groups/list/components/GroupsScreen')).GroupsScreen,
)
const RecentlyDeletedScreen = lazyScreen(
  async () => (await import('@/features/groups/list/components/RecentlyDeletedScreen')).RecentlyDeletedScreen,
)
const MembersScreen = lazyScreen(
  async () => (await import('@/features/groups/members/components/MembersScreen')).MembersScreen,
)
const NewGroupChoiceScreen = lazyScreen(
  async () => (await import('@/features/groups/newGroup/components/NewGroupChoiceScreen')).NewGroupChoiceScreen,
)
const NewGroupScreen = lazyScreen(
  async () => (await import('@/features/groups/newGroup/components/NewGroupScreen')).NewGroupScreen,
)
const GroupSettingsScreen = lazyScreen(
  async () => (await import('@/features/groups/settings/components/GroupSettingsScreen')).GroupSettingsScreen,
)
const PrivacyScreen = lazyScreen(
  async () => (await import('@/features/privacy/components/PrivacyScreen')).PrivacyScreen,
)
const SettingsScreen = lazyScreen(
  async () => (await import('@/features/settings/components/SettingsScreen')).SettingsScreen,
)
const TermsScreen = lazyScreen(
  async () => (await import('@/features/terms/components/TermsScreen')).TermsScreen,
)

const lazyScreens: LazyScreen[] = [
  AdminStatsScreen,
  OwnerToolsScreen,
  ChangePasswordScreen,
  GoogleSignUpScreen,
  MakeAccountScreen,
  SetPasswordScreen,
  BudgetScreen,
  RecurringIncomeScreen,
  BudgetSummaryScreen,
  AddExpenseScreen,
  RecentlyDeletedExpensesScreen,
  ExpenseDetailScreen,
  EditExpenseScreen,
  OneBillScreen,
  GroupScreen,
  JoinScreen,
  GroupsScreen,
  RecentlyDeletedScreen,
  MembersScreen,
  NewGroupChoiceScreen,
  NewGroupScreen,
  GroupSettingsScreen,
  PrivacyScreen,
  SettingsScreen,
  TermsScreen,
]

const groupIdPattern = ':groupId'
const expenseIdPattern = ':expenseId'
const inviteTokenPattern = ':token'

export const routeObjects: RouteObject[] = [
  {
    errorElement: <RouteError />,
    children: [
      {
        element: <RequireSignedOut />,
        children: [
          { path: routes.welcome, element: <WelcomeScreen /> },
          { path: routes.signUp, element: <SignUpScreen /> },
          { path: routes.logIn, element: <LogInScreen /> },
        ],
      },
      { path: routes.googleSignUp, element: <GoogleSignUpScreen /> },
      { path: routes.privacy, element: <PrivacyScreen /> },
      { path: routes.terms, element: <TermsScreen /> },
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
              {
                element: <RequireAccount />,
                children: [
                  { path: routes.budget, element: <BudgetScreen /> },
                  { path: routes.budgetRepeating, element: <RecurringIncomeScreen /> },
                  { path: routes.budgetSummary, element: <BudgetSummaryScreen /> },
                ],
              },
              { path: routes.settings, element: <SettingsScreen /> },
              {
                handle: adminOnlyHandle,
                children: [
                  { path: routes.admin, element: <AdminStatsScreen /> },
                  { path: routes.adminTools, element: <OwnerToolsScreen /> },
                ],
              },
            ],
          },
          {
            element: <RequireAccount />,
            children: [
              { path: routes.newGroup, element: <NewGroupChoiceScreen /> },
              { path: routes.newGroupGroup, element: <NewGroupScreen /> },
              { path: routes.newBill, element: <OneBillScreen /> },
              { path: routes.changePassword, element: <ChangePasswordScreen /> },
              { path: routes.setPassword, element: <SetPasswordScreen /> },
            ],
          },
          { path: routes.group(groupIdPattern), element: <GroupScreen /> },
          { path: routes.groupActivity(groupIdPattern), element: <GroupScreen /> },
          { path: routes.groupBalances(groupIdPattern), element: <GroupScreen /> },
          { path: routes.groupTotals(groupIdPattern), element: <GroupScreen /> },
          { path: routes.groupSettings(groupIdPattern), element: <GroupSettingsScreen /> },
          { path: routes.groupMembers(groupIdPattern), element: <MembersScreen /> },
          { path: routes.groupExpenseNew(groupIdPattern), element: <AddExpenseScreen /> },
          {
            path: routes.groupExpensesDeleted(groupIdPattern),
            element: <RecentlyDeletedExpensesScreen />,
          },
          {
            path: routes.groupExpense(groupIdPattern, expenseIdPattern),
            element: <ExpenseDetailScreen />,
          },
          {
            path: routes.groupExpenseEdit(groupIdPattern, expenseIdPattern),
            element: <EditExpenseScreen />,
          },
          { path: routes.makeAccount, element: <MakeAccountScreen /> },
        ],
      },
      { path: '*', element: <NotFoundScreen /> },
    ],
  },
]

export function preloadScreens(): Promise<void> {
  return Promise.all(lazyScreens.map((screen) => screen.preload())).then(() => undefined)
}

export const router = createBrowserRouter(routeObjects)
