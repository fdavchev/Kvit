import { useTranslation } from 'react-i18next'
import { errorMessageKey, hasErrorCode } from '@/core/api/errors'
import { useKvitSheet } from '@/shared/components/useKvitSheet'
import { NameSheet } from '../../shared/components/NameSheet'
import { useAddMember } from '../hooks/useAddMember'

interface AddNameSheetProps {
  groupId: string
  onClose: () => void
}

export function AddNameSheet({ groupId, onClose }: AddNameSheetProps) {
  const { t } = useTranslation()
  const addMember = useAddMember(groupId)
  const sheet = useKvitSheet()

  function submit(name: string): void {
    addMember.mutate(name, { onSuccess: sheet.close })
  }

  return (
    <NameSheet
      actionsRef={sheet.actionsRef}
      isPending={addMember.isPending}
      errorMessage={
        addMember.isError
          ? t(errorMessageKey(addMember.error), { name: addMember.variables })
          : null
      }
      isNameRefused={
        hasErrorCode(addMember.error, 'MEMBER_NAME_TAKEN') ||
        hasErrorCode(addMember.error, 'MEMBER_NAME_INVALID')
      }
      onSubmit={submit}
      onDismiss={sheet.close}
      onClose={onClose}
    />
  )
}
