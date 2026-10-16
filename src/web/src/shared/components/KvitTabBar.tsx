import { Link, useLocation } from 'react-router'
import { KvitTabPill } from './KvitTabPill'
import { currentNavItemIndex, type KvitNavItem } from './kvitNavItem'

interface KvitTabBarProps {
  items: readonly KvitNavItem[]
}

export function KvitTabBar({ items }: KvitTabBarProps) {
  const { pathname } = useLocation()
  const activeIndex: number = currentNavItemIndex(items, pathname)

  return (
    <nav className="relative flex rounded-full bg-card p-1 shadow-[0_2px_10px_var(--track-shadow)]">
      {activeIndex !== -1 && (
        <KvitTabPill
          activeIndex={activeIndex}
          tabCount={items.length}
          className="bg-tab-active shadow-[0_1px_3px_var(--track-shadow)]"
        />
      )}
      {items.map((item, index) => (
        <Link
          key={item.to}
          to={item.to}
          aria-current={index === activeIndex ? 'page' : undefined}
          className="pressable relative flex min-h-13 flex-1 flex-col items-center justify-center rounded-full text-xs font-semibold text-muted-foreground hover:text-foreground aria-[current=page]:text-tab-active-foreground"
        >
          {item.icon}
          {item.label}
        </Link>
      ))}
    </nav>
  )
}
