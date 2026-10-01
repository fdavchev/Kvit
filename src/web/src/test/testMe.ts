import type { QueryClient } from '@tanstack/react-query'
import { meQueryKey } from '@/core/auth/useMe'
import type { Me } from '@/core/services/me/meService'

export const testMe: Me = {
  id: '0b6f2a52-6c1e-4f55-9a37-3f2d8c1b7e10',
  displayName: 'Filip',
  email: 'filip@example.com',
  language: 'en',
  timeZone: 'Europe/Skopje',
  mustChangePassword: false,
}

export function seedMe(me: Me | null): (queryClient: QueryClient) => void {
  return (queryClient) => {
    queryClient.setQueryData<Me | null>(meQueryKey, me)
  }
}
