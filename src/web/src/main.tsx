import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { I18nextProvider } from 'react-i18next'
import { RouterProvider } from 'react-router/dom'
import { startI18n } from '@/core/i18n/i18n'
import { router } from '@/core/router/router'
import { KvitToaster } from '@/shared/components/KvitToaster'
import './index.css'

const rootElement = document.getElementById('root')
if (rootElement === null) {
  throw new Error('index.html has no element with id "root"')
}

const i18n = await startI18n()
const queryClient = new QueryClient()

createRoot(rootElement).render(
  <StrictMode>
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={queryClient}>
        <RouterProvider router={router} />
        <KvitToaster />
      </QueryClientProvider>
    </I18nextProvider>
  </StrictMode>,
)
