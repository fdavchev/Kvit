import { useMe } from './useMe'

export function useIsAdmin(): boolean {
  const { data: me } = useMe()
  return me?.isAdmin === true
}
