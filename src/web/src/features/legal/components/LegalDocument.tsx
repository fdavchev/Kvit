import { useTranslation } from 'react-i18next'
import { useLocation } from 'react-router'
import { readRouterStateText } from '@/core/router/readRouterStateText'
import { routes } from '@/core/router/routes'
import { balancesCardLook } from '@/features/groups/balances/balancesLooks'
import { KvitBackButton } from '@/shared/components/KvitBackButton'
import { KvitScreen } from '@/shared/components/KvitScreen'
import { KvitScreenTitle } from '@/shared/components/KvitScreenTitle'
import { cn } from '@/shared/utils/cn'
import { TextWithEmailLinks } from './TextWithEmailLinks'

interface LegalDocumentProps {
  document: 'terms' | 'privacy'
  sectionCount: number
}

const shortItemNumbers = [1, 2, 3, 4, 5] as const

const bodyLook = 'text-[0.9375rem] leading-[1.5] text-pretty text-muted-foreground'

export function LegalDocument({ document, sectionCount }: LegalDocumentProps) {
  const { t } = useTranslation()
  const location = useLocation()
  const backTo = readRouterStateText(location.state, 'from') ?? routes.welcome
  const sectionNumbers = Array.from({ length: sectionCount }, (_, index) => index + 1)

  return (
    <KvitScreen className="pb-16">
      <KvitBackButton to={backTo} />
      <KvitScreenTitle>{t(`${document}.title`)}</KvitScreenTitle>
      <p className="-mt-4 text-[0.9375rem] text-muted-foreground">{t(`${document}.updated`)}</p>
      <section className={cn(balancesCardLook, 'mt-6 flex flex-col gap-2')}>
        <h2 className="text-[1.0625rem] font-bold">{t(`${document}.short.title`)}</h2>
        <ul className="flex list-disc flex-col gap-1.5 pl-5 text-[0.9375rem] leading-[1.5] text-pretty">
          {shortItemNumbers.map((number) => (
            <li key={number}>
              <TextWithEmailLinks text={t(`${document}.short.items.${number}`)} />
            </li>
          ))}
        </ul>
      </section>
      {sectionNumbers.map((number) => (
        <section key={number}>
          <h2 className="mt-6 mb-1 text-[1.0625rem] font-bold">
            {t(`${document}.sections.${number}.heading`)}
          </h2>
          <p className={bodyLook}>
            <TextWithEmailLinks text={t(`${document}.sections.${number}.body`)} />
          </p>
        </section>
      ))}
      {document === 'terms' && (
        <p className="mt-6 text-[0.9375rem]">
          <a
            href="/third-party-licenses.txt"
            target="_blank"
            rel="noopener noreferrer"
            className="font-semibold text-link hover:underline"
          >
            {t('legal.licences')}
          </a>
        </p>
      )}
    </KvitScreen>
  )
}
