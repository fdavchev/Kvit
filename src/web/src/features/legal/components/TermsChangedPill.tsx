import { useRef } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useLocation } from 'react-router'
import { routes } from '@/core/router/routes'
import { KvitButton } from '@/shared/components/KvitButton'
import { kvitGlassPanelLook } from '@/shared/components/kvitGlassLooks'
import { useElementHeightOnPage } from '@/shared/hooks/useElementHeightOnPage'
import { cn } from '@/shared/utils/cn'
import { useAcceptTerms } from '../hooks/useAcceptTerms'

const termsPillSpaceProperty = '--terms-pill-space'
const gapAbovePillPx = 10

export function TermsChangedPill() {
  const { t } = useTranslation()
  const location = useLocation()
  const acceptTerms = useAcceptTerms()
  const pillRef = useRef<HTMLDivElement>(null)
  useElementHeightOnPage(pillRef, termsPillSpaceProperty, gapAbovePillPx)

  return (
    <div
      ref={pillRef}
      data-terms-pill
      role="status"
      className="pointer-events-none fixed inset-x-0 bottom-[calc(max(calc(var(--bottom-bar-height)+4px),calc(env(safe-area-inset-bottom)+8px))+var(--outbox-bar-space))] z-20 mx-auto w-full max-w-md px-4 motion-safe:animate-outbox-bar-in lg:left-(--rail-width)"
    >
      <div
        className={cn(
          'pointer-events-auto flex min-h-12 items-center gap-2 rounded-2xl py-1 pr-1.5 pl-4 text-[0.9375rem] leading-tight font-semibold',
          kvitGlassPanelLook,
        )}
      >
        <span className="min-w-0 flex-1">{t('legal.termsUpdated')}</span>
        <Link
          to={routes.terms}
          state={{ from: location.pathname }}
          className="pressable inline-flex min-h-11 min-w-11 flex-none items-center justify-center rounded-xl px-2 text-link hover:underline"
        >
          {t('legal.termsRead')}
        </Link>
        <KvitButton
          className="min-h-11 w-auto min-w-11 flex-none rounded-full px-4 text-[0.9375rem]"
          disabled={acceptTerms.isPending}
          onClick={() => acceptTerms.mutate()}
        >
          {t('legal.termsOk')}
        </KvitButton>
      </div>
    </div>
  )
}
