import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'
import { meQueryKey } from '@/core/auth/useMe'
import { useLanguage } from '@/core/i18n/useLanguage'
import { register, type RegisterInput } from '@/core/services/auth/authService'
import type { Me } from '@/core/services/me/meService'

export function useSignUp(): UseMutationResult<
  Me,
  Error,
  Omit<RegisterInput, 'language'>
> {
  const queryClient = useQueryClient()
  const { language } = useLanguage()

  return useMutation({
    mutationFn: (input: Omit<RegisterInput, 'language'>) =>
      register({ ...input, language }),
    onSuccess: (me) => {
      queryClient.setQueryData<Me | null>(meQueryKey, me)
    },
  })
}
