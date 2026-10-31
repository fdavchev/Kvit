import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import {
  fakeIdbKeyval,
  fakeOutboxStore,
  resetFakeOutboxStorage,
  seedOutbox,
  storedIds,
  storedOutbox,
} from '@/test/outboxTestHelpers'
import { testOutboxItem } from '@/test/outboxTestData'
import type { OutboxItem } from './outboxItem'
import {
  addOutboxItem,
  clearOutbox,
  readOutbox,
  removeOutboxItem,
  updateOutboxItem,
} from './outboxStorage'

vi.mock('idb-keyval', async () => (await import('@/test/outboxTestHelpers')).fakeIdbKeyval)

const personId = 'person-1'
const otherPersonId = 'person-2'
const createStoreCalls = [...fakeIdbKeyval.createStore.mock.calls]

function idsOf(items: OutboxItem[]): string[] {
  return items.map((item) => item.id)
}

beforeEach(() => {
  resetFakeOutboxStorage()
})

describe('outbox storage database', () => {
  it('opens its own kvit-outbox database with an items store', () => {
    expect(createStoreCalls).toEqual([['kvit-outbox', 'items']])
  })

  it('writes in its own database and never in the default one', async () => {
    await addOutboxItem(personId, testOutboxItem('a'))

    expect(fakeIdbKeyval.update).toHaveBeenCalledWith(personId, expect.any(Function), fakeOutboxStore)
  })

  it('reads from its own database and never from the default one', async () => {
    await readOutbox(personId)

    expect(fakeIdbKeyval.get).toHaveBeenCalledWith(personId, fakeOutboxStore)
  })

  it('deletes from its own database and never from the default one', async () => {
    await clearOutbox(personId)

    expect(fakeIdbKeyval.del).toHaveBeenCalledWith(personId, fakeOutboxStore)
  })

  it('keeps one ordered list of items under the key of the signed-in person', async () => {
    await addOutboxItem(personId, testOutboxItem('a'))
    await addOutboxItem(personId, testOutboxItem('b'))

    expect([...storedOutbox.keys()]).toEqual([personId])
    expect(storedIds(personId)).toEqual(['a', 'b'])
  })
})

describe('readOutbox', () => {
  afterEach(() => {
    vi.useRealTimers()
  })

  it('gives an empty list for a person who has nothing queued', async () => {
    await expect(readOutbox(personId)).resolves.toEqual([])
  })

  it('gives back the items of the person in the order they were queued', async () => {
    seedOutbox(personId, [testOutboxItem('c'), testOutboxItem('a'), testOutboxItem('b')])

    expect(idsOf(await readOutbox(personId))).toEqual(['c', 'a', 'b'])
  })

  it('gives back every field of a stored item unchanged', async () => {
    const item = testOutboxItem('a', {
      kind: 'settlement',
      groupId: null,
      state: 'failed',
      failure: { errorCode: 'GROUP_FINISHED', httpStatus: 400 },
    })
    seedOutbox(personId, [item])

    await expect(readOutbox(personId)).resolves.toEqual([item])
  })

  it('shows a person only their own items', async () => {
    seedOutbox(personId, [testOutboxItem('mine')])
    seedOutbox(otherPersonId, [testOutboxItem('theirs')])

    expect(idsOf(await readOutbox(personId))).toEqual(['mine'])
    expect(idsOf(await readOutbox(otherPersonId))).toEqual(['theirs'])
  })

  it('stops its time limit timer once the read has answered', async () => {
    vi.useFakeTimers({ toFake: ['setTimeout', 'clearTimeout'] })
    seedOutbox(personId, [testOutboxItem('a')])

    await readOutbox(personId)

    expect(vi.getTimerCount()).toBe(0)
  })

  it('fails with a specific error once the read takes longer than 3 seconds', async () => {
    vi.useFakeTimers({ toFake: ['setTimeout', 'clearTimeout'] })
    fakeIdbKeyval.get.mockReturnValueOnce(new Promise<OutboxItem[] | undefined>(() => {}))

    const reading = readOutbox(personId)
    const failure = expect(reading).rejects.toThrow('longer than 3000 ms')
    await vi.advanceTimersByTimeAsync(3000)

    await failure
  })

  it('does not fail before the 3 seconds have passed', async () => {
    vi.useFakeTimers({ toFake: ['setTimeout', 'clearTimeout'] })
    fakeIdbKeyval.get.mockReturnValueOnce(new Promise<OutboxItem[] | undefined>(() => {}))
    const onFailure = vi.fn()

    void readOutbox(personId).catch(onFailure)
    await vi.advanceTimersByTimeAsync(2999)

    expect(onFailure).not.toHaveBeenCalled()
  })

  it('passes on the storage error when reading fails instead of answering an empty list', async () => {
    fakeIdbKeyval.get.mockRejectedValueOnce(new Error('IndexedDB read failed'))

    await expect(readOutbox(personId)).rejects.toThrow('IndexedDB read failed')
  })
})

