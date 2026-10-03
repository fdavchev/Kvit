import { useMutation, type UseMutationResult } from '@tanstack/react-query'
import { useFinishLogIn } from '@/core/auth/useFinishLogIn'
import { logIn, type LogInInput } from '@/core/services/auth/authService'
import type { Me } from '@/core/services/me/meService'

export function useLogIn(): UseMutationResult<Me, Error, LogInInput> {
  const finishLogIn = useFinishLogIn()

  return useMutation({
    mutationFn: logIn,
    onSuccess: finishLogIn,
  })
}
