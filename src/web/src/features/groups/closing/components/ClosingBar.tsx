import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { errorMessageKey } from '@/core/api/errors'
import { isGroupStateChangedError } from '@/core/api/queryClient'
import { useSignedInMe } from '@/core/auth/useSignedInMe'
import type { Language } from '@/core/i18n/language'
import { useLanguage } from '@/core/i18n/useLanguage'
import type { Group } from '@/core/services/groups/groupsService'
import { KvitButton } from '@/shared/components/KvitButton'
import { KvitInlineError } from '@/shared/components/KvitInlineError'
import { kvitGlassButtonLook, kvitGlassDangerButtonLook } from '@/shared/components/kvitGlassLooks'
import { cn } from '@/shared/utils/cn'
import { formatExpenseDay, todayInTimeZone } from '@/shared/utils/formatDate'
import type { ClosingBarView } from '../closingBarView'
import { useConfirmClosing } from '../hooks/useConfirmClosing'
import { useStartClosing } from '../hooks/useStartClosing'
import { useToastWhenGroupStateChanged } from '../hooks/useToastWhenGroupStateChanged'
import { ClosingBarFrame } from './ClosingBarFrame'
import { ClosingCheckLine } from './ClosingCheckLine'
import { ObjectSheet } from './ObjectSheet'
import { UnlockGroupButton } from './UnlockGroupButton'

interface ClosingBarProps {
  group: Group
  view: ClosingBarView | null
}

const barButtonLook = 'min-h-11 rounded-full px-4 text-base'
const barOutlineButtonLook = 'border-field-border bg-transparent dark:border-muted-foreground/60'
const noteLook = 'text-[0.8125rem] text-pretty text-muted-foreground'

export function ClosingBar({ group, view }: ClosingBarProps) {
  const { t } = useTranslation()
  const { language } = useLanguage()
  const me = useSignedInMe()
  const [isObjecting, setIsObjecting] = useState<boolean>(false)

  function endsOnText(endsAt: string): string {
    return t('closing.endsOn', { day: endDayOf(endsAt, me.timeZone, language) })
  }

  function renderBar() {
    if (view === null) {
      return null
    }
    switch (view.kind) {
      case 'ready':
        return <ReadyBar groupId={group.id} />
      case 'waiting':
        return (
          <ClosingBarFrame title={t('closing.waitingTitle')}>
            <ul className="flex flex-col gap-1">
              {view.confirmedNames.map((name) => (
                <ClosingCheckLine key={`confirmed-${name}`} mark="✓" text={t('closing.personConfirmed', { name })} />
              ))}
              {view.waitingNames.map((name) => (
                <ClosingCheckLine key={`waiting-${name}`} mark="…" text={t('closing.personWaiting', { name })} />
              ))}
            </ul>
            <p className={noteLook}>{endsOnText(view.endsAt)}</p>
            <p className={noteLook}>{t('closing.changeCancels')}</p>
          </ClosingBarFrame>
        )
      case 'prompt':
        return (
          <PromptBar
            groupId={group.id}
            title={t('closing.prompt', { name: view.ownerName, group: group.name })}
            endsOn={endsOnText(view.endsAt)}
            onObject={() => setIsObjecting(true)}
          />
        )
      case 'confirmed':
        return (
          <ClosingBarFrame title={t('closing.youConfirmed')} icon="✓">
            <p className="text-[0.9375rem]">{t('closing.waitingFor', { names: view.waitingNames.join(', ') })}</p>
            <p className={noteLook}>{endsOnText(view.endsAt)}</p>
            <KvitButton type="button" className={cn(barButtonLook, kvitGlassDangerButtonLook)} onClick={() => setIsObjecting(true)}>
              {t('closing.object')}
            </KvitButton>
          </ClosingBarFrame>
        )
      case 'finished':
        return (
          <ClosingBarFrame title={t('groups.finishedReadOnly')} icon="🔒">
            {view.canUnlock && <UnlockGroupButton groupId={group.id} className={cn(barButtonLook, barOutlineButtonLook)} />}
          </ClosingBarFrame>
        )
    }
  }

  return (
    <>
      {renderBar()}
      {isObjecting && <ObjectSheet groupId={group.id} onClose={() => setIsObjecting(false)} />}
    </>
  )
}

interface ReadyBarProps {
  groupId: string
}

function ReadyBar({ groupId }: ReadyBarProps) {
  const { t } = useTranslation()
  const startClosing = useStartClosing(groupId)
  const toastWhenGroupStateChanged = useToastWhenGroupStateChanged()

  return (
    <ClosingBarFrame title={t('closing.readyTitle')} icon="🎉">
      <ul className="flex flex-col gap-1">
        <ClosingCheckLine mark="✓" text={t('closing.checkBalances')} />
        <ClosingCheckLine mark="✓" text={t('closing.checkPayments')} />
      </ul>
      <KvitButton
        type="button"
        className={cn(barButtonLook, kvitGlassButtonLook)}
        disabled={startClosing.isPending}
        onClick={() => startClosing.mutate(undefined, { onError: toastWhenGroupStateChanged })}
      >
        {t('closing.start')}
      </KvitButton>
      {startClosing.isError && !isGroupStateChangedError(startClosing.error) && (
        <KvitInlineError message={t(errorMessageKey(startClosing.error))} />
      )}
    </ClosingBarFrame>
  )
}

interface PromptBarProps {
  groupId: string
  title: string
  endsOn: string
  onObject: () => void
}

function PromptBar({ groupId, title, endsOn, onObject }: PromptBarProps) {
  const { t } = useTranslation()
  const confirmClosing = useConfirmClosing(groupId)
  const toastWhenGroupStateChanged = useToastWhenGroupStateChanged()

  return (
    <ClosingBarFrame title={title}>
      <p className={noteLook}>{endsOn}</p>
      <div className="flex gap-2.5">
        <KvitButton
          type="button"
          className={cn(barButtonLook, kvitGlassButtonLook, 'flex-1')}
          disabled={confirmClosing.isPending}
          onClick={() => confirmClosing.mutate(undefined, { onError: toastWhenGroupStateChanged })}
        >
          {t('settlements.confirm')}
        </KvitButton>
        <KvitButton
          type="button"
          className={cn(barButtonLook, kvitGlassDangerButtonLook, 'flex-1')}
          disabled={confirmClosing.isPending}
          onClick={onObject}
        >
          {t('closing.object')}
        </KvitButton>
      </div>
      {confirmClosing.isError && !isGroupStateChangedError(confirmClosing.error) && (
        <KvitInlineError message={t(errorMessageKey(confirmClosing.error))} />
      )}
    </ClosingBarFrame>
  )
}

function endDayOf(endsAt: string, timeZone: string, language: Language): string {
  return formatExpenseDay(todayInTimeZone(timeZone, new Date(endsAt)), todayInTimeZone(timeZone, new Date()), language)
}