describe('addOutboxItem', () => {
  it('queues the first item for a person', async () => {
    const item = testOutboxItem('a')

    await addOutboxItem(personId, item)

    await expect(readOutbox(personId)).resolves.toEqual([item])
  })

  it('keeps insertion order', async () => {
    await addOutboxItem(personId, testOutboxItem('c'))
    await addOutboxItem(personId, testOutboxItem('a'))
    await addOutboxItem(personId, testOutboxItem('b'))

    expect(idsOf(await readOutbox(personId))).toEqual(['c', 'a', 'b'])
  })

  it('keeps every item when several are added at the same time', async () => {
    await Promise.all([
      addOutboxItem(personId, testOutboxItem('a')),
      addOutboxItem(personId, testOutboxItem('b')),
      addOutboxItem(personId, testOutboxItem('c')),
    ])

    expect(idsOf(await readOutbox(personId))).toEqual(['a', 'b', 'c'])
  })

  it('does not create a duplicate when the id is already queued', async () => {
    await addOutboxItem(personId, testOutboxItem('a'))
    await addOutboxItem(personId, testOutboxItem('b'))

    await addOutboxItem(personId, testOutboxItem('a', { display: { title: 'Changed' } }))

    expect(idsOf(await readOutbox(personId))).toEqual(['a', 'b'])
  })

  it('does not touch the items of another person', async () => {
    seedOutbox(otherPersonId, [testOutboxItem('theirs')])

    await addOutboxItem(personId, testOutboxItem('mine'))

    expect(storedIds(otherPersonId)).toEqual(['theirs'])
    expect(storedIds(personId)).toEqual(['mine'])
  })

  it('allows the same id for two different people', async () => {
    await addOutboxItem(personId, testOutboxItem('same'))
    await addOutboxItem(otherPersonId, testOutboxItem('same'))

    expect(storedIds(personId)).toEqual(['same'])
    expect(storedIds(otherPersonId)).toEqual(['same'])
  })
})

describe('updateOutboxItem', () => {
  it('changes only the fields in the patch', async () => {
    const item = testOutboxItem('a')
    await addOutboxItem(personId, item)

    await updateOutboxItem(personId, 'a', {
      state: 'failed',
      failure: { errorCode: 'GROUP_NOT_FOUND', httpStatus: 404 },
    })

    await expect(readOutbox(personId)).resolves.toEqual([
      { ...item, state: 'failed', failure: { errorCode: 'GROUP_NOT_FOUND', httpStatus: 404 } },
    ])
  })

  it('leaves the other items and the order as they were', async () => {
    await addOutboxItem(personId, testOutboxItem('a'))
    await addOutboxItem(personId, testOutboxItem('b'))
    await addOutboxItem(personId, testOutboxItem('c'))

    await updateOutboxItem(personId, 'b', { state: 'sending' })

    const items = await readOutbox(personId)
    expect(idsOf(items)).toEqual(['a', 'b', 'c'])
    expect(items.map((item) => item.state)).toEqual(['waiting', 'sending', 'waiting'])
  })

  it('can clear a failure by setting it back to null', async () => {
    await addOutboxItem(
      personId,
      testOutboxItem('a', { state: 'failed', failure: { errorCode: null, httpStatus: 400 } }),
    )

    await updateOutboxItem(personId, 'a', { state: 'waiting', failure: null })

    const [item] = await readOutbox(personId)
    expect(item.state).toBe('waiting')
    expect(item.failure).toBeNull()
  })

  it('does nothing and does not throw for an unknown id', async () => {
    await addOutboxItem(personId, testOutboxItem('a'))

    await expect(updateOutboxItem(personId, 'unknown', { state: 'failed' })).resolves.toBeUndefined()

    const items = await readOutbox(personId)
    expect(idsOf(items)).toEqual(['a'])
    expect(items[0].state).toBe('waiting')
  })

  it('does nothing for a person who has nothing queued', async () => {
    await expect(updateOutboxItem(personId, 'a', { state: 'failed' })).resolves.toBeUndefined()

    await expect(readOutbox(personId)).resolves.toEqual([])
  })

  it('does not bring back an item that was removed', async () => {
    await addOutboxItem(personId, testOutboxItem('a'))
    await removeOutboxItem(personId, 'a')

    await updateOutboxItem(personId, 'a', { state: 'sending' })

    await expect(readOutbox(personId)).resolves.toEqual([])
  })

  it('does not change an item of another person with the same id', async () => {
    await addOutboxItem(personId, testOutboxItem('same'))
    await addOutboxItem(otherPersonId, testOutboxItem('same'))

    await updateOutboxItem(personId, 'same', { state: 'failed' })

    const [theirs] = await readOutbox(otherPersonId)
    expect(theirs.state).toBe('waiting')
  })
})

