import { useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useLanguage } from '@/core/i18n/useLanguage'
import { KvitInlineError } from '@/shared/components/KvitInlineError'
import type { GoogleIdentity } from './googleIdentity'
import { loadGoogleIdentity } from './loadGoogleIdentity'
import { listenForGoogleCredential, startGoogleIdentity } from './startGoogleIdentity'

interface GoogleSignInButtonProps {
  onCredential: (idToken: string) => void
}

const maxButtonWidth = 400
const redrawAfterResizeMilliseconds = 200

const slotLooks =
  'flex h-11 justify-center overflow-y-clip [grid-area:1/1] [&>div]:grid [&>div>*]:[grid-area:1/1]'

export function GoogleSignInButton({ onCredential }: GoogleSignInButtonProps) {
  const { t } = useTranslation()
  const { language } = useLanguage()
  const rootRef = useRef<HTMLDivElement>(null)
  const slotRef = useRef<HTMLDivElement>(null)
  const onCredentialRef = useRef<(idToken: string) => void>(onCredential)
  const [identity, setIdentity] = useState<GoogleIdentity | null>(null)
  const [hasFailed, setHasFailed] = useState<boolean>(false)

  useEffect(() => {
    onCredentialRef.current = onCredential
  }, [onCredential])

  useEffect(() => listenForGoogleCredential((idToken) => onCredentialRef.current(idToken)), [])

  useEffect(() => {
    let isMounted = true
    loadGoogleIdentity()
      .then((loadedIdentity) => {
        if (!isMounted) {
          return
        }
        startGoogleIdentity(loadedIdentity)
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

  const frameTitle = t('welcome.googleButtonTitle')

  useEffect(() => {
    if (hasFailed) {
      return
    }
    const slot = slotOf(slotRef.current)
    const titleFrames = (): void => {
      slot.querySelectorAll('iframe').forEach((frame) => {
        frame.title = frameTitle
      })
    }
    titleFrames()
    const observer = new MutationObserver(titleFrames)
    observer.observe(slot, { childList: true, subtree: true })
    return () => {
      observer.disconnect()
    }
  }, [frameTitle, hasFailed])

  useEffect(() => {
    const root = rootRef.current
    if (identity === null || root === null) {
      return
    }
    const slot = slotOf(slotRef.current)
    let drawnWidth: number | null = null
    let redrawTimer: ReturnType<typeof setTimeout> | undefined
    const drawAtCurrentWidth = (): void => {
      const width = Math.min(maxButtonWidth, Math.floor(root.getBoundingClientRect().width))
      if (width === drawnWidth) {
        return
      }
      drawnWidth = width
      identity.renderButton(slot, {
        type: 'standard',
        theme: 'outline',
        size: 'large',
        text: 'continue_with',
        shape: 'pill',
        logo_alignment: 'left',
        width: String(width),
        locale: language,
      })
    }
    drawAtCurrentWidth()
    const observer = new ResizeObserver(() => {
      clearTimeout(redrawTimer)
      redrawTimer = setTimeout(drawAtCurrentWidth, redrawAfterResizeMilliseconds)
    })
    observer.observe(root)
    return () => {
      observer.disconnect()
      clearTimeout(redrawTimer)
    }
  }, [identity, language])

  if (hasFailed) {
    return <KvitInlineError message={t('welcome.googleUnavailable')} />
  }
  return (
    <div ref={rootRef} className="grid min-h-11 w-full scheme-light">
      <div ref={slotRef} data-language={language} className={slotLooks} />
    </div>
  )
}

function slotOf(slot: HTMLDivElement | null): HTMLDivElement {
  if (slot === null) {
    throw new Error('The Google button slot is not on the page')
  }
  return slot
}
