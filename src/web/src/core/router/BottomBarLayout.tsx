import { useTranslation } from 'react-i18next'
import { Outlet } from 'react-router'
import { KvitTabBar } from '@/shared/components/KvitTabBar'
import { kvitIconAttributes } from '@/shared/components/kvitIconAttributes'
import { routes } from './routes'

const iconLook = 'size-[22px]'

export function BottomBarLayout() {
  const { t } = useTranslation()

  const items = [
    {
      to: routes.dashboard,
      label: t('nav.home'),
      icon: (
        <svg {...kvitIconAttributes} className={iconLook}>
          <path d="M3 11l9-8 9 8v9a1 1 0 0 1-1 1h-5v-6H9v6H4a1 1 0 0 1-1-1z" />
        </svg>
      ),
    },
    {
      to: routes.groups,
      label: t('nav.groups'),
      icon: (
        <svg {...kvitIconAttributes} className={iconLook}>
          <circle cx="9" cy="8" r="3.2" />
          <path d="M3 20c0-3.3 2.7-6 6-6s6 2.7 6 6" />
          <circle cx="17" cy="9" r="2.5" />
          <path d="M16 14.2c2.7 0 5 2.2 5 5.1" />
        </svg>
      ),
    },
    {
      to: routes.settings,
      label: t('nav.settings'),
      icon: (
        <svg {...kvitIconAttributes} className={iconLook}>
          <path d="M4 7h10M18 7h2M4 17h2M10 17h10" />
          <circle cx="16" cy="7" r="2" />
          <circle cx="8" cy="17" r="2" />
        </svg>
      ),
    },
  ]

  return (
    <>
      <Outlet />
      <div
        data-bottom-bar
        className="pointer-events-none sticky bottom-0 z-10 mx-auto h-(--bottom-bar-height) w-full max-w-md px-4 pt-2"
      >
        <div className="pointer-events-auto">
          <KvitTabBar items={items} />
        </div>
      </div>
    </>
  )
}
