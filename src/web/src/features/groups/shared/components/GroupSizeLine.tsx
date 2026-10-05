import { useTranslation } from 'react-i18next'
import type { Currency } from '@/shared/utils/formatMoney'

interface GroupSizeLineProps {
  memberCount: number
  currency: Currency
}

export function GroupSizeLine({ memberCount, currency }: GroupSizeLineProps) {
  const { t } = useTranslation()
  return (
    <span className="text-[0.8125rem] text-muted-foreground">
      {`${t('groups.people', { count: memberCount })} · ${currency}`}
    </span>
  )
}
