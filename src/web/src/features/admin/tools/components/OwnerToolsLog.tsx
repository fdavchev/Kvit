import { useTranslation } from 'react-i18next'
import { errorMessageKey } from '@/core/api/errors'
import { useSignedInMe } from '@/core/auth/useSignedInMe'
import { useLanguage } from '@/core/i18n/useLanguage'
import { KvitEmpty } from '@/shared/components/KvitEmpty'
import { KvitError } from '@/shared/components/KvitError'
import { KvitLoading } from '@/shared/components/KvitLoading'
import { KvitSkeleton } from '@/shared/components/KvitSkeleton'
import { formatHowLongAgo } from '@/shared/utils/formatDate'
import { useOwnerToolsLog } from '../hooks/useOwnerToolsLog'

const skeletonRowCount = 3

export function OwnerToolsLog() {
  const { t } = useTranslation()
  const { language } = useLanguage()
  const me = useSignedInMe()
  const logQuery = useOwnerToolsLog()

  if (logQuery.isPending || (logQuery.isError && logQuery.isFetching)) {
    return (
      <KvitLoading>
        {Array.from({ length: skeletonRowCount }, (_unused, index) => (
          <LogRowSkeleton key={index} />
        ))}
      </KvitLoading>
    )
  }
  if (logQuery.isError) {
    return <KvitError message={t(errorMessageKey(logQuery.error))} onRetry={() => void logQuery.refetch()} />
  }
  if (logQuery.data.length === 0) {
    return <KvitEmpty message={t('ownerTools.log.empty')} />
  }
  const now = new Date()
  return (
    <ul className="flex flex-col pb-6">
      {logQuery.data.map((entry) => (
        <li key={entry.id} className="flex flex-col gap-0.5 border-b border-field-border py-2.5">
          <p className="font-semibold text-pretty wrap-anywhere">
            {t('ownerTools.log.line', {
              admin: entry.adminName,
              action: t(`ownerTools.log.action.${entry.action}`),
              target: entry.targetLabel,
            })}
          </p>
          <p className="text-[0.9375rem] text-pretty wrap-anywhere">{entry.reason}</p>
          <time dateTime={entry.createdAt} className="text-[0.8125rem] text-muted-foreground">
            {formatHowLongAgo(entry.createdAt, now, me.timeZone, language, t)}
          </time>
        </li>
      ))}
    </ul>
  )
}

function LogRowSkeleton() {
  return (
    <div className="flex flex-col gap-2 border-b border-field-border py-3">
      <KvitSkeleton className="h-4 w-4/5 rounded-full" />
      <KvitSkeleton className="h-4 w-3/5 rounded-full" />
      <KvitSkeleton className="h-3 w-1/4 rounded-full" />
    </div>
  )
}
