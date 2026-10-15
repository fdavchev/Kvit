import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { errorMessageKey } from '@/core/api/errors'
import type { Group } from '@/core/services/groups/groupsService'
import { KvitForm } from '@/shared/components/KvitForm'
import type { Currency } from '@/shared/utils/formatMoney'
import { UnlockGroupButton } from '../../closing/components/UnlockGroupButton'
import { GroupFields } from '../../shared/components/GroupFields'
import { isValidGroupName } from '../../shared/groupName'
import { useUpdateGroup } from '../hooks/useUpdateGroup'
import { DeleteGroupButton } from './DeleteGroupButton'

interface OwnerGroupSettingsProps {
  group: Group
}

export function OwnerGroupSettings({ group }: OwnerGroupSettingsProps) {
  if (group.status === 'Finished') {
    return (
      <div className="flex flex-col gap-3">
        <UnlockGroupButton groupId={group.id} />
        <DeleteGroupButton group={group} />
      </div>
    )
  }
  return <GroupSettingsForm group={group} />
}

interface GroupSettingsFormProps {
  group: Group
}

function GroupSettingsForm({ group }: GroupSettingsFormProps) {
  const { t } = useTranslation()
  const updateGroup = useUpdateGroup(group.id)
  const [name, setName] = useState(group.name)
  const [emoji, setEmoji] = useState(group.emoji)
  const [currency, setCurrency] = useState<Currency>(group.defaultCurrency)
  const [isNameInvalid, setIsNameInvalid] = useState<boolean>(false)

  function save(): void {
    if (!isValidGroupName(name)) {
      setIsNameInvalid(true)
      updateGroup.reset()
      return
    }
    setIsNameInvalid(false)
    updateGroup.mutate(
      { name, emoji, currency },
      {
        onSuccess: () => {
          toast.success(t('groupSettings.saved'))
        },
      },
    )
  }

  return (
    <KvitForm
      submitLabel={t('groupSettings.save')}
      isPending={updateGroup.isPending}
      errorMessage={updateGroup.isError ? t(errorMessageKey(updateGroup.error)) : null}
      onSubmit={save}
      footer={<DeleteGroupButton group={group} />}
    >
      <GroupFields
        name={name}
        emoji={emoji}
        currency={currency}
        nameInvalid={isNameInvalid ? t('groupFields.nameInvalid') : null}
        onNameChange={setName}
        onEmojiChange={setEmoji}
        onCurrencyChange={setCurrency}
      />
    </KvitForm>
  )
}
