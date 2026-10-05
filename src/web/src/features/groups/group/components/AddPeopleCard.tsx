import { useTranslation } from 'react-i18next'
import { KvitButton } from '@/shared/components/KvitButton'

interface AddPeopleCardProps {
  onAddName: () => void
  onShareLink: () => void
}

export function AddPeopleCard({ onAddName, onShareLink }: AddPeopleCardProps) {
  const { t } = useTranslation()
  return (
    <section className="flex flex-col gap-3 rounded-[20px] bg-card p-[18px] text-card-foreground">
      <h2 className="text-lg font-bold">{t('group.addPeople')}</h2>
      <p className="text-[0.9375rem] text-pretty text-muted-foreground">
        {t('group.addPeopleNoKvit')}
      </p>
      <KvitButton variant="secondary" className="bg-field" onClick={onAddName}>
        {t('group.addName')}
      </KvitButton>
      <p className="pt-1.5 text-[0.9375rem] text-pretty text-muted-foreground">
        {t('group.addPeopleHasKvit')}
      </p>
      <KvitButton onClick={onShareLink}>{t('group.shareLink')}</KvitButton>
    </section>
  )
}
