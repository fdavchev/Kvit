import { QueryClient, QueryObserver } from '@tanstack/react-query'
import type { Persister } from '@tanstack/react-query-persist-client'
import { afterEach, describe, expect, it, vi, type Mock } from 'vitest'
import { createQueryClient } from '@/core/api/queryClient'
import type { Me } from '@/core/services/me/meService'
import { testMe } from '@/test/testMe'
import { createTestPersister } from '@/test/testPersister'
import { keepCacheToOnePerson } from './keepCacheToOnePerson'
import { meQueryKey } from './useMe'

const groupsKey = ['groups']
const groups = ['Greece trip']
const otherPerson: Me = { ...testMe, id: 'a7d1c2e4-0000-4000-8000-000000000002', displayName: 'Mila' }

interface Setup {
  queryClient: QueryClient
  persister: Persister
  removeClient: Mock<Persister['removeClient']>
}

function setUpWithSignedInPerson(): Setup {
  const removeClient = vi.fn<Persister['removeClient']>()
  const persister: Persister = { ...createTestPersister(), removeClient }
  const queryClient = new QueryClient()
  keepCacheToOnePerson(queryClient, persister)
  queryClient.setQueryData<Me | null>(meQueryKey, testMe)
  queryClient.setQueryData(groupsKey, groups)
  return { queryClient, persister, removeClient }
}

