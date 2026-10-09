import { useParams } from 'react-router'

export function useExpenseIdParam(): string {
  const { expenseId } = useParams()
  if (expenseId === undefined) {
    throw new Error('Expected the route to have an expenseId parameter, found none')
  }
  return expenseId
}
