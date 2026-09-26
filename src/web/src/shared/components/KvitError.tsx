import { useTranslation } from 'react-i18next'
import { KvitButton } from './KvitButton'

interface KvitErrorProps {
  message: string
  onRetry?: () => void
}

export function KvitError({ message, onRetry }: KvitErrorProps) {
  const { t } = useTranslation()
  return (
    <div role="alert" className="flex flex-col items-center gap-4 p-6 text-center">
      <p className="text-destructive">{message}</p>
      {onRetry && (
        <KvitButton variant="secondary" onClick={onRetry}>
          {t('common.retry')}
        </KvitButton>
      )}
    </div>
  )
}
