import { useTranslation } from 'react-i18next'
import type { OutboxItem } from '@/core/outbox/outboxItem'
import type { PersonalCategory } from '@/core/services/personalCategories/personalCategoriesService'
import { WaitingRow } from '@/features/outbox/components/WaitingRow'
import {
  amountOfItem,
  categoryOfWaitingItem,
  displayProblemOf,
  titleWithoutCategoryOf,
} from '@/features/outbox/waitingItemDisplay'
import { KvitEmojiTile } from '@/shared/components/KvitEmojiTile'
import { categoryColor } from '@/shared/utils/categoryLooks'
import { personalCategoryName } from '../../categories/personalCategoryName'
import { incomeEmoji, incomeTileColor } from './PersonalEntryListItem'

interface WaitingEntryRowProps {
  item: OutboxItem
  categories: readonly PersonalCategory[]
  onOpen: (item: OutboxItem) => void
}

export function WaitingEntryRow({ item, categories, onOpen }: WaitingEntryRowProps) {
  const { t } = useTranslation()
  const { category, unknownCategoryId } = categoryOfWaitingItem(item, categories)
  const amount = amountOfItem(item)

  function leadingTile() {
    if (category !== null) {
      return <KvitEmojiTile emoji={category.emoji} background={categoryColor(category)} />
    }
    if (item.display.entryKind === 'Income') {
      return <KvitEmojiTile emoji={incomeEmoji} background={incomeTileColor} />
    }
    return <KvitEmojiTile emoji="" variant="dashed" />
  }

  return (
    <li>
      <WaitingRow
        title={category === null ? titleWithoutCategoryOf(item, t) : personalCategoryName(category, t)}
        amountMinor={amount?.amountMinor}
        currency={amount?.currency}
        detail={category === null ? undefined : item.display.title}
        leading={leadingTile()}
        displayProblem={displayProblemOf(item, unknownCategoryId)}
        onOpen={() => onOpen(item)}
      />
    </li>
  )
}
