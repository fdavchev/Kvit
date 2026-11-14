import { useTranslation } from 'react-i18next'
import { useLocation } from 'react-router'
import { routes } from '@/core/router/routes'
import { KvitButton } from '@/shared/components/KvitButton'
import { KvitLinkButton } from '@/shared/components/KvitLinkButton'
import { KvitSheet } from '@/shared/components/KvitSheet'
import { useKvitSheet } from '@/shared/components/useKvitSheet'
import { useAcceptTerms } from '../hooks/useAcceptTerms'

interface TermsChangedSheetProps {
  onClose: () => void
}

export function TermsChangedSheet({ onClose }: TermsChangedSheetProps) {
  const { t } = useTranslation()
  const location = useLocation()
  const acceptTerms = useAcceptTerms()
  const sheet = useKvitSheet()

  return (
    <KvitSheet
      title={t('legal.termsMaterial.title')}
      onClose={onClose}
      actionsRef={sheet.actionsRef}
      isDismissible={false}
    >
      <p className="text-[0.9375rem] leading-[1.5] text-pretty text-muted-foreground">
        {t('legal.termsMaterial.body')}
      </p>
      <div className="flex flex-col gap-3">
        <KvitLinkButton to={routes.terms} state={{ from: location.pathname }} variant="secondary">
          {t('legal.termsMaterial.read')}
        </KvitLinkButton>
        <KvitButton
          disabled={acceptTerms.isPending}
          onClick={() => acceptTerms.mutate(undefined, { onSuccess: sheet.close })}
        >
          {t('legal.termsMaterial.agree')}
        </KvitButton>
      </div>
    </KvitSheet>
  )
}
