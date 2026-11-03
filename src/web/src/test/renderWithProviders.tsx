import { QueryClientProvider, type QueryClient } from '@tanstack/react-query'
import type { Persister } from '@tanstack/react-query-persist-client'
import { render, renderHook, type RenderHookResult } from '@testing-library/react'
import type { i18n as I18nInstance } from 'i18next'
import type { ReactElement, ReactNode } from 'react'
import { I18nextProvider } from 'react-i18next'
import { createMemoryRouter, type DataRouter, type RouteObject } from 'react-router'
import { RouterProvider } from 'react-router/dom'
import { createQueryClient } from '@/core/api/queryClient'
import { createI18n } from '@/core/i18n/i18n'
import type { Language } from '@/core/i18n/language'
import { OutboxProvider } from '@/core/outbox/OutboxProvider'
import { createOutboxSender, type OutboxSender } from '@/core/outbox/outboxSender'
import { refreshAfterQueuedItem, sendQueuedItem } from '@/features/outbox/outboxDispatch'
import { testMe } from './testMe'
import { createTestPersister } from './testPersister'

interface RenderOptions {
  language?: Language
  seedCache?: (queryClient: QueryClient) => void
  prepareI18n?: (i18n: I18nInstance) => void
  routerState?: unknown
  persister?: Persister
}

interface Providers {
  queryClient: QueryClient
  i18n: I18nInstance
  outboxSender: OutboxSender
  withProviders: (props: { children: ReactNode }) => ReactNode
}

interface HookRender<Result> {
  result: RenderHookResult<Result, unknown>['result']
  queryClient: QueryClient
  i18n: I18nInstance
  outboxSender: OutboxSender
}

interface RoutesRender {
  router: DataRouter
  queryClient: QueryClient
  i18n: I18nInstance
  outboxSender: OutboxSender
}

async function createProviders(options: RenderOptions): Promise<Providers> {
  const queryClient = createQueryClient(options.persister ?? createTestPersister())
  const defaultOptions = queryClient.getDefaultOptions()
  queryClient.setDefaultOptions({
    ...defaultOptions,
    queries: { ...defaultOptions.queries, retry: false },
  })
  options.seedCache?.(queryClient)
  const i18n = await createI18n(options.language ?? 'en')
  options.prepareI18n?.(i18n)
  const outboxSender = createOutboxSender({
    personId: testMe.id,
    sendItem: sendQueuedItem,
    afterSent: (item) => refreshAfterQueuedItem(queryClient, item),
  })

  const withProviders = ({ children }: { children: ReactNode }): ReactNode => (
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={queryClient}>
        <OutboxProvider sender={outboxSender}>{children}</OutboxProvider>
      </QueryClientProvider>
    </I18nextProvider>
  )

  return { queryClient, i18n, outboxSender, withProviders }
}

export async function renderHookWithProviders<Result>(
  hook: () => Result,
  options: RenderOptions = {},
): Promise<HookRender<Result>> {
  const { queryClient, i18n, outboxSender, withProviders } = await createProviders(options)
  const { result } = renderHook(hook, { wrapper: withProviders })
  return { result, queryClient, i18n, outboxSender }
}

export async function renderRoutesWithProviders(
  routeObjects: RouteObject[],
  initialPath: string,
  options: RenderOptions = {},
): Promise<RoutesRender> {
  const { queryClient, i18n, outboxSender, withProviders } = await createProviders(options)
  const initialEntry =
    options.routerState === undefined
      ? initialPath
      : { pathname: initialPath, state: options.routerState }
  const router = createMemoryRouter(routeObjects, { initialEntries: [initialEntry] })
  render(<RouterProvider router={router} />, { wrapper: withProviders })
  return { router, queryClient, i18n, outboxSender }
}

export async function renderElementWithProviders(
  element: ReactElement,
  path: string,
  options: RenderOptions = {},
): Promise<RoutesRender> {
  return renderRoutesWithProviders(
    [
      { path, element },
      { path: '*', element: <p>another page</p> },
    ],
    path,
    options,
  )
}
