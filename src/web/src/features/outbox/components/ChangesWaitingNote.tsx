import { useTranslation } from 'react-i18next'
import type { OutboxItem } from '@/core/outbox/outboxItem'
import { useOutboxItems } from '@/core/outbox/useOutbox'

type ChangesWaitingScope = { kind: 'group'; groupId: string } | { kind: 'personal' } | { kind: 'home' }

interface ChangesWaitingNoteProps {
  scope: ChangesWaitingScope
}

export function ChangesWaitingNote({ scope }: ChangesWaitingNoteProps) {
  const { t } = useTranslation()
  const count = useOutboxItems((item) => item.state !== 'failed' && isInScope(item, scope)).length

  if (count === 0) {
    return null
  }
  return <p className="mx-1 my-2 text-[0.8125rem] text-muted-foreground">{t('outbox.notCounted', { count })}</p>
}

function isInScope(item: OutboxItem, scope: ChangesWaitingScope): boolean {
  switch (scope.kind) {
    case 'group':
      return (item.kind === 'expense' || item.kind === 'settlement') && item.groupId === scope.groupId
    case 'personal':
      return item.kind === 'personalEntry'
    case 'home':
      return item.kind !== 'personalEntry'
    default:
      throw new Error(`Unknown scope of the changes-waiting note: ${JSON.stringify(scope satisfies never)}`)
  }
}
