import { useTranslation } from 'react-i18next'
import { useLanguage } from '@/core/i18n/useLanguage'
import type { PersonalEntry } from '@/core/services/personalEntries/personalEntriesService'
import { expenseDayText } from '@/features/expenses/shared/expenseDayText'
import { KvitEmojiTile } from '@/shared/components/KvitEmojiTile'
import { categoryColor } from '@/shared/utils/categoryLooks'
import { cn } from '@/shared/utils/cn'
import { formatMoney } from '@/shared/utils/formatMoney'
import { formatSignedMoney } from '@/shared/utils/formatSignedMoney'
import { personalCategoryName } from '../../categories/personalCategoryName'

interface PersonalEntryListItemProps {
  entry: PersonalEntry
  today: string
  onOpen: (entry: PersonalEntry) => void
}

const incomeEmoji = '\u{1F4B0}'
const incomeTileColor = 'var(--category-green)'

export function PersonalEntryListItem({ entry, today, onOpen }: PersonalEntryListItemProps) {
  const { t } = useTranslation()
  const { language } = useLanguage()
  const { category } = entry
  const dayText = expenseDayText(entry.entryDate, today, language, t)

  return (
    <li>
      <button
        type="button"
        onClick={() => onOpen(entry)}
        className="pressable flex min-h-16 w-full items-center gap-3 rounded-2xl px-3 py-2.5 text-left hover:bg-secondary-hover"
      >
        {category === null ? (
          <KvitEmojiTile emoji={incomeEmoji} background={incomeTileColor} />
        ) : (
          <KvitEmojiTile emoji={category.emoji} background={categoryColor(category)} />
        )}
        <span data-entry-text className="flex min-w-0 flex-1 flex-col">
          <span className="line-clamp-2 font-semibold break-words">
            {category === null ? (entry.note ?? t('budget.income.listTitle')) : personalCategoryName(category, t)}
          </span>
          <span className="line-clamp-2 text-[0.8125rem] break-words text-muted-foreground">
            {dayText}
            {category !== null && entry.note !== null && ` · ${entry.note}`}
          </span>
        </span>
        <span
          data-entry-amount
          className={cn('text-right font-bold whitespace-nowrap', category === null && 'text-positive')}
        >
          {category === null
            ? formatSignedMoney(entry.amountMinor, entry.currency, language)
            : formatMoney(entry.amountMinor, entry.currency, language)}
        </span>
      </button>
    </li>
  )
}
