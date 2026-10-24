import { useQueryClient } from '@tanstack/react-query'
import { useEffect, useRef } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { catchUpFailedMessageKey, catchUpOnOpen } from '../recurringIncomeCache'

export function useCatchUpOnOpen(): void {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const hasStartedRef = useRef<boolean>(false)

  useEffect(() => {
    if (hasStartedRef.current) {
      return
    }
    hasStartedRef.current = true
    catchUpOnOpen(queryClient).catch((error: unknown) => {
      console.error('The catch-up of repeating income failed when the Budget tab opened', error)
      toast.error(t(catchUpFailedMessageKey))
    })
  }, [queryClient, t])
}