describe('keepCacheToOnePerson', () => {
  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('removes every query except me and deletes the saved copy when the person signs out', () => {
    const { queryClient, removeClient } = setUpWithSignedInPerson()
    queryClient.setQueryData(['expenses', 'one'], ['Lunch'])

    queryClient.setQueryData<Me | null>(meQueryKey, null)

    expect(queryClient.getQueryCache().find({ queryKey: groupsKey })).toBeUndefined()
    expect(queryClient.getQueryCache().find({ queryKey: ['expenses', 'one'] })).toBeUndefined()
    expect(queryClient.getQueryData(meQueryKey)).toBeNull()
    expect(removeClient).toHaveBeenCalledTimes(1)
  })

  it('resets every query except me and deletes the saved copy when another person signs in', () => {
    const { queryClient, removeClient } = setUpWithSignedInPerson()
    queryClient.setQueryData(['expenses', 'one'], ['Lunch'])

    queryClient.setQueryData<Me | null>(meQueryKey, otherPerson)

    expect(queryClient.getQueryData(groupsKey)).toBeUndefined()
    expect(queryClient.getQueryData(['expenses', 'one'])).toBeUndefined()
    expect(queryClient.getQueryData(meQueryKey)).toEqual(otherPerson)
    expect(removeClient).toHaveBeenCalledTimes(1)
  })

  it('refetches a query that is being watched when another person signs in', async () => {
    const { queryClient } = setUpWithSignedInPerson()
    const queryFn = vi.fn(() => Promise.resolve(['Skopje trip']))
    const watcher = new QueryObserver(queryClient, {
      queryKey: groupsKey,
      queryFn,
      staleTime: Infinity,
    })
    const stopWatching = watcher.subscribe(() => {})

    queryClient.setQueryData<Me | null>(meQueryKey, otherPerson)

    await vi.waitFor(() => {
      expect(queryClient.getQueryData(groupsKey)).toEqual(['Skopje trip'])
    })
    expect(queryFn).toHaveBeenCalledTimes(1)
    stopWatching()
  })

  it('does nothing when the same person is saved again with a changed profile', () => {
    const { queryClient, removeClient } = setUpWithSignedInPerson()

    const renamedPerson: Me = { ...testMe, displayName: 'Filip D.' }

    queryClient.setQueryData<Me | null>(meQueryKey, renamedPerson)

    expect(queryClient.getQueryData(groupsKey)).toEqual(groups)
    expect(removeClient).not.toHaveBeenCalled()
  })

  it('does not wipe anything when a person signs in after nobody was known', () => {
    const removeClient = vi.fn<Persister['removeClient']>()
    const queryClient = new QueryClient()
    keepCacheToOnePerson(queryClient, { ...createTestPersister(), removeClient })
    queryClient.setQueryData(groupsKey, groups)

    queryClient.setQueryData<Me | null>(meQueryKey, testMe)

    expect(queryClient.getQueryData(groupsKey)).toEqual(groups)
    expect(removeClient).not.toHaveBeenCalled()
  })

  it('does not wipe anything when a person signs in after a signed-out visitor', () => {
    const removeClient = vi.fn<Persister['removeClient']>()
    const queryClient = new QueryClient()
    keepCacheToOnePerson(queryClient, { ...createTestPersister(), removeClient })
    queryClient.setQueryData<Me | null>(meQueryKey, null)
    queryClient.setQueryData(groupsKey, groups)

    queryClient.setQueryData<Me | null>(meQueryKey, testMe)

    expect(queryClient.getQueryData(groupsKey)).toEqual(groups)
    expect(removeClient).not.toHaveBeenCalled()
  })

  it('ignores changes to queries that are not me', () => {
    const { queryClient, removeClient } = setUpWithSignedInPerson()

    queryClient.setQueryData(groupsKey, ['Another trip'])
    queryClient.setQueryData(['me', 'details'], 'not the signed-in person')

    expect(queryClient.getQueryData(groupsKey)).toEqual(['Another trip'])
    expect(removeClient).not.toHaveBeenCalled()
  })

  it('stops watching when the returned function is called', () => {
    const removeClient = vi.fn<Persister['removeClient']>()
    const queryClient = new QueryClient()
    const stopWatching = keepCacheToOnePerson(queryClient, { ...createTestPersister(), removeClient })
    queryClient.setQueryData<Me | null>(meQueryKey, testMe)
    queryClient.setQueryData(groupsKey, groups)

    stopWatching()
    queryClient.setQueryData<Me | null>(meQueryKey, null)

    expect(queryClient.getQueryData(groupsKey)).toEqual(groups)
    expect(removeClient).not.toHaveBeenCalled()
  })

  it('is started by createQueryClient with the persister it is given', () => {
    const removeClient = vi.fn<Persister['removeClient']>()
    const queryClient = createQueryClient({ ...createTestPersister(), removeClient })
    queryClient.setQueryData<Me | null>(meQueryKey, testMe)

    queryClient.setQueryData<Me | null>(meQueryKey, null)

    expect(removeClient).toHaveBeenCalledTimes(1)
  })

  it('reports a rejected removeClient with the id of the previous person and the cause', async () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {})
    const cause = new Error('IndexedDB is full')
    const { queryClient, removeClient } = setUpWithSignedInPerson()
    removeClient.mockRejectedValue(cause)

    queryClient.setQueryData<Me | null>(meQueryKey, null)

    await vi.waitFor(() => {
      expect(consoleError).toHaveBeenCalledTimes(1)
    })
    expect(consoleError).toHaveBeenCalledWith(
      `Could not delete the saved data of the person who was signed in before (${testMe.id}) from this browser (IndexedDB)`,
      cause,
    )
  })

  it('reports a removeClient that throws at once the same way', async () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {})
    const cause = new Error('IndexedDB is blocked')
    const { queryClient, removeClient } = setUpWithSignedInPerson()
    removeClient.mockImplementation(() => {
      throw cause
    })

    queryClient.setQueryData<Me | null>(meQueryKey, otherPerson)

    await vi.waitFor(() => {
      expect(consoleError).toHaveBeenCalledTimes(1)
    })
    expect(consoleError).toHaveBeenCalledWith(
      `Could not delete the saved data of the person who was signed in before (${testMe.id}) from this browser (IndexedDB)`,
      cause,
    )
  })

  it('still clears the other queries when removeClient fails', async () => {
    vi.spyOn(console, 'error').mockImplementation(() => {})
    const { queryClient, removeClient } = setUpWithSignedInPerson()
    removeClient.mockRejectedValue(new Error('IndexedDB is full'))

    queryClient.setQueryData<Me | null>(meQueryKey, null)

    expect(queryClient.getQueryCache().find({ queryKey: groupsKey })).toBeUndefined()
    await vi.waitFor(() => {
      expect(removeClient).toHaveBeenCalled()
    })
  })
})
