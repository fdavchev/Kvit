import { useQuery, type UseQueryResult } from '@tanstack/react-query'
import { getDashboard, type Dashboard } from '@/core/services/dashboard/dashboardService'
import { dashboardKey } from '../shared/dashboardCache'

export function useDashboard(): UseQueryResult<Dashboard> {
  return useQuery({ queryKey: dashboardKey, queryFn: getDashboard })
}
