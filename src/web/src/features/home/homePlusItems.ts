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

export function homePlusItems(target: HomePlusMenuTarget | null, t: TFunction): HomePlusItem[] {
  const otherItems: HomePlusItem[] = [
    { to: routes.newBill, icon: '🧾', label: t('home.plusNewOneBill'), isMain: false },
    { to: routes.groups, icon: '⋯', label: t('home.plusAnotherGroup'), isMain: false },
  ]
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
