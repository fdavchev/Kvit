import { useId } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { errorMessageKey } from '@/core/api/errors'
import type { Group } from '@/core/services/groups/groupsService'
import { useSaveOnce } from '@/features/budget/categories/hooks/useSaveOnce'
import { balancesCardLook } from '@/features/groups/balances/balancesLooks'
import { cn } from '@/shared/utils/cn'
import { useChangeCountsInBudget } from '../hooks/useChangeCountsInBudget'

interface CountInBudgetSwitchProps {
  group: Group
}

export function CountInBudgetSwitch({ group }: CountInBudgetSwitchProps) {
  const { t } = useTranslation()
  const labelId = useId()
  const hintId = useId()
  const changeCounts = useChangeCountsInBudget(group.id)
  const oneAtATime = useSaveOnce()
  const isOn = changeCounts.isPending ? changeCounts.variables : group.countsInBudget

  function toggle(): void {
    if (!oneAtATime.tryStart()) {
      return
    }
    changeCounts.mutate(!isOn, {
      onError: (error) => {
        console.error('Changing whether the group counts in the budget failed', error)
        toast.error(t(errorMessageKey(error)))
      },
      onSettled: oneAtATime.finish,
    })
  }

  return (
    <button
      type="button"
      role="switch"
      aria-checked={isOn}
      aria-labelledby={labelId}
      aria-describedby={hintId}
      onClick={toggle}
      className={cn(balancesCardLook, 'pressable flex min-h-11 w-full items-center gap-4 text-left')}
    >
      <span className="flex min-w-0 flex-1 flex-col gap-0.5">
        <span id={labelId} className="text-[0.9375rem] font-semibold text-pretty">
          {t('groupSettings.countInBudget')}
        </span>
        <span id={hintId} className="text-[0.875rem] text-pretty text-muted-foreground">
          {t('groupSettings.countInBudgetHint')}
        </span>
      </span>
      <span
        aria-hidden="true"
        className={cn(
          'flex h-7 w-12 shrink-0 items-center rounded-full p-0.5',
          isOn ? 'bg-primary' : 'bg-field-border',
        )}
      >
        <span
          className={cn(
            'size-6 rounded-full bg-card shadow-[0_1px_3px_var(--track-shadow)] transition-transform duration-150 ease-out motion-reduce:transition-none',
            isOn ? 'translate-x-5' : 'translate-x-0',
          )}
        />
      </span>
    </button>
  )
}
