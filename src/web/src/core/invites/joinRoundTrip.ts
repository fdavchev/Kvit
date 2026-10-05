import { readRouterStateText } from '@/core/router/readRouterStateText'
import { routes } from '@/core/router/routes'

export interface JoinTokenState {
  joinToken: string
}

export function invitePreviewQueryKey(token: string): readonly ['invites', string] {
  return ['invites', token]
}

export function readJoinToken(routerState: unknown): string | null {
  return readRouterStateText(routerState, 'joinToken')
}

export function joinPath(token: string): string {
  return routes.join(encodeURIComponent(token))
}

export function joinTokenState(joinToken: string | null): JoinTokenState | undefined {
  return joinToken === null ? undefined : { joinToken }
}

export function pathAfterSignIn(joinToken: string | null): string {
  return joinToken === null ? routes.dashboard : joinPath(joinToken)
}

export function pathBeforeSignIn(joinToken: string | null): string {
  return joinToken === null ? routes.welcome : joinPath(joinToken)
}
