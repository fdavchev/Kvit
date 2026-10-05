import { useTranslation } from 'react-i18next'
import { routes } from '@/core/router/routes'
import { KvitLinkButton } from '@/shared/components/KvitLinkButton'

interface JoinMessageProps {
  message: string
}

export function JoinMessage({ message }: JoinMessageProps) {
  const { t } = useTranslation()

  return (
    <>
      <p
        role="alert"
        className="pt-10 text-center text-[1.0625rem] font-semibold text-pretty text-destructive"
      >
        {message}
      </p>
      <div className="mt-auto pt-8">
        <KvitLinkButton to={routes.dashboard} variant="secondary">
          {t('nav.home')}
        </KvitLinkButton>
      </div>
    </>
  )
}
