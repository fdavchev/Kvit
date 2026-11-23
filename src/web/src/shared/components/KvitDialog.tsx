import { Dialog } from '@base-ui/react/dialog'
import { useRef, type ReactNode, type RefObject } from 'react'
import { kvitGlassPanelLook } from '@/shared/components/kvitGlassLooks'
import { useToastsAboveDialog } from '@/shared/hooks/useToastsAboveElement'
import { cn } from '@/shared/utils/cn'

interface KvitDialogProps {
  title: string
  children: ReactNode
  onClose: () => void
}

export function KvitDialog({ title, children, onClose }: KvitDialogProps) {
  const popupRef = useRef<HTMLDivElement>(null)

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
        <Dialog.Backdrop className="fixed inset-0 z-50 bg-black/50" />
        <Dialog.Viewport className="fixed inset-0 z-50 grid place-items-center p-6">
          <KvitDialogPopup popupRef={popupRef} title={title}>
            {children}
          </KvitDialogPopup>
        </Dialog.Viewport>
      </Dialog.Portal>
    </Dialog.Root>
  )
}

interface KvitDialogPopupProps {
  popupRef: RefObject<HTMLDivElement | null>
  title: string
  children: ReactNode
}

function KvitDialogPopup({ popupRef, title, children }: KvitDialogPopupProps) {
  useToastsAboveDialog(popupRef)

  return (
    <Dialog.Popup ref={popupRef} className={cn('flex w-full max-w-sm flex-col gap-3 rounded-[20px] p-6', kvitGlassPanelLook)}>
      <Dialog.Title className="text-xl leading-tight font-bold">{title}</Dialog.Title>
      {children}
    </Dialog.Popup>
  )
}
