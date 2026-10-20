import { useTranslation } from 'react-i18next'
import type { PersonalEntry } from '@/core/services/personalEntries/personalEntriesService'
import { KvitSheet } from '@/shared/components/KvitSheet'
import { usePersonalEntryForm } from '../hooks/usePersonalEntryForm'
import { PersonalEntryForm } from './PersonalEntryForm'

interface PersonalIncomeSheetProps {
  entry?: PersonalEntry
  onClose: () => void
}

export function PersonalIncomeSheet({ entry, onClose }: PersonalIncomeSheetProps) {
  const { t } = useTranslation()
  const form = usePersonalEntryForm(entry)

  return (
    <KvitSheet title={t('budget.income.title')} onClose={onClose} actionsRef={form.sheet.actionsRef}>
      <PersonalEntryForm form={form} onSubmit={() => form.save({ kind: 'Income' })} />
    </KvitSheet>
  )
}
