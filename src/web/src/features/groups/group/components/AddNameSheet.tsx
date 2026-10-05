import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { errorMessageKey } from '@/core/api/errors'
import { KvitButton } from '@/shared/components/KvitButton'
import { KvitForm } from '@/shared/components/KvitForm'
import { KvitSheet } from '@/shared/components/KvitSheet'
import { KvitTextField } from '@/shared/components/KvitTextField'
import { useAddMember } from '../hooks/useAddMember'

interface AddNameSheetProps {
  groupId: string
  onClose: () => void
}

export function AddNameSheet({ groupId, onClose }: AddNameSheetProps) {
  const { t } = useTranslation()
  const addMember = useAddMember(groupId)
  const [name, setName] = useState('')

  function submit(): void {
    addMember.mutate(name, { onSuccess: onClose })
  }

  return (
    <KvitSheet title={t('addName.title')} onClose={onClose}>
      <KvitForm
        submitLabel={t('addName.submit')}
        isPending={addMember.isPending}
        errorMessage={
          addMember.isError
            ? t(errorMessageKey(addMember.error), { name: addMember.variables })
            : null
        }
        onSubmit={submit}
        footer={
          <KvitButton type="button" variant="link" onClick={onClose}>
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
