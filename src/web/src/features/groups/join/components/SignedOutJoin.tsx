import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { errorMessageKey, hasErrorCode } from '@/core/api/errors'
import type { GuestJoinChoice, InvitePreview } from '@/core/services/invites/invitesService'
import { KvitAvatar } from '@/shared/components/KvitAvatar'
import { KvitButton } from '@/shared/components/KvitButton'
import { KvitForm } from '@/shared/components/KvitForm'
import { KvitInlineError } from '@/shared/components/KvitInlineError'
import { KvitSpinner } from '@/shared/components/KvitSpinner'
import { KvitTextField } from '@/shared/components/KvitTextField'
import { kvitIconAttributes } from '@/shared/components/kvitIconAttributes'
import { useListAnimation } from '@/shared/hooks/useListAnimation'
import { useJoinAsGuest } from '../hooks/useJoinAsGuest'
import { JoinLogInSheet } from './JoinLogInSheet'
import { JoinMessage } from './JoinMessage'

interface SignedOutJoinProps {
  token: string
  preview: InvitePreview
  onGoogleCredential: (idToken: string) => void
}

const rowLook =
  'pressable flex min-h-14 w-full items-center gap-3 px-4 py-2 text-left font-semibold hover:bg-secondary-hover disabled:opacity-50'

const nameRefusedCodes = ['AUTH_DISPLAY_NAME_INVALID', 'MEMBER_NAME_TAKEN']

export function SignedOutJoin({ token, preview, onGoogleCredential }: SignedOutJoinProps) {
  const { t } = useTranslation()
  const joinAsGuest = useJoinAsGuest(token, preview.name)
  const listRef = useListAnimation<HTMLUListElement>()
  const [isNewNameOpen, setIsNewNameOpen] = useState<boolean>(false)
  const [newName, setNewName] = useState<string>('')
  const [isNameChangedSinceJoin, setIsNameChangedSinceJoin] = useState<boolean>(false)
  const [isLogInOpen, setIsLogInOpen] = useState<boolean>(false)
  const { error, variables, isPending } = joinAsGuest

  if (hasErrorCode(error, 'GROUP_CLOSING') || hasErrorCode(error, 'GROUP_FINISHED')) {
    return <JoinMessage message={t(errorMessageKey(error))} />
  }

  const typedName: string | null = variables !== undefined && 'name' in variables ? variables.name.trim() : null
  const errorMessage: string | null =
    joinAsGuest.isError && !hasErrorCode(error, 'GUEST_JOIN_SIGNED_IN')
      ? t(errorMessageKey(error), { name: typedName })
      : null
  const isNameRefused: boolean =
    typedName !== null && nameRefusedCodes.some((code) => hasErrorCode(error, code)) && !isNameChangedSinceJoin

  function join(choice: GuestJoinChoice): void {
    joinAsGuest.mutate(choice)
  }

  function changeNewName(nextName: string): void {
    setNewName(nextName)
    setIsNameChangedSinceJoin(true)
  }

  function joinWithNewName(): void {
    setIsNameChangedSinceJoin(false)
    join({ name: newName })
  }

  return (
    <div className="flex flex-col gap-3 pt-6">
      {typedName === null && errorMessage !== null && <KvitInlineError message={errorMessage} />}
      <h2 className="text-lg font-bold text-balance">
        {preview.unclaimedNames.length > 0 ? t('join.guest.whoAreYou') : t('join.guest.whatsYourName')}
      </h2>
      <ul
        ref={listRef}
        className="flex flex-col divide-y divide-border overflow-hidden rounded-[20px] bg-card text-card-foreground"
      >
        {preview.unclaimedNames.map((unclaimed, index) => {
          const isJoiningAsThis: boolean =
            isPending && variables !== undefined && 'claimMemberId' in variables && variables.claimMemberId === unclaimed.id
          return (
            <li key={unclaimed.id}>
              <button
                type="button"
                className={rowLook}
                disabled={isPending}
                onClick={() => join({ claimMemberId: unclaimed.id })}
              >
                <KvitAvatar name={unclaimed.name} pictureUrl={null} colorIndex={index} />
                <span className="min-w-0 flex-1 wrap-anywhere">{unclaimed.name}</span>
                <span className="grid size-8 flex-none place-items-center text-muted-foreground">
                  {isJoiningAsThis ? (
                    <KvitSpinner />
                  ) : (
                    <svg {...kvitIconAttributes} className="size-5">
                      <path d="m9 6 6 6-6 6" />
                    </svg>
                  )}
                </span>
              </button>
            </li>
          )
        })}
        <li>
          <button
            type="button"
            aria-expanded={isNewNameOpen}
            className={rowLook}
            disabled={isPending}
            onClick={() => setIsNewNameOpen((isOpen) => !isOpen)}
          >
            <span
              aria-hidden="true"
              className="grid size-9 flex-none place-items-center rounded-full bg-pill text-foreground"
            >
              <svg {...kvitIconAttributes} strokeWidth={2.5} className="size-5">
                <path d="M12 5v14M5 12h14" />
              </svg>
            </span>
            <span className="min-w-0 flex-1 font-bold wrap-anywhere">{t('join.guest.imNew')}</span>
          </button>
        </li>
      </ul>
      {isNewNameOpen && (
        <KvitForm
          submitLabel={t('join.join')}
          isPending={isPending}
          errorMessage={typedName === null ? null : errorMessage}
          onSubmit={joinWithNewName}
        >
          <KvitTextField
            id="guest-name"
            label={t('auth.signUp.name')}
            value={newName}
            onChange={changeNewName}
            invalid={isNameRefused}
            autoComplete="name"
            autoCapitalize="words"
            enterKeyHint="go"
            autoFocus
          />
        </KvitForm>
      )}
      <p className="flex flex-wrap items-center justify-center gap-x-1 pt-2 text-center text-[0.9375rem] text-muted-foreground">
        <span>{t('join.guest.haveAccount')}</span>
        <KvitButton
          variant="smallLink"
          className="w-auto min-w-11 px-1.5"
          disabled={isPending}
          onClick={() => setIsLogInOpen(true)}
        >
          {t('auth.logIn.title')}
        </KvitButton>
      </p>
      {isLogInOpen && (
        <JoinLogInSheet
          token={token}
          onGoogleCredential={onGoogleCredential}
          onClose={() => setIsLogInOpen(false)}
        />
      )}
    </div>
  )
}
