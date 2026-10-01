import { QueryClientProvider, type QueryClient } from '@tanstack/react-query'
import { render, renderHook, type RenderHookResult } from '@testing-library/react'
import type { i18n as I18nInstance } from 'i18next'
import { createElement, type ReactNode } from 'react'
import { I18nextProvider } from 'react-i18next'
import { createMemoryRouter, type DataRouter, type RouteObject } from 'react-router'
import { RouterProvider } from 'react-router/dom'
import { createQueryClient } from '@/core/api/queryClient'
import { createI18n } from '@/core/i18n/i18n'
import type { Language } from '@/core/i18n/language'

interface RenderOptions {
  language?: Language
  seedCache?: (queryClient: QueryClient) => void
}

interface Providers {
  queryClient: QueryClient
  i18n: I18nInstance
  withProviders: (props: { children: ReactNode }) => ReactNode
}

interface HookRender<Result> {
  result: RenderHookResult<Result, unknown>['result']
  queryClient: QueryClient
  i18n: I18nInstance
}

interface RoutesRender {
  router: DataRouter
  queryClient: QueryClient
  i18n: I18nInstance
}

async function createProviders(options: RenderOptions): Promise<Providers> {
  const queryClient = createQueryClient()
  const defaultOptions = queryClient.getDefaultOptions()
  queryClient.setDefaultOptions({
    ...defaultOptions,
    queries: { ...defaultOptions.queries, retry: false },
  })
  options.seedCache?.(queryClient)
  const i18n = await createI18n(options.language ?? 'en')

  const withProviders = ({ children }: { children: ReactNode }): ReactNode =>
    createElement(
      I18nextProvider,
      { i18n },
      createElement(QueryClientProvider, { client: queryClient }, children),
    )

  return { queryClient, i18n, withProviders }
}

export async function renderHookWithProviders<Result>(
  hook: () => Result,
  options: RenderOptions = {},
): Promise<HookRender<Result>> {
  const { queryClient, i18n, withProviders } = await createProviders(options)
  const { result } = renderHook(hook, { wrapper: withProviders })
  return { result, queryClient, i18n }
}

export async function renderRoutesWithProviders(
  routeObjects: RouteObject[],
  initialPath: string,
  options: RenderOptions = {},
): Promise<RoutesRender> {
  const { queryClient, i18n, withProviders } = await createProviders(options)
  const router = createMemoryRouter(routeObjects, { initialEntries: [initialPath] })
  render(<RouterProvider router={router} />, { wrapper: withProviders })
  return { router, queryClient, i18n }
}
