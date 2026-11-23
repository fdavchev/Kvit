import { createContext, useContext } from 'react'

export const KvitFormErrorIdContext = createContext<string | null>(null)

export function useKvitFormErrorId(): string | null {
  return useContext(KvitFormErrorIdContext)
}
