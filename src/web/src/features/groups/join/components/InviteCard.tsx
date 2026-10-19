import { Fragment } from 'react'
import { useTranslation } from 'react-i18next'
import type { InvitePreview } from '@/core/services/invites/invitesService'
import { KvitEmojiTile } from '@/shared/components/KvitEmojiTile'

interface InviteCardProps {
  preview: InvitePreview
}

export function InviteCard({ preview }: InviteCardProps) {
  const { t } = useTranslation()

  return (
    <div className="flex flex-col items-center gap-2.5 pt-6 text-center">
      <KvitEmojiTile emoji={preview.emoji} size="large" />
      <h1 className="text-[1.75rem] leading-[1.15] font-extrabold tracking-[-0.02em] text-balance wrap-anywhere">
        {t('join.invitedTo', { name: preview.name })}
      </h1>
      <p className="text-[0.9375rem] text-pretty text-muted-foreground">
        <span className="font-bold">{t('join.inGroup')}:</span>{' '}
        {preview.memberNames.map((memberName, index) => (
          <Fragment key={index}>
            {index > 0 && ' '}
            <span className="inline-block max-w-full wrap-anywhere">
              <span>{memberName}</span>
              {index < preview.memberNames.length - 1 && ','}
            </span>
          </Fragment>
        ))}
      </p>
    </div>
  )
}
