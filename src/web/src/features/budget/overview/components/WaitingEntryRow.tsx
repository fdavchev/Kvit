import { useTranslation } from 'react-i18next'
import type { OutboxItem } from '@/core/outbox/outboxItem'
import type { PersonalCategory } from '@/core/services/personalCategories/personalCategoriesService'
import { WaitingRow } from '@/features/outbox/components/WaitingRow'
import { amountOfItem, titleWithoutCategoryOf } from '@/features/outbox/waitingItemDisplay'
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
  const category = categoryOfEntry(item, categories)
  const { amountMinor, currency } = amountOfItem(item)

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
        amountMinor={amountMinor}
        currency={currency}
        detail={category === null ? undefined : item.display.title}
        leading={leadingTile()}
        onOpen={() => onOpen(item)}
      />
    </li>
  )
}

function categoryOfEntry(item: OutboxItem, categories: readonly PersonalCategory[]): PersonalCategory | null {
  const categoryId = item.display.categoryId ?? null
  if (categoryId === null) {
    return null
  }
  const category = categories.find((candidate) => candidate.id === categoryId)
  if (category === undefined) {
    throw new Error(`The category ${categoryId} of the waiting personal entry ${item.id} is not one of the personal categories`)
  }
  return category
}
