import { Dialog } from '@base-ui/react/dialog'
import { useState, type ReactNode } from 'react'

interface KvitSheetProps {
  title: string
  children: ReactNode
  onClose: () => void
}

export function KvitSheet({ title, children, onClose }: KvitSheetProps) {
  const [isOpen, setIsOpen] = useState<boolean>(true)

  return (
    <Dialog.Root
      open={isOpen}
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
        <Dialog.Backdrop className="fixed inset-0 z-50 bg-black/50 transition-[opacity] duration-200 ease-out data-ending-style:opacity-0 data-starting-style:opacity-0 motion-reduce:transition-none" />
        <Dialog.Viewport className="fixed inset-0 z-50 flex items-end justify-center">
          <Dialog.Popup className="flex w-full max-w-md flex-col gap-4 rounded-t-[24px] bg-card px-5 pt-5 pb-[max(24px,env(safe-area-inset-bottom))] text-card-foreground transition-[translate,opacity] duration-200 ease-out data-ending-style:translate-y-full data-ending-style:opacity-0 data-starting-style:translate-y-full motion-reduce:transition-none">
            <Dialog.Title className="text-lg leading-tight font-bold">{title}</Dialog.Title>
            {children}
          </Dialog.Popup>
        </Dialog.Viewport>
      </Dialog.Portal>
    </Dialog.Root>
  )
}
