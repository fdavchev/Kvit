import { useTranslation } from 'react-i18next'

export function ClosingCancelsNote() {
  const { t } = useTranslation()

  return <p className="pt-2 pb-3 text-[0.9375rem] text-pretty text-muted-foreground">{t('closing.addCancels')}</p>
}
