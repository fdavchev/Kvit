import { useTranslation } from 'react-i18next'

export function KvitLoading() {
  const { t } = useTranslation()
  return (
    <div role="status" className="flex justify-center p-6">
      <span
        aria-hidden="true"
        className="size-8 animate-spin rounded-full border-4 border-muted border-t-primary"
      />
      <span className="sr-only">{t('common.loading')}</span>
    </div>
  )
}
