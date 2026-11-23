import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'
import { updateCachedMe } from '@/core/auth/updateCachedMe'
import type { Language } from '@/core/i18n/language'
import { changeLanguage } from '@/core/services/me/meService'

export function useChangeLanguage(): UseMutationResult<void, Error, Language> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: changeLanguage,
    scope: { id: 'accountLanguage' },
    onSuccess: (_answer, language) => {
      updateCachedMe(queryClient, { language })
    },
  })
}
