import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'
import { meQueryKey } from '@/core/auth/useMe'
import { useLanguage } from '@/core/i18n/useLanguage'
import { logIn, type LogInInput } from '@/core/services/auth/authService'
import type { Me } from '@/core/services/me/meService'

export function useLogIn(): UseMutationResult<Me, Error, LogInInput> {
  const queryClient = useQueryClient()
  const { changeLanguage } = useLanguage()

  return useMutation({
    mutationFn: logIn,
    onSuccess: async (me) => {
      queryClient.setQueryData<Me | null>(meQueryKey, me)
      await changeLanguage(me.language)
    },
  })
}
