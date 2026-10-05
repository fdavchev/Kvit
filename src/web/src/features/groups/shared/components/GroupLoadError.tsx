import { useTranslation } from 'react-i18next'
import { errorMessageKey, isNotFoundError } from '@/core/api/errors'
import { routes } from '@/core/router/routes'
import { KvitError } from '@/shared/components/KvitError'
import { KvitLinkButton } from '@/shared/components/KvitLinkButton'

interface GroupLoadErrorProps {
  error: Error
  onRetry: () => void
}

export function GroupLoadError({ error, onRetry }: GroupLoadErrorProps) {
  const { t } = useTranslation()
  const message = t(errorMessageKey(error))

  if (isNotFoundError(error)) {
    return (
      <KvitError
        message={message}
        action={
          <KvitLinkButton to={routes.groups} variant="secondary">
            {t('groups.goToGroups')}
          </KvitLinkButton>
        }
      />
    )
  }
  return <KvitError message={message} onRetry={onRetry} />
}
