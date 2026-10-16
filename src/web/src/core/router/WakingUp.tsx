import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { KvitScreen } from '@/shared/components/KvitScreen'
import { KvitSpinner } from '@/shared/components/KvitSpinner'

const wakingUpDelayMilliseconds = 2000

export function WakingUp() {
  const { t } = useTranslation()
  const [isTakingLong, setIsTakingLong] = useState<boolean>(false)

  useEffect(() => {
    const timer = setTimeout(() => setIsTakingLong(true), wakingUpDelayMilliseconds)
    return () => clearTimeout(timer)
  }, [])

  return (
    <KvitScreen className="items-center justify-center">
      <div role="status" className="flex flex-col items-center gap-4 p-6 text-center">
        <KvitSpinner />
        {isTakingLong ? (
          <>
            <p className="text-lg font-bold">{t('wakingUp.title')}</p>
            <p className="text-pretty text-muted-foreground">{t('wakingUp.hint')}</p>
          </>
        ) : (
          <span className="sr-only">{t('common.loading')}</span>
        )}
      </div>
    </KvitScreen>
  )
}
