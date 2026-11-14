import { cn } from 'cn'
import { useTranslation } from 'react-i18next'

interface KvitRatesSourceNoteProps {
  className?: string
}

export function KvitRatesSourceNote({ className }: KvitRatesSourceNoteProps) {
  const { t } = useTranslation()
  return (
    <p className={cn('text-[0.8125rem] text-pretty text-muted-foreground', className)}>
      {t('legal.ratesSource')}
    </p>
  )
}
