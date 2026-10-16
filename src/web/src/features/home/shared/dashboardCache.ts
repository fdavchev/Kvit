import type { QueryClient } from '@tanstack/react-query'

export const dashboardKey = ['dashboard'] as const

export async function invalidateDashboard(queryClient: QueryClient): Promise<void> {
  await queryClient.invalidateQueries({ queryKey: dashboardKey, exact: true })
}
