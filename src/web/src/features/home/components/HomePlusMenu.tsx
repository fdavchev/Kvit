import { useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { useIsGuest } from '@/core/auth/useIsGuest'
import { PersonalSpendingSheet } from '@/features/budget/entries/components/PersonalSpendingSheet'
import { kvitGlassButtonLook, kvitGlassPanelLook } from '@/shared/components/kvitGlassLooks'
import { useToastsAboveElement } from '@/shared/hooks/useToastsAboveElement'
import { cn } from '@/shared/utils/cn'
import { homePlusItemKey, homePlusItems, type HomePlusMenuTarget } from '../homePlusItems'

interface HomePlusMenuProps {
  target: HomePlusMenuTarget | null
}

const itemLook = 'pressable flex min-h-12 items-center gap-2.5 rounded-[14px] px-3.5 font-bold'
const plainItemLook = cn(itemLook, 'hover:bg-secondary-hover')
const mainItemLook = cn(itemLook, 'bg-primary text-primary-foreground hover:bg-primary-hover')

export function HomePlusMenu({ target }: HomePlusMenuProps) {
  const { t } = useTranslation()
  const isGuest = useIsGuest()
  const [isOpen, setIsOpen] = useState<boolean>(false)
  const [isSpendingOpen, setIsSpendingOpen] = useState<boolean>(false)
  const menuAreaRef = useRef<HTMLDivElement>(null)
  const floatingAreaRef = useRef<HTMLDivElement>(null)
  useToastsAboveElement(floatingAreaRef)

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

  function openSpending(): void {
    setIsOpen(false)
    setIsSpendingOpen(true)
  }

  return (
    <div ref={floatingAreaRef} className="pointer-events-none sticky bottom-[calc(var(--bottom-bar-height)+16px)] z-10 mt-auto flex justify-end pt-4">
      <div ref={menuAreaRef} className="pointer-events-auto relative">
        {isOpen && (
          <div
            role="menu"
            className={cn(
              'absolute right-0 bottom-full mb-2.5 flex w-max max-w-[calc(100vw-48px)] min-w-[230px] flex-col rounded-[20px] p-1.5',
              kvitGlassPanelLook,
            )}
          >
            {homePlusItems(target, isGuest, t).map((item) => {
              const content = (
                <>
                  <span aria-hidden="true" className="w-6 text-center">{item.icon}</span>
                  <span className="wrap-anywhere">{item.label}</span>
                </>
              )
              const look = item.isMain ? mainItemLook : plainItemLook
              return item.kind === 'link' ? (
                <Link key={homePlusItemKey(item)} role="menuitem" to={item.to} className={look} onClick={close}>
                  {content}
                </Link>
              ) : (
                <button key={homePlusItemKey(item)} type="button" role="menuitem" className={cn(look, 'text-left')} onClick={openSpending}>
                  {content}
                </button>
              )
            })}
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
      {isSpendingOpen && <PersonalSpendingSheet onClose={() => setIsSpendingOpen(false)} />}
    </div>
  )
}
