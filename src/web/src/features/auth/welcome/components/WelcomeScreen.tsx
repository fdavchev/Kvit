import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { KvitButton } from '@/shared/components/KvitButton'
import { useApiHealth } from '../hooks/useApiHealth'
import { LanguageSwitch } from './LanguageSwitch'

export function WelcomeScreen() {
  const { t } = useTranslation()
  useApiHealth()
  const taglineTranslation = t('welcome.taglineTranslation')

  function showComingSoon() {
    toast(t('common.comingSoon'))
  }

  return (
    <main className="mx-auto flex min-h-dvh max-w-md flex-col px-6 pb-8">
      <div className="flex justify-end">
        <LanguageSwitch />
      </div>
      <div className="flex flex-1 flex-col items-center justify-center text-center">
        <h1 className="text-6xl font-extrabold tracking-tight text-primary">
          {t('app.name')}
        </h1>
        <p lang="mk" className="mt-4 text-2xl font-semibold">
          {t('welcome.tagline')}
        </p>
        {taglineTranslation !== '' && (
          <p className="text-base text-muted-foreground">
            {taglineTranslation}
          </p>
        )}
        <p className="mt-8 text-muted-foreground">{t('welcome.pitch')}</p>
      </div>
      <div className="flex flex-col gap-3">
        <KvitButton className="w-full" onClick={showComingSoon}>
          {t('welcome.continueWithGoogle')}
        </KvitButton>
        <KvitButton
          variant="secondary"
          className="w-full"
          onClick={showComingSoon}
        >
          {t('welcome.signUpWithEmail')}
        </KvitButton>
        <KvitButton variant="link" className="w-full" onClick={showComingSoon}>
          {t('welcome.haveAccount')}
        </KvitButton>
      </div>
    </main>
  )
}
