import { useRef } from 'react'
import { useTranslation } from 'react-i18next'
import { kvitGlassButtonLook } from '@/shared/components/kvitGlassLooks'
import { kvitRoundActionLooks } from '@/shared/components/kvitRoundActionLooks'
import { useToastsAboveElement } from '@/shared/hooks/useToastsAboveElement'
import { cn } from '@/shared/utils/cn'

interface BudgetAddButtonProps {
  onClick: () => void
}

export function BudgetFloatingAddButton({ onClick }: BudgetAddButtonProps) {
  const { t } = useTranslation()
  const floatingAreaRef = useRef<HTMLDivElement>(null)
  useToastsAboveElement(floatingAreaRef)

  return (
    <div
      ref={floatingAreaRef}
      data-floating-add
      className="pointer-events-none sticky bottom-[calc(var(--bottom-bar-height)+16px)] z-10 mt-auto flex justify-end pt-4"
    >
      <button
        type="button"
        aria-label={t('budget.screen.add')}
        className={cn(
          'pressable pointer-events-auto grid size-15 place-items-center rounded-[20px] text-[2rem] leading-none',
          kvitGlassButtonLook,
        )}
        onClick={onClick}
      >
        <span aria-hidden="true">+</span>
      </button>
    </div>
  )
}

export function BudgetRoundAddButton({ onClick }: BudgetAddButtonProps) {
  const { t } = useTranslation()

  return (
    <div className="mt-4 flex">
      <button type="button" className={kvitRoundActionLooks.main} onClick={onClick}>
        <span aria-hidden="true">＋</span>
        <span className="wrap-anywhere">{t('budget.screen.add')}</span>
      </button>
    </div>
  )
}
