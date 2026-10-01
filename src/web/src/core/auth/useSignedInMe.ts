import type { Me } from '@/core/services/me/meService'
import { useMe } from './useMe'

export function useSignedInMe(): Me {
  const { data: me } = useMe()
  if (me === undefined || me === null) {
    throw new Error(
      `Expected a signed-in person in the me cache, found ${String(me)}`,
    )
  }
  return me
}
