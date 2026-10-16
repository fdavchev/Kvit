import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { KvitSpinner } from './KvitSpinner'

interface KvitLoadingProps {
  children?: ReactNode
}

export function KvitLoading({ children }: KvitLoadingProps) {
  const { t } = useTranslation()
  return (
    <div role="status" className={children === undefined ? 'flex justify-center p-6' : undefined}>
      {children ?? <KvitSpinner />}
      <span className="sr-only">{t('common.loading')}</span>
    </div>
  )
}
