import type { ReactNode } from 'react'
import { matchPath } from 'react-router'

export interface KvitNavItem {
  to: string
  label: string
  icon: ReactNode
}

const rootPath = '/'

export function isCurrentNavPath(to: string, pathname: string): boolean {
  return matchPath({ path: to, end: to === rootPath }, pathname) !== null
}

export function currentNavItemIndex(items: readonly KvitNavItem[], pathname: string): number {
  return items.findIndex((item) => isCurrentNavPath(item.to, pathname))
}
