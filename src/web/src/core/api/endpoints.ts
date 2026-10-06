export const endpoints = {
  health: '/api/health',
  register: '/api/auth/register',
  logIn: '/api/auth/login',
  logOut: '/api/auth/logout',
  changePassword: '/api/auth/change-password',
  googleLogIn: '/api/auth/google',
  googleSignUp: '/api/auth/google/sign-up',
  setPassword: '/api/auth/set-password',
  me: '/api/me',
  meLanguage: '/api/me/language',
  groups: '/api/groups',
  invitePreview: '/api/invites/preview',
  inviteJoin: '/api/invites/join',
  categories: '/api/categories',
  group: (groupId: string) => `/api/groups/${groupId}`,
  groupRestore: (groupId: string) => `/api/groups/${groupId}/restore`,
  groupLeave: (groupId: string) => `/api/groups/${groupId}/leave`,
  groupMembers: (groupId: string) => `/api/groups/${groupId}/members`,
  groupMember: (groupId: string, memberId: string) => `/api/groups/${groupId}/members/${memberId}`,
  groupMemberLetBackIn: (groupId: string, memberId: string) =>
    `/api/groups/${groupId}/members/${memberId}/let-back-in`,
  groupMemberClaim: (groupId: string, memberId: string) =>
    `/api/groups/${groupId}/members/${memberId}/claim`,
  groupMemberUndoClaim: (groupId: string, memberId: string) =>
    `/api/groups/${groupId}/members/${memberId}/undo-claim`,
  groupOwner: (groupId: string) => `/api/groups/${groupId}/owner`,
  groupInviteReset: (groupId: string) => `/api/groups/${groupId}/invite/reset`,
  groupInviteUndoReset: (groupId: string) => `/api/groups/${groupId}/invite/undo-reset`,
  groupExpenses: (groupId: string) => `/api/groups/${groupId}/expenses`,
  groupExpensesDeleted: (groupId: string) => `/api/groups/${groupId}/expenses/deleted`,
  groupExpense: (groupId: string, expenseId: string) =>
    `/api/groups/${groupId}/expenses/${expenseId}`,
  groupExpenseRestore: (groupId: string, expenseId: string) =>
    `/api/groups/${groupId}/expenses/${expenseId}/restore`,
} as const
