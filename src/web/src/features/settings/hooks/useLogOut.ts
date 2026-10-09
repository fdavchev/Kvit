import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'
import { meQueryKey } from '@/core/auth/useMe'
import { logOut } from '@/core/services/auth/authService'
import type { Me } from '@/core/services/me/meService'

export function useLogOut(): UseMutationResult<void, Error, void> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: logOut,
    onSuccess: () => {
      queryClient.getMutationCache().clear()
      queryClient.setQueryData<Me | null>(meQueryKey, null)
    },
  })
}
