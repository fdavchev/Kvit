import { useQuery, type UseQueryResult } from '@tanstack/react-query'
import { invitePreviewQueryKey } from '@/core/invites/joinRoundTrip'
import { previewInvite, type InvitePreview } from '@/core/services/invites/invitesService'

export function useInvitePreview(token: string): UseQueryResult<InvitePreview> {
  return useQuery({ queryKey: invitePreviewQueryKey(token), queryFn: () => previewInvite(token) })
}
