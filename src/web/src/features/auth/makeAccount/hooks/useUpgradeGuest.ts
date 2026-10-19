import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useRef } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router'
import { toast } from 'sonner'
import { errorMessageKey, hasErrorCode } from '@/core/api/errors'
import { meQueryKey } from '@/core/auth/useMe'
import { routes } from '@/core/router/routes'
import {
  googleUpgradeGuest,
  upgradeGuest,
  type UpgradeGuestInput,
} from '@/core/services/auth/authService'
import type { Me } from '@/core/services/me/meService'
import { refreshEveryGroup } from '@/features/groups/shared/groupCache'

export interface GuestUpgrade {
  upgradeWithEmail: (input: UpgradeGuestInput) => void
  upgradeWithGoogle: (idToken: string) => void
  isPending: boolean
  emailError: Error | null
}

export function useUpgradeGuest(onGoogleEmailTaken: (idToken: string) => void): GuestUpgrade {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const isSendingRef = useRef<boolean>(false)

  function finishUpgrade(me: Me): void {
    queryClient.setQueryData<Me | null>(meQueryKey, me)
    void refreshEveryGroup(queryClient)
    toast.success(t('auth.makeAccount.done'))
    navigate(routes.settings, { replace: true })
  }

  function leaveIfAlreadyUpgraded(error: Error): boolean {
    if (!hasErrorCode(error, 'AUTH_NOT_GUEST')) {
      return false
    }
    toast.error(t(errorMessageKey(error)))
    void queryClient.invalidateQueries({ queryKey: meQueryKey })
    navigate(routes.settings, { replace: true })
    return true
  }

  function stopSending(): void {
    isSendingRef.current = false
  }

  function startSending(): boolean {
    if (isSendingRef.current) {
      return false
    }
    isSendingRef.current = true
    return true
  }

  const emailUpgrade = useMutation({
    mutationFn: upgradeGuest,
    onSuccess: finishUpgrade,
    onError: (error) => {
      leaveIfAlreadyUpgraded(error)
    },
    onSettled: stopSending,
  })

  const googleUpgrade = useMutation({
    mutationFn: googleUpgradeGuest,
    onSuccess: finishUpgrade,
    onError: (error, idToken) => {
      if (leaveIfAlreadyUpgraded(error)) {
        return
      }
      if (hasErrorCode(error, 'AUTH_GOOGLE_EMAIL_TAKEN')) {
        onGoogleEmailTaken(idToken)
        return
      }
      console.error('Making an account with Google failed', error)
      toast.error(t(errorMessageKey(error)))
    },
    onSettled: stopSending,
  })

  return {
    upgradeWithEmail: (input) => {
      if (startSending()) {
        emailUpgrade.mutate(input)
      }
    },
    upgradeWithGoogle: (idToken) => {
      if (startSending()) {
        googleUpgrade.mutate(idToken)
      }
    },
    isPending: emailUpgrade.isPending || googleUpgrade.isPending,
    emailError: emailUpgrade.error,
  }
}
