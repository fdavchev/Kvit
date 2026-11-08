import { useId, useState } from 'react'
import { useTranslation } from 'react-i18next'
import type { DeleteAccountNameChoice } from '@/core/services/me/meService'
import { KvitButton } from '@/shared/components/KvitButton'
import { KvitSheet } from '@/shared/components/KvitSheet'
import { useKvitSheet } from '@/shared/components/useKvitSheet'

interface NameChoiceOption {
  value: DeleteAccountNameChoice
  textKey: string
  hintKey: string
}

const nameChoiceOptions: readonly NameChoiceOption[] = [
  {
    value: 'KeepName',
    textKey: 'settings.deleteAccountSheet.keepName',
    hintKey: 'settings.deleteAccountSheet.keepNameHint',
  },
  {
    value: 'FormerMember',
    textKey: 'settings.deleteAccountSheet.formerMember',
    hintKey: 'settings.deleteAccountSheet.formerMemberHint',
  },
]

interface DeleteAccountSheetProps {
  isGuest: boolean
  isDeleting: boolean
  onConfirm: (nameChoice: DeleteAccountNameChoice) => void
  onClose: () => void
}

export function DeleteAccountSheet({ isGuest, isDeleting, onConfirm, onClose }: DeleteAccountSheetProps) {
  const { t } = useTranslation()
  const sheet = useKvitSheet()
  const labelId = useId()
  const optionName = useId()
  const [nameChoice, setNameChoice] = useState<DeleteAccountNameChoice>('KeepName')

  return (
    <KvitSheet title={t('settings.deleteAccountSheet.title')} onClose={onClose} actionsRef={sheet.actionsRef}>
      <div role="radiogroup" aria-labelledby={labelId} className="flex flex-col gap-1">
        <span id={labelId} className="text-[0.9375rem] font-semibold">
          {t('settings.deleteAccountSheet.nameLabel')}
        </span>
        {nameChoiceOptions.map((option) => (
          <label
            key={option.value}
            className="pressable -mx-2 flex min-h-11 cursor-pointer items-start gap-3 rounded-2xl px-2 py-1.5 text-[0.9375rem] has-checked:bg-background"
          >
            <input
              type="radio"
              name={optionName}
              checked={option.value === nameChoice}
              onChange={() => setNameChoice(option.value)}
              aria-labelledby={`${optionName}-${option.value}-text`}
              aria-describedby={`${optionName}-${option.value}-hint`}
              className="mt-0.5 size-5 shrink-0 cursor-pointer accent-primary"
            />
            <span className="flex flex-col gap-0.5">
              <span id={`${optionName}-${option.value}-text`} className="font-semibold text-pretty">
                {t(option.textKey)}
              </span>
              <span id={`${optionName}-${option.value}-hint`} className="text-[0.875rem] text-pretty text-muted-foreground">
                {t(option.hintKey)}
              </span>
            </span>
          </label>
        ))}
      </div>
      <p className="text-[0.9375rem] text-pretty text-muted-foreground">
        {t(isGuest ? 'settings.deleteAccountSheet.whatHappensGuest' : 'settings.deleteAccountSheet.whatHappens')}
      </p>
      <div className="flex flex-col gap-2">
        <KvitButton
          variant="danger"
          className="bg-field"
          disabled={isDeleting}
          aria-busy={isDeleting}
          onClick={() => onConfirm(nameChoice)}
        >
          {t('settings.deleteAccountSheet.confirm')}
        </KvitButton>
        <KvitButton variant="link" onClick={sheet.close}>
          {t('settlements.cancel')}
        </KvitButton>
      </div>
    </KvitSheet>
  )
}
