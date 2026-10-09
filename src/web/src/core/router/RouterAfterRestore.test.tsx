import { PersistQueryClientProvider, type PersistedClient, type Persister } from '@tanstack/react-query-persist-client'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen } from '@testing-library/react'
import { createMemoryRouter, type DataRouter } from 'react-router'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { createTestPersister } from '@/test/testPersister'
import { RouterAfterRestore } from './RouterAfterRestore'

const routerHolder = vi.hoisted(() => ({ current: undefined as DataRouter | undefined }))

vi.mock('./router', () => ({
  get router() {
    return routerHolder.current
  },
}))

interface SlowRestore {
  persister: Persister
  finish: () => void
  fail: (cause: Error) => void
}

function persisterWithSlowRestore(): SlowRestore {
  let finish: () => void = () => {}
  let fail: (cause: Error) => void = () => {}
  const restored = new Promise<PersistedClient | undefined>((resolve, reject) => {
    finish = () => resolve(undefined)
    fail = reject
  })
  return {
    persister: { ...createTestPersister(), restoreClient: () => restored },
    finish,
    fail,
  }
}

function renderAfterRestore(persister: Persister, onError?: () => void): void {
  render(
    <PersistQueryClientProvider
      client={new QueryClient()}
      persistOptions={{ persister }}
      onError={onError}
    >
      <RouterAfterRestore />
    </PersistQueryClientProvider>,
  )
}

describe('RouterAfterRestore', () => {
  beforeEach(() => {
    routerHolder.current = createMemoryRouter([{ path: '/', element: <p>router page</p> }], {
      initialEntries: ['/'],
    })
  })

  afterEach(() => {
    routerHolder.current?.dispose()
    routerHolder.current = undefined
    vi.restoreAllMocks()
  })

  it('renders nothing while the saved data is still being restored', () => {
    const { persister } = persisterWithSlowRestore()

    renderAfterRestore(persister)

    expect(screen.queryByText('router page')).toBeNull()
    expect(document.body.textContent).toBe('')
  })

  it('renders the router once the restore has finished', async () => {
    const { persister, finish } = persisterWithSlowRestore()
    renderAfterRestore(persister)

    finish()

    expect(await screen.findByText('router page')).toBeTruthy()
  })

  it('renders the router when the restore fails, so the app still starts', async () => {
    vi.spyOn(console, 'error').mockImplementation(() => {})
    vi.spyOn(console, 'warn').mockImplementation(() => {})
    const onError = vi.fn()
    const { persister, fail } = persisterWithSlowRestore()
    renderAfterRestore(persister, onError)

    fail(new Error('IndexedDB is broken'))

    expect(await screen.findByText('router page')).toBeTruthy()
    expect(onError).toHaveBeenCalledTimes(1)
  })

  it('renders the router at once when nothing is being restored', () => {
    render(
      <QueryClientProvider client={new QueryClient()}>
        <RouterAfterRestore />
      </QueryClientProvider>,
    )

    expect(screen.getByText('router page')).toBeTruthy()
  })
})
