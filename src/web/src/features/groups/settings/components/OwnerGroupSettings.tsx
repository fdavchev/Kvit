import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router'
import { toast } from 'sonner'
import { errorMessageKey } from '@/core/api/errors'
import { routes } from '@/core/router/routes'
import type { Group } from '@/core/services/groups/groupsService'
import { KvitButton } from '@/shared/components/KvitButton'
import { KvitForm } from '@/shared/components/KvitForm'
import { showUndoToast } from '@/shared/toasts/showUndoToast'
import type { Currency } from '@/shared/utils/formatMoney'
import { useRestoreGroup } from '../../list/hooks/useRestoreGroup'
import { GroupFields } from '../../shared/components/GroupFields'
import { useDeleteGroup } from '../hooks/useDeleteGroup'
import { useUpdateGroup } from '../hooks/useUpdateGroup'

interface OwnerGroupSettingsProps {
  group: Group
}

export function OwnerGroupSettings({ group }: OwnerGroupSettingsProps) {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const updateGroup = useUpdateGroup(group.id)
  const deleteGroup = useDeleteGroup(group.id)
  const restoreGroup = useRestoreGroup()
  const [name, setName] = useState(group.name)
  const [emoji, setEmoji] = useState(group.emoji)
  const [currency, setCurrency] = useState<Currency>(group.defaultCurrency)

  function showFailure(error: Error): void {
    console.error('A group settings action failed', error)
    toast.error(t(errorMessageKey(error)))
  }

  function save(): void {
    updateGroup.mutate(
      { name, emoji, currency },
      {
        onSuccess: () => {
          toast.success(t('groupSettings.saved'))
        },
      },
    )
  }

  function undoDelete(): void {
    void restoreGroup.mutateAsync(group.id).catch(showFailure)
  }

  function deleteNow(): void {
    deleteGroup.mutate(undefined, {
      onSuccess: () => {
        showUndoToast(t('groupSettings.deleted'), {
          danger: true,
          undoLabel: t('common.undo'),
          onUndo: undoDelete,
        })
        navigate(routes.groups, { replace: true })
      },
      onError: showFailure,
    })
  }

  return (
    <KvitForm
      submitLabel={t('groupSettings.save')}
      isPending={updateGroup.isPending}
      errorMessage={updateGroup.isError ? t(errorMessageKey(updateGroup.error)) : null}
      onSubmit={save}
      footer={
        <KvitButton
          type="button"
          variant="danger"
          disabled={deleteGroup.isPending}
          onClick={deleteNow}
        >
          {t('groupSettings.delete')}
        </KvitButton>
      }
    >
      <GroupFields
        name={name}
        emoji={emoji}
        currency={currency}
        onNameChange={setName}
        onEmojiChange={setEmoji}
        onCurrencyChange={setCurrency}
      />
    </KvitForm>
  )
}
