import type { ReactNode } from 'react'
import type { OutboxSender } from './outboxSender'
import { OutboxContext } from './useOutbox'

interface OutboxProviderProps {
  sender: OutboxSender
  children: ReactNode
}

export function OutboxProvider({ sender, children }: OutboxProviderProps) {
  return <OutboxContext value={sender}>{children}</OutboxContext>
}
