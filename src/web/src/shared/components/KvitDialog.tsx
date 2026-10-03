import { Dialog } from '@base-ui/react/dialog'
import type { ReactNode } from 'react'

interface KvitDialogProps {
  title: string
  children: ReactNode
  onClose: () => void
}

export function KvitDialog({ title, children, onClose }: KvitDialogProps) {
  return (
    <Dialog.Root
      open
      onOpenChange={(open) => {
        if (!open) {
          onClose()
        }
      }}
    >
      <Dialog.Portal>
        <Dialog.Backdrop className="fixed inset-0 bg-black/50" />
        <Dialog.Viewport className="fixed inset-0 grid place-items-center p-6">
          <Dialog.Popup className="flex w-full max-w-sm flex-col gap-3 rounded-[20px] bg-card p-6 text-card-foreground">
            <Dialog.Title className="text-xl leading-tight font-bold">{title}</Dialog.Title>
            {children}
          </Dialog.Popup>
        </Dialog.Viewport>
      </Dialog.Portal>
    </Dialog.Root>
  )
}
