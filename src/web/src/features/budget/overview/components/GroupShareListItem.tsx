import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { useLanguage } from '@/core/i18n/useLanguage'
import { routes } from '@/core/router/routes'
import type { PersonalCategory } from '@/core/services/personalCategories/personalCategoriesService'
import type { GroupShare } from '@/core/services/personalEntries/personalEntriesService'
import { expenseDayText } from '@/features/expenses/shared/expenseDayText'
import { KvitAmount } from '@/shared/components/KvitAmount'
import { KvitEmojiTile } from '@/shared/components/KvitEmojiTile'
import { categoryColor } from '@/shared/utils/categoryLooks'
import { moneyParts } from '@/shared/utils/formatMoney'
import { personalCategoryName } from '../../categories/personalCategoryName'

interface GroupShareListItemProps {
  share: GroupShare
  category: PersonalCategory | null
  today: string
}

export function GroupShareListItem({ share, category, today }: GroupShareListItemProps) {
  const { t } = useTranslation()
  const { language } = useLanguage()
  const categoryName = category === null ? null : personalCategoryName(category, t)
  const title = share.title ?? categoryName ?? share.groupName
  const dayText = expenseDayText(share.entryDate, today, language, t)

  return (
    <li>
      <Link
        to={routes.groupExpense(share.groupId, share.expenseId)}
        className="pressable flex min-h-16 w-full items-center gap-3 rounded-2xl px-3 py-2.5 text-left hover:bg-secondary-hover"
      >
        {category === null ? (
          <KvitEmojiTile emoji="" variant="dashed" />
        ) : (
          <KvitEmojiTile emoji={category.emoji} background={categoryColor(category)} />
        )}
        <span data-share-text className="flex min-w-0 flex-1 flex-col items-start gap-0.5">
          <span data-share-title className="max-w-full font-semibold break-words">
            {title}
          </span>
          <span
            data-share-group
            className="max-w-full rounded-lg border border-field-border px-2 text-xs leading-5 font-semibold break-words"
          >
            {share.groupName}
          </span>
          <span data-share-details className="max-w-full text-[0.8125rem] break-words text-muted-foreground">
            {share.title !== null && categoryName !== null ? `${categoryName} · ${dayText}` : dayText}
          </span>
        </span>
        <span data-share-amount className="text-right font-bold whitespace-nowrap">
          <KvitAmount parts={moneyParts(share.shareMinor, share.currency, language)} />
        </span>
      </Link>
    </li>
  )
}
