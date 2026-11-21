import { useMatches } from 'react-router'

export const adminOnlyHandle = { isAdminOnly: true } as const

export function useIsAdminOnlyAddress(): boolean {
  return useMatches().some((match) => match.handle === adminOnlyHandle)
}
