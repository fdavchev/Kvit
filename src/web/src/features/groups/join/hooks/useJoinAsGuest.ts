import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router'
import { toast } from 'sonner'
import { errorMessageKey, hasErrorCode } from '@/core/api/errors'
import { meQueryKey } from '@/core/auth/useMe'
import { useLanguage } from '@/core/i18n/useLanguage'
import { invitePreviewQueryKey } from '@/core/invites/joinRoundTrip'
import { routes } from '@/core/router/routes'
import { joinAsGuest, type GuestJoinChoice } from '@/core/services/invites/invitesService'
import { getMe, type Me } from '@/core/services/me/meService'
import { refreshEveryGroup } from '../../shared/groupCache'

interface GuestJoined {
  groupId: string
  me: Me
}

export function useJoinAsGuest(
  token: string,
  groupName: string,
): UseMutationResult<GuestJoined, Error, GuestJoinChoice> {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const { language } = useLanguage()

  return useMutation({
    mutationFn: async (choice: GuestJoinChoice) => {
      const groupId = await joinAsGuest(token, choice, language)
      const me = await getMe()
      if (me === null) {
        throw new Error(
          `Joined the group ${groupId} as a guest, but asking who is signed in answered that nobody is`,
        )
      }
      return { groupId, me }
    },
    onSuccess: ({ groupId, me }) => {
      queryClient.setQueryData<Me | null>(meQueryKey, me)
      void queryClient.invalidateQueries({
        queryKey: invitePreviewQueryKey(token),
        refetchType: 'none',
      })
      void refreshEveryGroup(queryClient)
      toast.success(t('join.joined', { name: groupName }))
      navigate(routes.group(groupId), { replace: true, flushSync: true })
    },
    onError: (error) => {
      if (hasErrorCode(error, 'MEMBER_NOT_FOUND')) {
        void queryClient.invalidateQueries({ queryKey: invitePreviewQueryKey(token) })
        return
      }
      if (hasErrorCode(error, 'GUEST_JOIN_SIGNED_IN')) {
        console.error('This browser is already signed in, so no guest was made', error)
        toast.error(t(errorMessageKey(error)))
        void queryClient.invalidateQueries({ queryKey: meQueryKey })
        void queryClient.invalidateQueries({ queryKey: invitePreviewQueryKey(token) })
      }
    },
  })
}
