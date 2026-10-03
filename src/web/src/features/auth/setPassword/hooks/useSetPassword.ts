import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'
import { updateCachedMe } from '@/core/auth/updateCachedMe'
import { setPassword } from '@/core/services/auth/authService'

export function useSetPassword(): UseMutationResult<void, Error, string> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: setPassword,
    onSuccess: () => {
      updateCachedMe(queryClient, { hasPassword: true })
    },
  })
}
