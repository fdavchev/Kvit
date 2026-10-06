import { describe, expect, it } from 'vitest'
import { routes } from './routes'

const groupId = '7c1d4e0a-3b52-4a7e-9d6f-1e2a3b4c5d6e'
const expenseId = 'e0000000-0000-4000-8000-000000000004'

describe('routes', () => {
  it.each([
    ['googleSignUp', '/signup/google'],
    ['setPassword', '/settings/set-password'],
    ['privacy', '/privacy'],
    ['groups', '/groups'],
    ['newGroup', '/groups/new'],
    ['recentlyDeletedGroups', '/groups/recently-deleted'],
  ] as const)('has the route %s at %s', (name, path) => {
    expect(routes[name]).toBe(path)
  })

  it.each([
    ['group', '/groups/7c1d4e0a-3b52-4a7e-9d6f-1e2a3b4c5d6e'],
    ['groupSettings', '/groups/7c1d4e0a-3b52-4a7e-9d6f-1e2a3b4c5d6e/settings'],
    ['groupMembers', '/groups/7c1d4e0a-3b52-4a7e-9d6f-1e2a3b4c5d6e/members'],
    ['groupExpenseNew', '/groups/7c1d4e0a-3b52-4a7e-9d6f-1e2a3b4c5d6e/expenses/new'],
    ['groupExpensesDeleted', '/groups/7c1d4e0a-3b52-4a7e-9d6f-1e2a3b4c5d6e/expenses/deleted'],
  ] as const)('builds the route %s for a group id as %s', (name, path) => {
    expect(routes[name](groupId)).toBe(path)
  })

  it('builds the join route for an invite token', () => {
    expect(routes.join('q3Fz8-mXk2_Lw9Tn5Vb1Rc7Yh0JdAeSgUiOpKfXzM4')).toBe(
      '/join/q3Fz8-mXk2_Lw9Tn5Vb1Rc7Yh0JdAeSgUiOpKfXzM4',
    )
  })

  it('turns the join route into the route pattern when it is given :token', () => {
    expect(routes.join(':token')).toBe('/join/:token')
  })

  it.each([
    ['groupExpense', '/groups/7c1d4e0a-3b52-4a7e-9d6f-1e2a3b4c5d6e/expenses/e0000000-0000-4000-8000-000000000004'],
    ['groupExpenseEdit', '/groups/7c1d4e0a-3b52-4a7e-9d6f-1e2a3b4c5d6e/expenses/e0000000-0000-4000-8000-000000000004/edit'],
  ] as const)('builds the route %s for a group id and an expense id as %s', (name, path) => {
    expect(routes[name](groupId, expenseId)).toBe(path)
  })

  it.each([
    ['groupExpenseNew', '/groups/:groupId/expenses/new'],
    ['groupExpensesDeleted', '/groups/:groupId/expenses/deleted'],
  ] as const)('turns %s into the route pattern when it is given :groupId', (name, pattern) => {
    expect(routes[name](':groupId')).toBe(pattern)
  })

  it.each([
    ['groupExpense', '/groups/:groupId/expenses/:expenseId'],
    ['groupExpenseEdit', '/groups/:groupId/expenses/:expenseId/edit'],
  ] as const)('turns %s into the route pattern when it is given :groupId and :expenseId', (name, pattern) => {
    expect(routes[name](':groupId', ':expenseId')).toBe(pattern)
  })
})
