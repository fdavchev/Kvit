import type { TFunction } from 'i18next'
import { routes } from '@/core/router/routes'

export interface HomePlusMenuTarget {
  groupId: string
  groupName: string
}

interface HomePlusItemLook {
  icon: string
  label: string
  isMain: boolean
}

export interface HomePlusLink extends HomePlusItemLook {
  kind: 'link'
  to: string
}

export interface HomePlusSpendingOpener extends HomePlusItemLook {
  kind: 'personalSpending'
}

export type HomePlusItem = HomePlusLink | HomePlusSpendingOpener

export function homePlusItems(target: HomePlusMenuTarget | null, isGuest: boolean, t: TFunction): HomePlusItem[] {
  const anotherGroupItem: HomePlusItem = { kind: 'link', to: routes.groups, icon: '⋯', label: t('home.plusAnotherGroup'), isMain: false }
  const otherItems: HomePlusItem[] = isGuest
    ? [anotherGroupItem]
    : [
        { kind: 'link', to: routes.newBill, icon: '🧾', label: t('home.plusNewOneBill'), isMain: false },
        anotherGroupItem,
        { kind: 'personalSpending', icon: '👛', label: t('budget.entries.homeItem'), isMain: false },
      ]
  if (target === null) {
    return otherItems
  }
  return [
    {
      kind: 'link',
      to: routes.groupExpenseNew(target.groupId),
      icon: '＋',
      label: t('home.plusAddTo', { group: target.groupName }),
      isMain: true,
    },
    ...otherItems,
  ]
}

export function homePlusItemKey(item: HomePlusItem): string {
  return item.kind === 'link' ? item.to : item.kind
}
