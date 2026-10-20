import { Dialog } from '@base-ui/react/dialog'
import { useRef, useState, type ReactNode, type RefObject } from 'react'
import { kvitGlassPanelLook } from '@/shared/components/kvitGlassLooks'
import { cn } from '@/shared/utils/cn'

export type KvitSheetActions = Dialog.Root.Actions

interface KvitSheetProps {
  title: ReactNode
  children: ReactNode
  onClose: () => void
  actionsRef?: RefObject<KvitSheetActions | null>
  returnsFocusOnClose?: () => boolean
}

export function KvitSheet({ title, children, onClose, actionsRef, returnsFocusOnClose }: KvitSheetProps) {
  const [isOpen, setIsOpen] = useState<boolean>(true)
  const [openerElement] = useState<Element | null>(() => document.activeElement)
  const popupRef = useRef<HTMLDivElement>(null)

  function focusAfterClose(): HTMLElement | boolean {
    if (returnsFocusOnClose !== undefined && !returnsFocusOnClose()) {
      return false
    }
    const focused = document.activeElement
    const hasFocusMovedOutside = focused !== null && focused !== document.body && popupRef.current?.contains(focused) !== true
    if (hasFocusMovedOutside) {
      return false
    }
    if (openerElement instanceof HTMLElement && openerElement !== document.body && openerElement.isConnected) {
      return openerElement
    }
    return true
  }

  return (
    <Dialog.Root
      open={isOpen}
      actionsRef={actionsRef}
      onOpenChange={(open) => {
        if (!open) {
          setIsOpen(false)
        }
      }}
      onOpenChangeComplete={(open) => {
        if (!open) {
          onClose()
        }
      }}
    >
      <Dialog.Portal>
        <Dialog.Backdrop className="fixed inset-0 z-50 bg-black/50 transition-[opacity] duration-200 ease-out data-ending-style:opacity-0 data-starting-style:opacity-0 starting:opacity-0 motion-reduce:transition-none" />
        <Dialog.Viewport className="fixed inset-0 z-50 flex items-end justify-center">
          <Dialog.Popup
            ref={popupRef}
            finalFocus={focusAfterClose}
            className={cn(
              'flex max-h-[calc(100dvh_-_max(1.5rem,env(safe-area-inset-top)))] w-full max-w-md flex-col gap-4 rounded-t-[24px] pt-5 transition-[translate,opacity] duration-200 ease-out data-ending-style:translate-y-full data-ending-style:opacity-0 data-starting-style:translate-y-full starting:translate-y-full motion-reduce:transition-none',
              kvitGlassPanelLook,
            )}
          >
            <Dialog.Title className="flex-none px-5 text-lg leading-tight font-bold">{title}</Dialog.Title>
            <div
              data-kvit-sheet-body=""
              className="-mt-1.5 flex min-h-0 flex-col gap-4 overflow-y-auto overscroll-contain px-5 pt-1.5 pb-[max(24px,env(safe-area-inset-bottom))]"
            >
              {children}
            </div>
          </Dialog.Popup>
        </Dialog.Viewport>
      </Dialog.Portal>
    </Dialog.Root>
  )
}
