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
} as const
