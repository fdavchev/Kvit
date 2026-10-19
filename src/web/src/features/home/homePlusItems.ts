import type { TFunction } from 'i18next'
import { routes } from '@/core/router/routes'

export interface HomePlusMenuTarget {
  groupId: string
  groupName: string
}

export interface HomePlusItem {
  to: string
  icon: string
  label: string
  isMain: boolean
}

export function homePlusItems(target: HomePlusMenuTarget | null, isGuest: boolean, t: TFunction): HomePlusItem[] {
  const anotherGroupItem: HomePlusItem = { to: routes.groups, icon: '⋯', label: t('home.plusAnotherGroup'), isMain: false }
  const otherItems: HomePlusItem[] = isGuest
    ? [anotherGroupItem]
    : [{ to: routes.newBill, icon: '🧾', label: t('home.plusNewOneBill'), isMain: false }, anotherGroupItem]
  if (target === null) {
    return otherItems
  }
  return [
    {
      to: routes.groupExpenseNew(target.groupId),
      icon: '＋',
      label: t('home.plusAddTo', { group: target.groupName }),
      isMain: true,
    },
    ...otherItems,
  ]
}
