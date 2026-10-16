import { useQuery } from '@tanstack/react-query'
import { getDashboard, type DashboardGroup } from '@/core/services/dashboard/dashboardService'
import { dashboardKey } from '../shared/dashboardCache'

const noGroups: readonly DashboardGroup[] = []

export function useLoadedDashboardGroups(): readonly DashboardGroup[] {
  const dashboardQuery = useQuery({ queryKey: dashboardKey, queryFn: getDashboard, enabled: false })
  return dashboardQuery.data?.groups ?? noGroups
}
