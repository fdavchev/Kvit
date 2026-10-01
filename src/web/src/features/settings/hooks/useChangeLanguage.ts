import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'
import { updateCachedMe } from '@/core/auth/updateCachedMe'
import type { Language } from '@/core/i18n/language'
import { useLanguage } from '@/core/i18n/useLanguage'
import { changeLanguage } from '@/core/services/me/meService'

export function useChangeLanguage(): UseMutationResult<void, Error, Language> {
  const queryClient = useQueryClient()
  const { changeLanguage: changeScreenLanguage } = useLanguage()

  return useMutation({
    mutationFn: changeLanguage,
    onSuccess: async (_answer, language) => {
      updateCachedMe(queryClient, { language })
      await changeScreenLanguage(language)
    },
  })
}
