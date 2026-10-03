import { useTranslation } from 'react-i18next'
import { useLocation } from 'react-router'
import { readRouterStateText } from '@/core/router/readRouterStateText'
import { routes } from '@/core/router/routes'
import { KvitBackButton } from '@/shared/components/KvitBackButton'
import { KvitScreen } from '@/shared/components/KvitScreen'
import { KvitScreenTitle } from '@/shared/components/KvitScreenTitle'

const sectionNumbers = [1, 2, 3, 4, 5, 6, 7] as const

export function PrivacyScreen() {
  const { t } = useTranslation()
  const location = useLocation()
  const backTo = readRouterStateText(location.state, 'from') ?? routes.welcome

  return (
    <KvitScreen className="pb-16">
      <KvitBackButton to={backTo} />
      <KvitScreenTitle>{t('privacy.title')}</KvitScreenTitle>
      <p className="-mt-4 text-[0.9375rem] text-muted-foreground">{t('privacy.updated')}</p>
      {sectionNumbers.map((number) => (
        <section key={number}>
          <h2 className="mt-6 mb-1 text-[1.0625rem] font-bold">
            {t(`privacy.sections.${number}.heading`)}
          </h2>
          <p className="text-[0.9375rem] leading-[1.5] text-pretty text-muted-foreground">
            {t(`privacy.sections.${number}.body`)}
          </p>
        </section>
      ))}
    </KvitScreen>
  )
}
