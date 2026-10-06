import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { routes } from '@/core/router/routes'
import { KvitBackButton } from '@/shared/components/KvitBackButton'
import { KvitEmojiTile } from '@/shared/components/KvitEmojiTile'
import { KvitScreen } from '@/shared/components/KvitScreen'
import { KvitScreenTitle } from '@/shared/components/KvitScreenTitle'
import { defaultGroupEmoji } from '../../shared/groupEmojis'

interface ChoiceRowProps {
  to: string
  emoji: string
  title: string
  hint: string
}

const billEmoji = '\u{1F9FE}'

export function NewGroupChoiceScreen() {
  const { t } = useTranslation()

  return (
    <KvitScreen>
      <div className="flex min-h-12">
        <KvitBackButton to={routes.groups} />
      </div>
      <KvitScreenTitle>{t('newGroup.title')}</KvitScreenTitle>
      <div className="flex flex-col gap-2.5">
        <ChoiceRow
          to={routes.newBill}
          emoji={billEmoji}
          title={t('newGroup.oneBill')}
          hint={t('newGroup.oneBillHint')}
        />
        <ChoiceRow
          to={routes.newGroupGroup}
          emoji={defaultGroupEmoji}
          title={t('newGroup.group')}
          hint={t('newGroup.groupHint')}
        />
      </div>
    </KvitScreen>
  )
}

function ChoiceRow({ to, emoji, title, hint }: ChoiceRowProps) {
  return (
    <Link
      to={to}
      className="pressable flex min-h-16 items-center gap-3 rounded-2xl bg-card px-3.5 py-2.5 text-card-foreground hover:bg-secondary-hover"
    >
      <KvitEmojiTile emoji={emoji} />
      <span className="flex min-w-0 flex-1 flex-col">
        <span className="font-semibold">{title}</span>
        <span className="text-[0.8125rem] text-muted-foreground">{hint}</span>
      </span>
      <span aria-hidden="true" className="text-xl text-muted-foreground">
        ›
      </span>
    </Link>
  )
}
