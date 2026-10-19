import { useMe } from './useMe'

export function useIsGuest(): boolean {
  const { data: me } = useMe()
  return me?.isGuest === true
}
