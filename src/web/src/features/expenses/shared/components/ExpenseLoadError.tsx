import { ApiError } from '@/core/api/apiClient'
import { isNotFoundError } from '@/core/api/errors'
import { GroupLoadError } from '@/features/groups/shared/components/GroupLoadError'

interface ExpenseLoadErrorProps {
  error: Error
  onRetry: () => void
}

const expenseNotFoundKey = 'errors.EXPENSE_NOT_FOUND'

export function ExpenseLoadError({ error, onRetry }: ExpenseLoadErrorProps) {
  const isCodelessNotFound: boolean =
    isNotFoundError(error) && error instanceof ApiError && error.errorCode === null
  return (
    <GroupLoadError
      error={error}
      onRetry={onRetry}
      messageKey={isCodelessNotFound ? expenseNotFoundKey : undefined}
    />
  )
}
