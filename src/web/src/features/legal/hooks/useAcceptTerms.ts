import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { errorMessageKey } from '@/core/api/errors'
import { updateCachedMe } from '@/core/auth/updateCachedMe'
import { legalVersion } from '@/core/legal/legalInfo'
import { acceptTerms } from '@/core/services/me/meService'

export function useAcceptTerms(): UseMutationResult<void, Error, void> {
  const { t } = useTranslation()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: acceptTerms,
    onSuccess: () => {
      updateCachedMe(queryClient, {
        termsVersion: legalVersion,
        termsUpToDate: true,
        termsUpdateIsMaterial: false,
      })
    },
    onError: (error) => {
      toast.error(t(errorMessageKey(error)))
    },
  })
}
