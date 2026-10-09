import { useTranslation } from 'react-i18next'

export function KvitUpdatingNote() {
  const { t } = useTranslation()
  return (
    <p
      role="status"
      className="rounded-full bg-card px-3 py-1 text-xs font-semibold text-muted-foreground shadow-[0_1px_3px_var(--track-shadow)]"
    >
      {t('common.updating')}
    </p>
  )
}
