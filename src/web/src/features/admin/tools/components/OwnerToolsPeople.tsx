import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { errorMessageKey } from '@/core/api/errors'
import { useSignedInMe } from '@/core/auth/useSignedInMe'
import type { OwnerToolsPerson } from '@/core/services/ownerTools/ownerToolsService'
import { KvitEmpty } from '@/shared/components/KvitEmpty'
import { KvitError } from '@/shared/components/KvitError'
import { KvitLoading } from '@/shared/components/KvitLoading'
import { useEraseName } from '../hooks/useEraseName'
import { useOwnerToolsPeople } from '../hooks/useOwnerToolsPeople'
import { useSuspendPerson } from '../hooks/useSuspendPerson'
import { useUnsuspendPerson } from '../hooks/useUnsuspendPerson'
import { OwnerToolsPersonCard, type PersonActionKind } from './OwnerToolsPersonCard'
import { OwnerToolsReasonSheet } from './OwnerToolsReasonSheet'
import { OwnerToolsSearchForm } from './OwnerToolsSearchForm'

interface OpenPersonAction {
  kind: PersonActionKind
  person: OwnerToolsPerson
}

const shortestSearch = 2

export function OwnerToolsPeople() {
  const { t } = useTranslation()
  const me = useSignedInMe()
  const [query, setQuery] = useState<string | null>(null)
  const peopleQuery = useOwnerToolsPeople(query)
  const suspendPerson = useSuspendPerson()
  const unsuspendPerson = useUnsuspendPerson()
  const eraseName = useEraseName()
  const actions = { suspend: suspendPerson, unsuspend: unsuspendPerson, eraseName }
  const [openAction, setOpenAction] = useState<OpenPersonAction | null>(null)

  function search(text: string): void {
    const searchText = text.trim()
    if (searchText.length < shortestSearch) {
      return
    }
    if (searchText === query) {
      void peopleQuery.refetch()
      return
    }
    setQuery(searchText)
  }

  function renderResults() {
    if (query === null) {
      return null
    }
    if (peopleQuery.isPending || (peopleQuery.isError && peopleQuery.isFetching)) {
      return <KvitLoading />
    }
    if (peopleQuery.isError) {
      return <KvitError message={t(errorMessageKey(peopleQuery.error))} />
    }
    if (peopleQuery.data.length === 0) {
      return <KvitEmpty message={t('ownerTools.people.empty')} />
    }
    return (
      <ul className="flex flex-col gap-3">
        {peopleQuery.data.map((person) => (
          <li key={person.id}>
            <OwnerToolsPersonCard
              person={person}
              isMe={person.id === me.id}
              onAction={(kind) => setOpenAction({ kind, person })}
            />
          </li>
        ))}
      </ul>
    )
  }

  return (
    <>
      <OwnerToolsSearchForm
        id="owner-tools-people-search"
        label={t('ownerTools.people.searchLabel')}
        buttonLabel={t('ownerTools.people.search')}
        onSearch={search}
      />
      {renderResults()}
      {openAction !== null && (
        <OwnerToolsReasonSheet
          title={t(`ownerTools.sheet.${openAction.kind}.title`, { name: openAction.person.displayName })}
          body={t(`ownerTools.sheet.${openAction.kind}.body`)}
          doneMessage={t(`ownerTools.done.${openAction.kind}`, { name: openAction.person.displayName })}
          targetId={openAction.person.id}
          action={actions[openAction.kind]}
          onClose={() => setOpenAction(null)}
        />
      )}
    </>
  )
}
