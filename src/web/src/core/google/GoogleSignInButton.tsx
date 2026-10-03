import { useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { languages, type Language } from '@/core/i18n/language'
import { useLanguage } from '@/core/i18n/useLanguage'
import { KvitInlineError } from '@/shared/components/KvitInlineError'
import { googleClientId } from './googleClientId'
import type { GoogleIdentity } from './googleIdentity'
import { loadGoogleIdentity } from './loadGoogleIdentity'

interface GoogleSignInButtonProps {
  onCredential: (idToken: string) => void
}

const maxButtonWidth = 400

const slotLooks =
  'flex h-11 justify-center overflow-y-clip [grid-area:1/1] [&>div]:grid [&>div>*]:[grid-area:1/1]'

const inactiveSlotLooks = 'opacity-0 pointer-events-none'

export function GoogleSignInButton({ onCredential }: GoogleSignInButtonProps) {
  const { t } = useTranslation()
  const { language } = useLanguage()
  const rootRef = useRef<HTMLDivElement>(null)
  const slotsRef = useRef<Record<Language, HTMLDivElement | null>>({ en: null, mk: null })
  const onCredentialRef = useRef<(idToken: string) => void>(onCredential)
  const [identity, setIdentity] = useState<GoogleIdentity | null>(null)
  const [hasFailed, setHasFailed] = useState<boolean>(false)

  useEffect(() => {
    onCredentialRef.current = onCredential
  }, [onCredential])

  useEffect(() => {
    let isMounted = true
    loadGoogleIdentity()
      .then((loadedIdentity) => {
        if (!isMounted) {
          return
        }
        loadedIdentity.initialize({
          client_id: googleClientId(),
          callback: (response) => onCredentialRef.current(response.credential),
        })
        setIdentity(loadedIdentity)
      })
      .catch((error: unknown) => {
        console.error('Could not start Google sign-in', error)
        if (isMounted) {
          setHasFailed(true)
        }
      })
    return () => {
      isMounted = false
    }
  }, [])

  useEffect(() => {
    const root = rootRef.current
    if (identity === null || root === null) {
      return
    }
    const width = Math.min(maxButtonWidth, Math.floor(root.getBoundingClientRect().width))
    languages.forEach((slotLanguage) => {
      const slot = slotsRef.current[slotLanguage]
      if (slot === null) {
        throw new Error(`The Google button slot for "${slotLanguage}" is not on the page`)
      }
      identity.renderButton(slot, {
        type: 'standard',
        theme: 'outline',
        size: 'large',
        text: 'continue_with',
        shape: 'pill',
        logo_alignment: 'left',
        width: String(width),
        locale: slotLanguage,
      })
    })
  }, [identity])

  if (hasFailed) {
    return <KvitInlineError message={t('welcome.googleUnavailable')} />
  }
  return (
    <div ref={rootRef} className="grid min-h-11 w-full scheme-light">
      {languages.map((slotLanguage) => {
        const isActive = slotLanguage === language
        return (
          <div
            key={slotLanguage}
            ref={(element) => {
              slotsRef.current[slotLanguage] = element
            }}
            data-language={slotLanguage}
            aria-hidden={isActive ? undefined : true}
            inert={!isActive}
            className={isActive ? slotLooks : `${slotLooks} ${inactiveSlotLooks}`}
          />
        )
      })}
    </div>
  )
}
