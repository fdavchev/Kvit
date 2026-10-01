import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { useSignedInMe } from '@/core/auth/useSignedInMe'
import { routes } from '@/core/router/routes'
import { KvitScreen } from '@/shared/components/KvitScreen'

export function HomeScreen() {
  const { t } = useTranslation()
  const me = useSignedInMe()

  return (
    <KvitScreen>
      <header className="flex items-center justify-between gap-4 pt-11 pb-7">
        <h1 className="text-[2.25rem] leading-[1.1] font-extrabold tracking-[-0.025em]">
          {t('home.greeting', { name: me.displayName })}
        </h1>
        <Link
          to={routes.settings}
          aria-label={t('settings.title')}
          className="pressable grid size-12 flex-none place-items-center rounded-full bg-avatar text-xl font-extrabold text-avatar-foreground hover:bg-avatar-hover"
        >
          {me.displayName.charAt(0).toUpperCase()}
        </Link>
      </header>
      <section className="grid flex-1 place-items-center pt-8 pb-[18dvh]">
        <p className="max-w-[26ch] text-center text-balance text-muted-foreground">
          {t('home.placeholder')}
        </p>
      </section>
    </KvitScreen>
  )
}
