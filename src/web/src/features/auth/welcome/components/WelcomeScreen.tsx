import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { GoogleSignInButton } from '@/core/google/GoogleSignInButton'
import type { Language } from '@/core/i18n/language'
import { useLanguage } from '@/core/i18n/useLanguage'
import { routes } from '@/core/router/routes'
import { useGoogleSignIn } from '@/features/auth/google/hooks/useGoogleSignIn'
import { KvitLanguageSwitch } from '@/shared/components/KvitLanguageSwitch'
import { KvitLinkButton } from '@/shared/components/KvitLinkButton'
import { KvitScreen } from '@/shared/components/KvitScreen'
import { KvitThemeToggle } from '@/shared/components/KvitThemeToggle'
import { useApiHealth } from '../hooks/useApiHealth'
import { GoogleEmailTakenDialog } from './GoogleEmailTakenDialog'

export function WelcomeScreen() {
  const { t } = useTranslation()
  const { language, changeLanguage } = useLanguage()
  useApiHealth()
  const [takenIdToken, setTakenIdToken] = useState<string | null>(null)
  const signInWithGoogle = useGoogleSignIn(setTakenIdToken)
  const taglineTranslation = t('welcome.taglineTranslation')

  async function switchLanguage(newLanguage: Language): Promise<void> {
    try {
      await changeLanguage(newLanguage)
    } catch (error) {
      console.error('Could not change the language', error)
      toast.error(t('errors.generic'))
    }
  }

  return (
    <KvitScreen>
      <header className="flex flex-1 flex-col">
        <div className="flex items-center justify-end gap-2">
          <KvitThemeToggle />
          <KvitLanguageSwitch
            language={language}
            onChange={(newLanguage) => void switchLanguage(newLanguage)}
          />
        </div>
        <div className="flex flex-1 flex-col items-center justify-center py-8 text-center">
          <h1 className="text-[5rem] leading-[1.05] font-extrabold tracking-[-0.03em] text-wordmark">
            {t('app.name')}
          </h1>
          <p
            lang="mk"
            className="mt-5 text-[1.625rem] leading-[1.2] font-semibold tracking-[-0.015em]"
          >
            {t('welcome.tagline')}
          </p>
          {taglineTranslation !== '' && (
            <p className="mt-1 text-muted-foreground">{taglineTranslation}</p>
          )}
        </div>
      </header>
      <div className="flex flex-col gap-3">
        <GoogleSignInButton onCredential={signInWithGoogle} />
        <p className="my-1 text-center text-[0.8125rem] font-semibold tracking-[0.02em] text-muted-foreground">
          {t('welcome.or')}
        </p>
        <KvitLinkButton to={routes.signUp} variant="secondary">
          {t('welcome.signUpWithEmail')}
        </KvitLinkButton>
        <KvitLinkButton to={routes.logIn} variant="link">
          {t('welcome.haveAccount')}
        </KvitLinkButton>
        <KvitLinkButton
          to={routes.privacy}
          state={{ from: routes.welcome }}
          variant="smallLink"
        >
          {t('common.privacy')}
        </KvitLinkButton>
      </div>
      {takenIdToken !== null && (
        <GoogleEmailTakenDialog
          idToken={takenIdToken}
          onCredential={signInWithGoogle}
          onClose={() => setTakenIdToken(null)}
        />
      )}
    </KvitScreen>
  )
}
