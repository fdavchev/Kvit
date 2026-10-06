import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { NameSheet } from '@/features/groups/shared/components/NameSheet'
import { isValidMemberName } from '@/features/groups/shared/memberName'
import { useKvitSheet } from '@/shared/components/useKvitSheet'

interface BillNameSheetProps {
  takenNames: readonly string[]
  onAdd: (name: string) => void
  onClose: () => void
}

type Refusal = { kind: 'invalid' } | { kind: 'taken'; name: string }

export function BillNameSheet({ takenNames, onAdd, onClose }: BillNameSheetProps) {
  const { t } = useTranslation()
  const sheet = useKvitSheet()
  const [refusal, setRefusal] = useState<Refusal | null>(null)

  function submit(typedName: string): void {
    const name = typedName.trim()
    if (!isValidMemberName(name)) {
      setRefusal({ kind: 'invalid' })
      return
    }
    if (takenNames.some((taken) => taken.trim().toLowerCase() === name.toLowerCase())) {
      setRefusal({ kind: 'taken', name })
      return
    }
    setRefusal(null)
    onAdd(name)
    sheet.close()
  }

  function refusalMessage(): string | null {
    if (refusal === null) {
      return null
    }
    return refusal.kind === 'invalid'
      ? t('errors.MEMBER_NAME_INVALID')
      : t('errors.MEMBER_NAME_TAKEN', { name: refusal.name })
  }

  return (
    <NameSheet
      actionsRef={sheet.actionsRef}
      isPending={false}
      errorMessage={refusalMessage()}
      onSubmit={submit}
      onDismiss={sheet.close}
      onClose={onClose}
    />
  )
}
