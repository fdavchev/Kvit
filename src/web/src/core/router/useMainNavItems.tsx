import { useTranslation } from 'react-i18next'
import { useIsGuest } from '@/core/auth/useIsGuest'
import { kvitIconAttributes } from '@/shared/components/kvitIconAttributes'
import type { KvitNavItem } from '@/shared/components/kvitNavItem'
import { routes } from './routes'

const iconLook = 'size-[22px]'

export function useMainNavItems(): KvitNavItem[] {
  const { t } = useTranslation()
  const isGuest = useIsGuest()

  const home: KvitNavItem = {
    to: routes.dashboard,
    label: t('nav.home'),
    icon: (
      <svg {...kvitIconAttributes} className={iconLook}>
        <path d="M3 11l9-8 9 8v9a1 1 0 0 1-1 1h-5v-6H9v6H4a1 1 0 0 1-1-1z" />
      </svg>
    ),
  }
  const groups: KvitNavItem = {
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
  }
  const budget: KvitNavItem = {
    to: routes.budget,
    label: t('nav.budget'),
    icon: (
      <svg {...kvitIconAttributes} className={iconLook}>
        <path d="M4 7.5A2.5 2.5 0 0 1 6.5 5H18a1 1 0 0 1 1 1v2" />
        <path d="M4 7.5V17a2 2 0 0 0 2 2h13a1 1 0 0 0 1-1v-9a1 1 0 0 0-1-1H6.5A2.5 2.5 0 0 1 4 7.5z" />
        <circle cx="16" cy="13.5" r="1.2" />
      </svg>
    ),
  }
  const settings: KvitNavItem = {
    to: routes.settings,
    label: t('nav.settings'),
    icon: (
      <svg {...kvitIconAttributes} className={iconLook}>
        <path d="M4 7h10M18 7h2M4 17h2M10 17h10" />
        <circle cx="16" cy="7" r="2" />
        <circle cx="8" cy="17" r="2" />
      </svg>
    ),
  }

  return isGuest ? [home, groups, settings] : [home, groups, budget, settings]
}
