import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router'
import { toast } from 'sonner'
import { errorMessageKey } from '@/core/api/errors'
import { routes } from '@/core/router/routes'
import type { InvitePreview } from '@/core/services/invites/invitesService'
import { KvitButton } from '@/shared/components/KvitButton'
import { KvitInlineError } from '@/shared/components/KvitInlineError'
import { kvitChipLook } from '@/shared/components/kvitChipLook'
import { useJoinGroup } from '../hooks/useJoinGroup'

interface SignedInJoinProps {
  token: string
  preview: InvitePreview
}

export function SignedInJoin({ token, preview }: SignedInJoinProps) {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const joinGroup = useJoinGroup()
  const [isAskingName, setIsAskingName] = useState<boolean>(false)

  function joinNow(claimMemberId?: string): void {
    joinGroup.mutate(
      { token, claimMemberId },
      {
        onSuccess: (groupId) => {
          toast.success(t('join.joined', { name: preview.name }))
          navigate(routes.group(groupId), { replace: true })
        },
      },
    )
  }

  function pressJoin(): void {
    if (preview.unclaimedNames.length === 0) {
      joinNow()
      return
    }
    setIsAskingName(true)
  }

  return (
    <div className="mt-auto flex flex-col gap-3 pt-8">
      {joinGroup.isError && <KvitInlineError message={t(errorMessageKey(joinGroup.error))} />}
      {isAskingName ? (
        <section className="flex flex-col gap-3 rounded-[20px] bg-card p-[18px] text-card-foreground">
          <h2 className="text-lg font-bold">{t('join.areYou')}</h2>
          <div className="flex flex-wrap gap-2">
            {preview.unclaimedNames.map((unclaimed) => (
              <button
                key={unclaimed.id}
                type="button"
                className={kvitChipLook}
                disabled={joinGroup.isPending}
                onClick={() => joinNow(unclaimed.id)}
              >
                {unclaimed.name}
              </button>
            ))}
            <button
              type="button"
              className={kvitChipLook}
              disabled={joinGroup.isPending}
              onClick={() => joinNow()}
            >
              {t('join.imNew')}
            </button>
          </div>
        </section>
      ) : (
        <KvitButton disabled={joinGroup.isPending} onClick={pressJoin}>
          {t('join.join')}
        </KvitButton>
      )}
    </div>
  )
}
