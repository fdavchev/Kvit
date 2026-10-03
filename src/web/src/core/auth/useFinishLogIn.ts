import { useQueryClient } from '@tanstack/react-query'
import { useLanguage } from '@/core/i18n/useLanguage'
import type { Me } from '@/core/services/me/meService'
import { meQueryKey } from './useMe'

export function useFinishLogIn(): (me: Me) => Promise<void> {
  const queryClient = useQueryClient()
  const { changeLanguage } = useLanguage()

  return async (me) => {
    queryClient.setQueryData<Me | null>(meQueryKey, me)
    await changeLanguage(me.language)
  }
}
