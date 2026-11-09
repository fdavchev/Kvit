import type { UseMutationResult } from '@tanstack/react-query'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { errorMessageKey } from '@/core/api/errors'
import { KvitButton } from '@/shared/components/KvitButton'
import { KvitForm } from '@/shared/components/KvitForm'
import { KvitSheet } from '@/shared/components/KvitSheet'
import { KvitTextField } from '@/shared/components/KvitTextField'
import { useKvitSheet } from '@/shared/components/useKvitSheet'
import type { OwnerToolsAction } from '../types'

interface OwnerToolsReasonSheetProps {
  title: string
  body: string
  doneMessage: string
  targetId: string
  action: UseMutationResult<void, Error, OwnerToolsAction>
  onClose: () => void
}

const reasonMaxLength = 500

export function OwnerToolsReasonSheet({ title, body, doneMessage, targetId, action, onClose }: OwnerToolsReasonSheetProps) {
  const { t } = useTranslation()
  const sheet = useKvitSheet()
  const [reason, setReason] = useState<string>('')
  const trimmedReason = reason.trim()
  const isReasonValid = trimmedReason.length > 0 && trimmedReason.length <= reasonMaxLength

  function confirm(): void {
    action.mutate(
      { id: targetId, reason: trimmedReason },
      {
        onSuccess: () => {
          toast.success(doneMessage)
          sheet.close()
        },
        onError: (error) => {
          toast.error(t(errorMessageKey(error)))
        },
      },
    )
  }

  return (
    <KvitSheet title={title} onClose={onClose} actionsRef={sheet.actionsRef}>
      <p className="text-[0.9375rem] text-pretty text-muted-foreground">{body}</p>
      <KvitForm
        submitLabel={t('ownerTools.confirm')}
        isPending={action.isPending}
        canSubmit={isReasonValid}
        errorMessage={null}
        onSubmit={confirm}
        footer={
          <KvitButton type="button" variant="link" onClick={sheet.close}>
            {t('settlements.cancel')}
          </KvitButton>
        }
      >
        <KvitTextField
          id="owner-tools-reason"
          label={t('ownerTools.reasonLabel')}
          value={reason}
          onChange={setReason}
          autoComplete="off"
          enterKeyHint="send"
        />
      </KvitForm>
    </KvitSheet>
  )
}
