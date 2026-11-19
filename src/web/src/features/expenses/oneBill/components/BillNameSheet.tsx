import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { NameSheet } from '@/features/groups/shared/components/NameSheet'
import { isValidMemberName } from '@/features/groups/shared/memberName'
import { useKvitSheet } from '@/shared/components/useKvitSheet'

interface BillNameSheetProps {
  ownName: string
  addedNames: readonly string[]
  onAdd: (name: string) => void
  onClose: () => void
}

type Refusal = { kind: 'invalid' } | { kind: 'own' } | { kind: 'taken'; name: string }

function isSameName(first: string, second: string): boolean {
  return first.trim().toLowerCase() === second.trim().toLowerCase()
}

export function BillNameSheet({ ownName, addedNames, onAdd, onClose }: BillNameSheetProps) {
  const { t } = useTranslation()
  const sheet = useKvitSheet()
  const [refusal, setRefusal] = useState<Refusal | null>(null)

  function submit(typedName: string): void {
    const name = typedName.trim()
    if (!isValidMemberName(name)) {
      setRefusal({ kind: 'invalid' })
      return
    }
    if (isSameName(ownName, name)) {
      setRefusal({ kind: 'own' })
      return
    }
    if (addedNames.some((added) => isSameName(added, name))) {
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
    switch (refusal.kind) {
      case 'invalid':
        return t('errors.MEMBER_NAME_INVALID')
      case 'own':
        return t('oneBill.ownName')
      case 'taken':
        return t('errors.MEMBER_NAME_TAKEN', { name: refusal.name })
    }
  }

  return (
    <NameSheet
      actionsRef={sheet.actionsRef}
      isPending={false}
      errorMessage={refusalMessage()}
      isNameRefused={refusal !== null}
      onSubmit={submit}
      onDismiss={sheet.close}
      onClose={onClose}
    />
  )
}
