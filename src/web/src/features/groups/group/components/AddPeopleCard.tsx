import { useTranslation } from 'react-i18next'
import { KvitButton } from '@/shared/components/KvitButton'

interface AddPeopleCardProps {
  onAddName: () => void
  onShareLink: () => void
}

const rowLook = 'flex items-center gap-3'
const rowTextLook = 'min-w-0 flex-1 text-[0.9375rem] text-pretty'
const rowButtonLook = 'min-h-11 w-auto max-w-[48%] flex-none rounded-full px-4 text-[0.9375rem] font-bold'

export function AddPeopleCard({ onAddName, onShareLink }: AddPeopleCardProps) {
  const { t } = useTranslation()
  return (
    <section className="flex flex-col gap-3 rounded-[20px] bg-card p-4 text-card-foreground">
      <h2 className="text-[1.0625rem] font-bold">{t('group.addPeople')}</h2>
      <div className={rowLook}>
        <p className={rowTextLook}>{t('group.addPeopleNoKvit')}</p>
        <KvitButton
          variant="secondary"
          className={`${rowButtonLook} border-field-border bg-transparent`}
          onClick={onAddName}
        >
          {t('group.addName')}
        </KvitButton>
      </div>
      <div className={rowLook}>
        <p className={rowTextLook}>{t('group.addPeopleHasKvit')}</p>
        <KvitButton className={rowButtonLook} onClick={onShareLink}>
          {t('group.shareLink')}
        </KvitButton>
      </div>
    </section>
  )
}
