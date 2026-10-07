import { useTranslation } from 'react-i18next'
import { KvitButton } from '@/shared/components/KvitButton'

interface AddPeopleButtonsProps {
  onAddName: () => void
  onShareLink: () => void
}

const buttonLook = 'min-h-12 flex-1 rounded-full px-2 text-[0.9375rem] font-bold'

export function AddPeopleButtons({ onAddName, onShareLink }: AddPeopleButtonsProps) {
  const { t } = useTranslation()
  return (
    <div className="flex flex-col gap-2.5 pb-2">
      <div className="flex gap-2.5">
        <KvitButton
          variant="secondary"
          className={`${buttonLook} border-field-border bg-transparent`}
          onClick={onAddName}
        >
          {t('group.addName')}
        </KvitButton>
        <KvitButton className={buttonLook} onClick={onShareLink}>
          {t('group.shareLink')}
        </KvitButton>
      </div>
      <div className="text-center text-[0.8125rem] text-pretty text-muted-foreground">
        <p>{t('group.addPeopleNoKvit')}</p>
        <p>{t('group.addPeopleHasKvit')}</p>
      </div>
    </div>
  )
}
