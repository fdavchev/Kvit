export const endpoints = {
  health: '/api/health',
  register: '/api/auth/register',
  logIn: '/api/auth/login',
  logOut: '/api/auth/logout',
  changePassword: '/api/auth/change-password',
  googleLogIn: '/api/auth/google',
  googleSignUp: '/api/auth/google/sign-up',
  setPassword: '/api/auth/set-password',
  upgradeGuest: '/api/auth/upgrade',
  googleUpgradeGuest: '/api/auth/google/upgrade',
  me: '/api/me',
  meLanguage: '/api/me/language',
  meActive: '/api/me/active',
  dashboard: '/api/dashboard',
  groups: '/api/groups',
  invitePreview: '/api/invites/preview',
  inviteJoin: '/api/invites/join',
  inviteJoinAsGuest: '/api/invites/join-as-guest',
  categories: '/api/categories',
  oneBill: '/api/groups/one-bill',
  group: (groupId: string) => `/api/groups/${groupId}`,
  groupActivity: (groupId: string) => `/api/groups/${groupId}/activity`,
  groupRestore: (groupId: string) => `/api/groups/${groupId}/restore`,
  groupLeave: (groupId: string) => `/api/groups/${groupId}/leave`,
  groupClosing: (groupId: string) => `/api/groups/${groupId}/closing`,
  groupClosingConfirm: (groupId: string) => `/api/groups/${groupId}/closing/confirm`,
  groupClosingObject: (groupId: string) => `/api/groups/${groupId}/closing/object`,
  groupUnlock: (groupId: string) => `/api/groups/${groupId}/unlock`,
  groupMembers: (groupId: string) => `/api/groups/${groupId}/members`,
  groupMember: (groupId: string, memberId: string) => `/api/groups/${groupId}/members/${memberId}`,
  groupMemberLetBackIn: (groupId: string, memberId: string) =>
    `/api/groups/${groupId}/members/${memberId}/let-back-in`,
  groupMemberLetInAgain: (groupId: string, memberId: string) =>
    `/api/groups/${groupId}/members/${memberId}/let-in-again`,
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
  groupBalances: (groupId: string) => `/api/groups/${groupId}/balances`,
  groupSettlements: (groupId: string) => `/api/groups/${groupId}/settlements`,
  groupSettlement: (groupId: string, settlementId: string) =>
    `/api/groups/${groupId}/settlements/${settlementId}`,
  groupSettlementConfirm: (groupId: string, settlementId: string) =>
    `/api/groups/${groupId}/settlements/${settlementId}/confirm`,
  groupSettlementReject: (groupId: string, settlementId: string) =>
    `/api/groups/${groupId}/settlements/${settlementId}/reject`,
  groupSettlementCancel: (groupId: string, settlementId: string) =>
    `/api/groups/${groupId}/settlements/${settlementId}/cancel`,
  groupSettlementRestore: (groupId: string, settlementId: string) =>
    `/api/groups/${groupId}/settlements/${settlementId}/restore`,
} as const
