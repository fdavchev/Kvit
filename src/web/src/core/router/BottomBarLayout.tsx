import { Outlet } from 'react-router'
import { useIsRefreshingSavedData } from '@/core/api/useIsRefreshingSavedData'
import { KvitTabBar } from '@/shared/components/KvitTabBar'
import { KvitUpdatingNote } from '@/shared/components/KvitUpdatingNote'
import { useIsDesktop } from '@/shared/hooks/useIsDesktop'
import { useMainNavItems } from './useMainNavItems'

export function BottomBarLayout() {
  const items = useMainNavItems()
  const isRefreshingSavedData = useIsRefreshingSavedData()
  const isDesktop = useIsDesktop()

  return (
    <>
      <div
        data-updating-note-slot
        className="pointer-events-none absolute inset-x-0 top-[max(0.125rem,env(safe-area-inset-top))] z-10 flex justify-center"
      >
        {isRefreshingSavedData && <KvitUpdatingNote />}
      </div>
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
