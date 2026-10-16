import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { errorMessageKey } from '@/core/api/errors'
import { useSignedInMe } from '@/core/auth/useSignedInMe'
import { routes } from '@/core/router/routes'
import { KvitAvatar } from '@/shared/components/KvitAvatar'
import { KvitError } from '@/shared/components/KvitError'
import { KvitLoading } from '@/shared/components/KvitLoading'
import { KvitScreen } from '@/shared/components/KvitScreen'
import { useDashboard } from '../hooks/useDashboard'
import { HomeDashboard } from './HomeDashboard'

export function HomeScreen() {
  const { t } = useTranslation()
  const me = useSignedInMe()
  const dashboardQuery = useDashboard()

  function renderDashboard() {
    if (dashboardQuery.isPending) {
      return <KvitLoading />
    }
    if (dashboardQuery.isError) {
      return (
        <KvitError
          message={t(errorMessageKey(dashboardQuery.error))}
          onRetry={() => void dashboardQuery.refetch()}
        />
      )
    }
    return <HomeDashboard dashboard={dashboardQuery.data} />
  }

  return (
    <KvitScreen>
      <header className="flex items-center justify-between gap-4 pt-11 pb-7">
        <h1 className="text-[2.25rem] leading-[1.1] font-extrabold tracking-[-0.025em]">
          {t('home.greeting', { name: me.displayName })}
        </h1>
        <Link
          to={routes.settings}
          aria-label={t('settings.title')}
          className="pressable grid size-12 flex-none overflow-hidden rounded-full hover:bg-avatar-hover"
        >
          <KvitAvatar name={me.displayName} pictureUrl={me.pictureUrl} colorIndex="brand" size="fill" />
        </Link>
      </header>
      {renderDashboard()}
    </KvitScreen>
  )
}
