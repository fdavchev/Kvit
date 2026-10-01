import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'
import { updateCachedMe } from '@/core/auth/updateCachedMe'
import { changePassword, type ChangePasswordInput } from '@/core/services/auth/authService'

export function useChangePassword(): UseMutationResult<
  void,
  Error,
  ChangePasswordInput
> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: changePassword,
    onSuccess: () => {
      updateCachedMe(queryClient, { mustChangePassword: false })
    },
  })
}
