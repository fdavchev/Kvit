import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { routes } from '@/core/router/routes'
import { kvitGlassButtonLook } from '@/shared/components/kvitGlassLooks'
import { kvitIconAttributes } from '@/shared/components/kvitIconAttributes'
import { cn } from '@/shared/utils/cn'

interface AddExpenseButtonProps {
  groupId: string
}

export function AddExpenseButton({ groupId }: AddExpenseButtonProps) {
  const { t } = useTranslation()

  return (
    <Link
      to={routes.groupExpenseNew(groupId)}
      aria-label={t('expense.addTitle')}
      className={cn('pressable pointer-events-auto grid size-15 place-items-center rounded-[20px]', kvitGlassButtonLook)}
    >
      <svg {...kvitIconAttributes} strokeWidth={2.5} className="size-8">
        <path d="M12 5v14M5 12h14" />
      </svg>
    </Link>
  )
}
