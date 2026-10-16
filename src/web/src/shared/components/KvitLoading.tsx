import { useTranslation } from 'react-i18next'
import { KvitSpinner } from './KvitSpinner'

export function KvitLoading() {
  const { t } = useTranslation()
  return (
    <div role="status" className="flex justify-center p-6">
      <KvitSpinner />
      <span className="sr-only">{t('common.loading')}</span>
    </div>
  )
}
