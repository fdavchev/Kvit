import { PersistQueryClientProvider } from '@tanstack/react-query-persist-client'
import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { I18nextProvider } from 'react-i18next'
import { createQueryClient } from '@/core/api/queryClient'
import {
  checkRestoredMeWithServer,
  createSavedQueryCachePersister,
  reportRestoreFailure,
  savedQueryCacheOptions,
} from '@/core/api/savedQueryCache'
import { startI18n } from '@/core/i18n/i18n'
import { RouterAfterRestore } from '@/core/router/RouterAfterRestore'
import { renderStartupFailure } from '@/core/startup/renderStartupFailure'
import { startTheme } from '@/core/theme/startTheme'
import { KvitToaster } from '@/shared/components/KvitToaster'
import './index.css'

startTheme()

const rootElement = document.getElementById('root')
if (rootElement === null) {
  throw new Error('index.html has no element with id "root"')
}

const i18n = await startI18n().catch((error: unknown) => {
  renderStartupFailure(rootElement, error)
  throw error
})
const persister = createSavedQueryCachePersister()
const queryClient = createQueryClient(persister)

createRoot(rootElement).render(
  <StrictMode>
    <I18nextProvider i18n={i18n}>
      <PersistQueryClientProvider
        client={queryClient}
        persistOptions={savedQueryCacheOptions(queryClient, persister)}
        onSuccess={() => checkRestoredMeWithServer(queryClient)}
        onError={() => reportRestoreFailure(persister)}
      >
        <RouterAfterRestore />
        <KvitToaster />
      </PersistQueryClientProvider>
    </I18nextProvider>
  </StrictMode>,
)
