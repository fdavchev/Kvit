import { useMutation } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router'
import { toast } from 'sonner'
import { errorMessageKey, hasErrorCode } from '@/core/api/errors'
import { useFinishLogIn } from '@/core/auth/useFinishLogIn'
import { routes } from '@/core/router/routes'
import { googleLogIn } from '@/core/services/auth/authService'

export function useGoogleSignIn(
  onEmailTaken?: (idToken: string) => void,
): (idToken: string) => void {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const finishLogIn = useFinishLogIn()
  const logIn = useMutation({ mutationFn: googleLogIn, onSuccess: finishLogIn })

  return (idToken) => {
    logIn.mutate(idToken, {
      onSuccess: () => navigate(routes.dashboard, { replace: true }),
      onError: (error) => {
        if (hasErrorCode(error, 'AUTH_GOOGLE_NO_ACCOUNT')) {
          navigate(routes.googleSignUp, { state: { idToken } })
          return
        }
        if (onEmailTaken !== undefined && hasErrorCode(error, 'AUTH_GOOGLE_EMAIL_TAKEN')) {
          onEmailTaken(idToken)
          return
        }
        console.error('The Google log-in failed', error)
        toast.error(t(errorMessageKey(error)))
      },
    })
  }
}
