import type { ReactNode } from 'react'
import { NavLink } from 'react-router'

interface KvitTabBarItem {
  to: string
  label: string
  icon: ReactNode
}

interface KvitTabBarProps {
  items: readonly KvitTabBarItem[]
}

export function KvitTabBar({ items }: KvitTabBarProps) {
  return (
    <nav className="flex rounded-full bg-card p-1 shadow-[0_2px_10px_var(--track-shadow)]">
      {items.map((item) => (
        <NavLink
          key={item.to}
          to={item.to}
          className="pressable flex min-h-13 flex-1 flex-col items-center justify-center rounded-full text-xs font-semibold text-muted-foreground hover:text-foreground aria-[current=page]:bg-tab-active aria-[current=page]:text-tab-active-foreground aria-[current=page]:shadow-[0_1px_3px_var(--track-shadow)]"
        >
          {item.icon}
          {item.label}
        </NavLink>
      ))}
    </nav>
  )
}
