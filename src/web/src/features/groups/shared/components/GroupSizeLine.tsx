import { useTranslation } from 'react-i18next'
import { countedText } from '@/core/i18n/pluralForm'
import { useLanguage } from '@/core/i18n/useLanguage'
import type { Currency } from '@/shared/utils/formatMoney'

interface GroupSizeLineProps {
  memberCount: number
  currency: Currency
}

export function GroupSizeLine({ memberCount, currency }: GroupSizeLineProps) {
  const { t } = useTranslation()
  const { language } = useLanguage()
  return (
    <span className="text-[0.8125rem] text-muted-foreground">
      {`${countedText(t, 'groups.people', memberCount, language)} · ${currency}`}
    </span>
  )
}
