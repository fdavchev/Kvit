import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { errorMessageKey } from '@/core/api/errors'
import { KvitButton } from '@/shared/components/KvitButton'
import { KvitForm } from '@/shared/components/KvitForm'
import { KvitSheet } from '@/shared/components/KvitSheet'
import { KvitTextField } from '@/shared/components/KvitTextField'
import { kvitGlassDangerButtonLook } from '@/shared/components/kvitGlassLooks'
import { useKvitSheet } from '@/shared/components/useKvitSheet'
import { useObjectToClosing } from '../hooks/useObjectToClosing'

interface ObjectSheetProps {
  groupId: string
  onClose: () => void
}

const reasonMaxLength = 200

export function ObjectSheet({ groupId, onClose }: ObjectSheetProps) {
  const { t } = useTranslation()
  const sheet = useKvitSheet()
  const objectToClosing = useObjectToClosing(groupId)
  const [reason, setReason] = useState<string>('')

  function send(): void {
    const trimmedReason = reason.trim()
    objectToClosing.mutate(trimmedReason === '' ? undefined : trimmedReason, {
      onSuccess: () => {
        toast.success(t('closing.objected'))
        sheet.close()
      },
    })
  }

  return (
    <KvitSheet title={t('closing.objectTitle')} onClose={onClose} actionsRef={sheet.actionsRef}>
      <KvitForm
        submitLabel={t('closing.send')}
        submitClassName={kvitGlassDangerButtonLook}
        isPending={objectToClosing.isPending}
        errorMessage={objectToClosing.isError ? t(errorMessageKey(objectToClosing.error)) : null}
        onSubmit={send}
        footer={
          <KvitButton type="button" variant="link" onClick={sheet.close}>
            {t('settlements.cancel')}
          </KvitButton>
        }
      >
        <KvitTextField
          id="closing-reason"
          label={t('closing.reasonLabel')}
          value={reason}
          onChange={setReason}
          maxLength={reasonMaxLength}
          autoComplete="off"
          enterKeyHint="send"
        />
      </KvitForm>
    </KvitSheet>
  )
}
