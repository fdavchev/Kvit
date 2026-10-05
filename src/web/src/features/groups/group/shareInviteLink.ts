import { routes } from '@/core/router/routes'

export type ShareOutcome = 'shared' | 'copied' | 'closed'

export async function shareInviteLink(inviteToken: string): Promise<ShareOutcome> {
  if (typeof navigator.share === 'function') {
    try {
      await navigator.share({ url: inviteLinkOf(inviteToken) })
      return 'shared'
    } catch (error) {
      if (error instanceof DOMException && error.name === 'AbortError') {
        return 'closed'
      }
      throw error
    }
  }
  await copyInviteLink(inviteToken)
  return 'copied'
}

export async function copyInviteLink(inviteToken: string): Promise<void> {
  const clipboard: Clipboard | undefined = navigator.clipboard
  if (clipboard === undefined) {
    throw new Error('This browser can neither open a share menu nor copy the invite link')
  }
  await clipboard.writeText(inviteLinkOf(inviteToken))
}

function inviteLinkOf(inviteToken: string): string {
  return `${window.location.origin}${routes.join(inviteToken)}`
}
