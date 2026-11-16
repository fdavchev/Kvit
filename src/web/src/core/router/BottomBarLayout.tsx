import { Outlet } from 'react-router'
import { KvitTabBar } from '@/shared/components/KvitTabBar'
import { useIsDesktop } from '@/shared/hooks/useIsDesktop'
import { useMainNavItems } from './useMainNavItems'

export function BottomBarLayout() {
  const items = useMainNavItems()
  const isDesktop = useIsDesktop()

  return (
    <>
      <Outlet />
      {!isDesktop && (
        <div
          data-bottom-bar
          className="pointer-events-none sticky bottom-0 z-10 mx-auto h-(--bottom-bar-height) w-full max-w-md px-4 pt-2"
        >
          <div className="pointer-events-auto">
            <KvitTabBar items={items} />
          </div>
        </div>
      )}
    </>
  )
}
