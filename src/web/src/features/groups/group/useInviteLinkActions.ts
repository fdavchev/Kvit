import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { copyInviteLink, shareInviteLink } from './shareInviteLink'

interface InviteLinkActions {
  share: (inviteToken: string) => Promise<void>
  copy: (inviteToken: string) => Promise<void>
}

export function useInviteLinkActions(): InviteLinkActions {
  const { t } = useTranslation()

  function showFailure(error: unknown): void {
    console.error('Sharing the invite link failed', error)
    toast.error(t('errors.generic'))
  }

  async function share(inviteToken: string): Promise<void> {
    try {
      const outcome = await shareInviteLink(inviteToken)
      if (outcome === 'copied') {
        toast.success(t('group.linkCopied'))
      }
    } catch (error) {
      showFailure(error)
    }
  }

  async function copy(inviteToken: string): Promise<void> {
    try {
      await copyInviteLink(inviteToken)
      toast.success(t('group.linkCopied'))
    } catch (error) {
      showFailure(error)
    }
  }

  return { share, copy }
}