describe('removeOutboxItem', () => {
  it('removes only the item with that id', async () => {
    await addOutboxItem(personId, testOutboxItem('a'))
    await addOutboxItem(personId, testOutboxItem('b'))
    await addOutboxItem(personId, testOutboxItem('c'))

    await removeOutboxItem(personId, 'b')

    expect(idsOf(await readOutbox(personId))).toEqual(['a', 'c'])
  })

  it('leaves an empty list after the last item is removed', async () => {
    await addOutboxItem(personId, testOutboxItem('a'))

    await removeOutboxItem(personId, 'a')

    await expect(readOutbox(personId)).resolves.toEqual([])
  })

  it('does nothing and does not throw for an unknown id', async () => {
    await addOutboxItem(personId, testOutboxItem('a'))

    await expect(removeOutboxItem(personId, 'unknown')).resolves.toBeUndefined()

    expect(idsOf(await readOutbox(personId))).toEqual(['a'])
  })

  it('does nothing and does not throw when the item was already removed', async () => {
    await addOutboxItem(personId, testOutboxItem('a'))
    await removeOutboxItem(personId, 'a')

    await expect(removeOutboxItem(personId, 'a')).resolves.toBeUndefined()
  })

  it('does nothing for a person who has nothing queued', async () => {
    await expect(removeOutboxItem(personId, 'a')).resolves.toBeUndefined()

    await expect(readOutbox(personId)).resolves.toEqual([])
  })

  it('does not remove an item of another person with the same id', async () => {
    await addOutboxItem(personId, testOutboxItem('same'))
    await addOutboxItem(otherPersonId, testOutboxItem('same'))

    await removeOutboxItem(personId, 'same')

    expect(storedIds(personId)).toEqual([])
    expect(storedIds(otherPersonId)).toEqual(['same'])
  })
})

describe('clearOutbox', () => {
  it('deletes every item of the person', async () => {
    await addOutboxItem(personId, testOutboxItem('a'))
    await addOutboxItem(personId, testOutboxItem('b'))

    await clearOutbox(personId)

    await expect(readOutbox(personId)).resolves.toEqual([])
    expect(storedOutbox.has(personId)).toBe(false)
  })

  it('leaves the items of another person alone', async () => {
    await addOutboxItem(personId, testOutboxItem('mine'))
    await addOutboxItem(otherPersonId, testOutboxItem('theirs'))

    await clearOutbox(personId)

    expect(storedIds(otherPersonId)).toEqual(['theirs'])
  })

  it('does nothing and does not throw for a person who has nothing queued', async () => {
    await expect(clearOutbox(personId)).resolves.toBeUndefined()
  })

  it('lets the person queue new items afterwards', async () => {
    await addOutboxItem(personId, testOutboxItem('a'))
    await clearOutbox(personId)

    await addOutboxItem(personId, testOutboxItem('b'))

    expect(idsOf(await readOutbox(personId))).toEqual(['b'])
  })
})
