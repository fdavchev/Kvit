import { useQuery, type UseQueryResult } from '@tanstack/react-query'
import { getActivity, type ActivityEvent } from '@/core/services/activity/activityService'
import { activityQueryKey } from '../../shared/groupCache'

export function useActivity(groupId: string): UseQueryResult<ActivityEvent[]> {
  return useQuery({ queryKey: activityQueryKey(groupId), queryFn: () => getActivity(groupId) })
}
