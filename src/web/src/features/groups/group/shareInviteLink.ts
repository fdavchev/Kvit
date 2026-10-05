export type ShareOutcome = 'shared' | 'copied' | 'closed'

export async function shareInviteLink(inviteToken: string): Promise<ShareOutcome> {
  const link = `${window.location.origin}/join/${inviteToken}`
  if (typeof navigator.share === 'function') {
    try {
      await navigator.share({ url: link })
      return 'shared'
    } catch (error) {
      if (error instanceof DOMException && error.name === 'AbortError') {
        return 'closed'
      }
      throw error
    }
  }
  const clipboard: Clipboard | undefined = navigator.clipboard
  if (clipboard === undefined) {
    throw new Error('This browser can neither open a share menu nor copy the invite link')
  }
  await clipboard.writeText(link)
  return 'copied'
}
