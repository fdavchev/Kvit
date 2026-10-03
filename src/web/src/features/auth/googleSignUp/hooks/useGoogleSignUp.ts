import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'
import { meQueryKey } from '@/core/auth/useMe'
import { useLanguage } from '@/core/i18n/useLanguage'
import { googleSignUp, type GoogleSignUpInput } from '@/core/services/auth/authService'
import type { Me } from '@/core/services/me/meService'

export function useGoogleSignUp(): UseMutationResult<
  Me,
  Error,
  Omit<GoogleSignUpInput, 'language'>
> {
  const queryClient = useQueryClient()
  const { language } = useLanguage()

  return useMutation({
    mutationFn: (input: Omit<GoogleSignUpInput, 'language'>) =>
      googleSignUp({ ...input, language }),
    onSuccess: (me) => {
      queryClient.setQueryData<Me | null>(meQueryKey, me)
    },
  })
}
