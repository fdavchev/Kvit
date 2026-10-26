import type { QueryClient } from '@tanstack/react-query'
import { meQueryKey } from '@/core/auth/useMe'
import type { Me } from '@/core/services/me/meService'

export const testMe: Me & { email: string } = {
  id: '0b6f2a52-6c1e-4f55-9a37-3f2d8c1b7e10',
  displayName: 'Filip',
  email: 'filip@example.com',
  language: 'en',
  timeZone: 'Europe/Skopje',
  mustChangePassword: false,
  hasPassword: true,
  pictureUrl: null,
  isGuest: false,
  budgetTotalCurrency: null,
}

export const testGuestMe: Me = {
  id: '5d0c9e1b-2a4f-4c6d-8e7f-9a0b1c2d3e4f',
  displayName: 'Marko',
  email: null,
  language: 'en',
  timeZone: 'Europe/Skopje',
  mustChangePassword: false,
  hasPassword: false,
  pictureUrl: null,
  isGuest: true,
  budgetTotalCurrency: null,
}

export function seedMe(me: Me | null): (queryClient: QueryClient) => void {
  return (queryClient) => {
    queryClient.setQueryData<Me | null>(meQueryKey, me)
  }
}
