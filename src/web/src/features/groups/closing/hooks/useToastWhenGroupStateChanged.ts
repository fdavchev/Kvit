import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { errorMessageKey } from '@/core/api/errors'
import { isGroupStateChangedError } from '@/core/api/queryClient'

export function useToastWhenGroupStateChanged(): (error: unknown) => void {
  const { t } = useTranslation()

  return (error) => {
    if (isGroupStateChangedError(error)) {
      toast.error(t(errorMessageKey(error)))
    }
  }
}
