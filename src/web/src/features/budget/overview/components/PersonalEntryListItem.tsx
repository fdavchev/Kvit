import { useTranslation } from 'react-i18next'
import { useLanguage } from '@/core/i18n/useLanguage'
import type { PersonalEntry } from '@/core/services/personalEntries/personalEntriesService'
import { expenseDayText } from '@/features/expenses/shared/expenseDayText'
import { KvitEmojiTile } from '@/shared/components/KvitEmojiTile'
import { categoryColor } from '@/shared/utils/categoryLooks'
import { formatMoney } from '@/shared/utils/formatMoney'
import { personalCategoryName } from '../../categories/personalCategoryName'

interface PersonalEntryListItemProps {
  entry: PersonalEntry
  today: string
  onOpen: (entry: PersonalEntry) => void
}

export function PersonalEntryListItem({ entry, today, onOpen }: PersonalEntryListItemProps) {
  const { t } = useTranslation()
  const { language } = useLanguage()

  return (
    <li>
      <button
        type="button"
        onClick={() => onOpen(entry)}
        className="pressable flex min-h-16 w-full items-center gap-3 rounded-2xl px-3 py-2.5 text-left hover:bg-secondary-hover"
      >
        <KvitEmojiTile emoji={entry.category.emoji} background={categoryColor(entry.category)} />
        <span data-entry-text className="flex min-w-0 flex-1 flex-col">
          <span className="line-clamp-2 font-semibold break-words">{personalCategoryName(entry.category, t)}</span>
          <span className="line-clamp-2 text-[0.8125rem] break-words text-muted-foreground">
            {expenseDayText(entry.entryDate, today, language, t)}
            {entry.note !== null && ` · ${entry.note}`}
          </span>
        </span>
        <span data-entry-amount className="text-right font-bold whitespace-nowrap">
          {formatMoney(entry.amountMinor, entry.currency, language)}
        </span>
      </button>
    </li>
  )
}
