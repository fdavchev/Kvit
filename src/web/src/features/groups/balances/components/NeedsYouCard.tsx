import { useTranslation } from 'react-i18next'
import { errorMessageKey } from '@/core/api/errors'
import { useAnswerSettlement } from '@/features/settlements/pending/hooks/useAnswerSettlement'
import { KvitAvatar } from '@/shared/components/KvitAvatar'
import { KvitButton } from '@/shared/components/KvitButton'
import { KvitInlineError } from '@/shared/components/KvitInlineError'
import { KvitUnbrokenSlashWords } from '@/shared/components/KvitUnbrokenSlashWords'
import { kvitGlassButtonLook, kvitGlassDangerButtonLook } from '@/shared/components/kvitGlassLooks'
import { cn } from '@/shared/utils/cn'
import { balancesActionButtonLook, balancesCardLook, balancesRowLook, balancesRowTextLook } from '../balancesLooks'
import type { NeedsYouView } from '../balancesView'

interface NeedsYouCardProps {
  groupId: string
  item: NeedsYouView
  sentence: string
  howLongAgo: string
  canAnswer: boolean
}

export function NeedsYouCard({ groupId, item, sentence, howLongAgo, canAnswer }: NeedsYouCardProps) {
  const { t } = useTranslation()
  const answerSettlement = useAnswerSettlement(groupId, item.settlementId)

  return (
    <li className={cn(balancesCardLook, 'flex flex-col gap-2.5')}>
      <div className={cn(balancesRowLook, 'gap-3')}>
        <KvitAvatar name={item.payer.name} pictureUrl={item.payer.pictureUrl} colorIndex={item.payer.colorIndex} />
        <div className={balancesRowTextLook}>
          <p className="font-bold break-words">
            <KvitUnbrokenSlashWords text={sentence} />
          </p>
          <p className="text-[0.8125rem] text-muted-foreground">{howLongAgo}</p>
        </div>
      </div>
      {canAnswer && (
        <div className="flex gap-2.5">
          <KvitButton
            className={cn(balancesActionButtonLook, kvitGlassDangerButtonLook, 'flex-1')}
            disabled={answerSettlement.isPending}
            onClick={() => answerSettlement.mutate('reject')}
          >
            {t('settlements.reject')}
          </KvitButton>
          <KvitButton
            className={cn(balancesActionButtonLook, kvitGlassButtonLook, 'flex-1')}
            disabled={answerSettlement.isPending}
            onClick={() => answerSettlement.mutate('confirm')}
          >
            {t('settlements.confirm')}
          </KvitButton>
        </div>
      )}
      {answerSettlement.isError && <KvitInlineError message={t(errorMessageKey(answerSettlement.error))} />}
    </li>
  )
}
