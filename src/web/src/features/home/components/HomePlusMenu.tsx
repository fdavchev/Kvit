import { useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { routes } from '@/core/router/routes'
import { kvitGlassButtonLook, kvitGlassPanelLook } from '@/shared/components/kvitGlassLooks'
import { cn } from '@/shared/utils/cn'

export interface HomePlusMenuTarget {
  groupId: string
  groupName: string
}

interface HomePlusMenuProps {
  target: HomePlusMenuTarget | null
}

const itemLook = 'pressable flex min-h-12 items-center gap-2.5 rounded-[14px] px-3.5 font-bold'
const plainItemLook = cn(itemLook, 'hover:bg-secondary-hover')

export function HomePlusMenu({ target }: HomePlusMenuProps) {
  const { t } = useTranslation()
  const [isOpen, setIsOpen] = useState<boolean>(false)
  const menuAreaRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (!isOpen) {
      return
    }
    function closeOnEscape(event: KeyboardEvent): void {
      if (event.key === 'Escape') {
        setIsOpen(false)
      }
    }
    function closeOnTapOutside(event: Event): void {
      if (event.target instanceof Node && menuAreaRef.current?.contains(event.target) === false) {
        setIsOpen(false)
      }
    }
    document.addEventListener('keydown', closeOnEscape)
    document.addEventListener('pointerdown', closeOnTapOutside)
    return () => {
      document.removeEventListener('keydown', closeOnEscape)
      document.removeEventListener('pointerdown', closeOnTapOutside)
    }
  }, [isOpen])

  function close(): void {
    setIsOpen(false)
  }

  return (
    <div className="pointer-events-none sticky bottom-[calc(var(--bottom-bar-height)+16px)] z-10 mt-auto flex justify-end pt-4">
      <div ref={menuAreaRef} className="pointer-events-auto relative">
        {isOpen && (
          <div
            role="menu"
            className={cn(
              'absolute right-0 bottom-full mb-2.5 flex w-max max-w-[calc(100vw-48px)] min-w-[230px] flex-col rounded-[20px] p-1.5',
              kvitGlassPanelLook,
            )}
          >
            {target !== null && (
              <Link
                role="menuitem"
                to={routes.groupExpenseNew(target.groupId)}
                className={cn(itemLook, 'bg-primary text-primary-foreground hover:bg-primary-hover')}
                onClick={close}
              >
                <span aria-hidden="true" className="w-6 text-center">＋</span>
                <span className="wrap-anywhere">{t('home.plusAddTo', { group: target.groupName })}</span>
              </Link>
            )}
            <Link role="menuitem" to={routes.newBill} className={plainItemLook} onClick={close}>
              <span aria-hidden="true" className="w-6 text-center">🧾</span>
              {t('home.plusNewOneBill')}
            </Link>
            <Link role="menuitem" to={routes.groups} className={plainItemLook} onClick={close}>
              <span aria-hidden="true" className="w-6 text-center">⋯</span>
              {t('home.plusAnotherGroup')}
            </Link>
          </div>
        )}
        <button
          type="button"
          aria-label={t('home.plus')}
          aria-haspopup="menu"
          aria-expanded={isOpen}
          className={cn('pressable grid size-15 place-items-center rounded-[20px] text-[2rem] leading-none', kvitGlassButtonLook)}
          onClick={() => setIsOpen(!isOpen)}
        >
          <span aria-hidden="true">{isOpen ? '×' : '+'}</span>
        </button>
      </div>
    </div>
  )
}
