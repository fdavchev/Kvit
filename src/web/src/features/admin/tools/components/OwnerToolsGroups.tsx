import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { errorMessageKey } from '@/core/api/errors'
import type { OwnerToolsGroup } from '@/core/services/ownerTools/ownerToolsService'
import { KvitError } from '@/shared/components/KvitError'
import { KvitLoading } from '@/shared/components/KvitLoading'
import { useCloseGroup } from '../hooks/useCloseGroup'
import { useOwnerToolsGroup } from '../hooks/useOwnerToolsGroup'
import { useReopenGroup } from '../hooks/useReopenGroup'
import { OwnerToolsGroupCard, type GroupActionKind } from './OwnerToolsGroupCard'
import { OwnerToolsReasonSheet } from './OwnerToolsReasonSheet'
import { OwnerToolsSearchForm } from './OwnerToolsSearchForm'

interface OpenGroupAction {
  kind: GroupActionKind
  group: OwnerToolsGroup
}

export function OwnerToolsGroups() {
  const { t } = useTranslation()
  const [groupId, setGroupId] = useState<string | null>(null)
  const groupQuery = useOwnerToolsGroup(groupId)
  const closeGroup = useCloseGroup()
  const reopenGroup = useReopenGroup()
  const actions = { closeGroup, reopenGroup }
  const [openAction, setOpenAction] = useState<OpenGroupAction | null>(null)

  function find(text: string): void {
    const groupNumber = text.trim()
    if (groupNumber === '') {
      return
    }
    if (groupNumber === groupId) {
      void groupQuery.refetch()
      return
    }
    setGroupId(groupNumber)
  }

  function renderResult() {
    if (groupId === null) {
      return null
    }
    if (groupQuery.isPending || (groupQuery.isError && groupQuery.isFetching)) {
      return <KvitLoading />
    }
    if (groupQuery.isError) {
      return <KvitError message={t(errorMessageKey(groupQuery.error))} />
    }
    const group = groupQuery.data
    return <OwnerToolsGroupCard group={group} onAction={(kind) => setOpenAction({ kind, group })} />
  }

  return (
    <>
      <OwnerToolsSearchForm
        id="owner-tools-group-search"
        label={t('ownerTools.groups.searchLabel')}
        buttonLabel={t('ownerTools.groups.search')}
        onSearch={find}
      />
      {renderResult()}
      {openAction !== null && (
        <OwnerToolsReasonSheet
          title={t(`ownerTools.sheet.${openAction.kind}.title`, { name: openAction.group.name })}
          body={t(`ownerTools.sheet.${openAction.kind}.body`)}
          doneMessage={t(`ownerTools.done.${openAction.kind}`)}
          targetId={openAction.group.id}
          action={actions[openAction.kind]}
          onClose={() => setOpenAction(null)}
        />
      )}
    </>
  )
}
