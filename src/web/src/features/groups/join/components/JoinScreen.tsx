import { useTranslation } from 'react-i18next'
import { Navigate, useParams } from 'react-router'
import { errorMessageKey, hasErrorCode } from '@/core/api/errors'
import { useMe } from '@/core/auth/useMe'
import { routes } from '@/core/router/routes'
import { useGoogleSignIn } from '@/features/auth/google/hooks/useGoogleSignIn'
import { KvitError } from '@/shared/components/KvitError'
import { KvitLoading } from '@/shared/components/KvitLoading'
import { KvitScreen } from '@/shared/components/KvitScreen'
import { useInvitePreview } from '../hooks/useInvitePreview'
import { InviteCard } from './InviteCard'
import { JoinMessage } from './JoinMessage'
import { SignedInJoin } from './SignedInJoin'
import { SignedOutJoin } from './SignedOutJoin'

export function JoinScreen() {
  const { t } = useTranslation()
  const token = useJoinTokenParam()
  const meQuery = useMe()
  const previewQuery = useInvitePreview(token)
  const signInWithGoogle = useGoogleSignIn(token)

  function renderContent() {
    if (previewQuery.isError) {
      if (hasErrorCode(previewQuery.error, 'INVITE_NOT_FOUND')) {
        return <JoinMessage message={t('errors.INVITE_NOT_FOUND')} />
      }
      return (
        <KvitError
          message={t(errorMessageKey(previewQuery.error))}
          onRetry={() => void previewQuery.refetch()}
        />
      )
    }
    if (meQuery.isError) {
      return (
        <KvitError
          message={t(errorMessageKey(meQuery.error))}
          onRetry={() => void meQuery.refetch()}
        />
      )
    }
    if (previewQuery.isPending || meQuery.isPending) {
      return <KvitLoading />
    }
    const preview = previewQuery.data
    if (preview.status === 'AlreadyMember') {
      if (preview.groupId === null) {
        throw new Error('The invite says the person is already in the group but gives no group id')
      }
      return <Navigate to={routes.group(preview.groupId)} replace />
    }
    if (preview.status === 'Removed') {
      return <JoinMessage message={t('join.removed', { name: preview.name })} />
    }
    return (
      <>
        <InviteCard preview={preview} />
        {meQuery.data === null ? (
          <SignedOutJoin token={token} onGoogleCredential={signInWithGoogle} />
        ) : (
          <SignedInJoin token={token} preview={preview} />
        )}
      </>
    )
  }

  return (
    <KvitScreen>
      <div className="min-h-12" />
      {renderContent()}
    </KvitScreen>
  )
}

function useJoinTokenParam(): string {
  const { token } = useParams()
  if (token === undefined) {
    throw new Error('Expected the route to have a token parameter, found none')
  }
  return token
}
