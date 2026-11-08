import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'
import { meQueryKey } from '@/core/auth/useMe'
import { deleteMyAccount, type DeleteAccountNameChoice, type Me } from '@/core/services/me/meService'

export function useDeleteMyAccount(): UseMutationResult<void, Error, DeleteAccountNameChoice> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: deleteMyAccount,
    onSuccess: () => {
      queryClient.getMutationCache().clear()
      queryClient.setQueryData<Me | null>(meQueryKey, null)
    },
  })
}
