import { useState, type RefObject } from 'react'
import { useTranslation } from 'react-i18next'
import { KvitButton } from '@/shared/components/KvitButton'
import { KvitForm } from '@/shared/components/KvitForm'
import { KvitSheet, type KvitSheetActions } from '@/shared/components/KvitSheet'
import { KvitTextField } from '@/shared/components/KvitTextField'

interface NameSheetProps {
  actionsRef: RefObject<KvitSheetActions | null>
  isPending: boolean
  errorMessage: string | null
  onSubmit: (name: string) => void
  onDismiss: () => void
  onClose: () => void
}

export function NameSheet({ actionsRef, isPending, errorMessage, onSubmit, onDismiss, onClose }: NameSheetProps) {
  const { t } = useTranslation()
  const [name, setName] = useState('')

  return (
    <KvitSheet title={t('addName.title')} onClose={onClose} actionsRef={actionsRef}>
      <KvitForm
        submitLabel={t('addName.submit')}
        isPending={isPending}
        errorMessage={errorMessage}
        onSubmit={() => onSubmit(name)}
        footer={
          <KvitButton type="button" variant="link" onClick={onDismiss}>
            {t('common.close')}
          </KvitButton>
        }
      >
        <KvitTextField
          id="add-name"
          label={t('addName.label')}
          value={name}
          onChange={setName}
          hint={t('addName.hint')}
          autoComplete="off"
          enterKeyHint="done"
        />
      </KvitForm>
    </KvitSheet>
  )
}
