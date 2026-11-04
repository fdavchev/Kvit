import { screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { languages, type Language } from '@/core/i18n/language'
import { OutboxProvider } from '@/core/outbox/OutboxProvider'
import type { OutboxItem } from '@/core/outbox/outboxItem'
import { createFakeOutbox } from '@/test/fakeOutboxSender'
import {
  otherGroupId,
  queuedExpense,
  queuedOneBill,
  queuedPersonalEntry,
  queuedSettlement,
} from '@/test/outboxScreenTestData'
import { outboxText } from '@/test/outboxTextsTestData'
import { renderElementWithProviders } from '@/test/renderWithProviders'
import { seedMe, testMe } from '@/test/testMe'
import { testGroupId } from '@/test/groupTestData'
import { ChangesWaitingNote } from './ChangesWaitingNote'

type Scope = Parameters<typeof ChangesWaitingNote>[0]['scope']

const groupScope: Scope = { kind: 'group', groupId: testGroupId }
const personalScope: Scope = { kind: 'personal' }
const homeScope: Scope = { kind: 'home' }

async function renderNote(scope: Scope, items: readonly OutboxItem[], language: Language = 'en') {
  const outbox = createFakeOutbox(items)
  await renderElementWithProviders(
    <OutboxProvider sender={outbox.sender}>
      <ChangesWaitingNote scope={scope} />
    </OutboxProvider>,
    '/note',
    { language, seedCache: seedMe({ ...testMe, language }) },
  )
  return outbox
}

function note(): HTMLElement | null {
  return screen.queryByText(/not counted yet|не се брои|не се бројат/)
}

function noteText(): string | undefined {
  return note()?.textContent ?? undefined
}

describe('ChangesWaitingNote', () => {
  describe('in general', () => {
    it('renders nothing when no change is waiting', async () => {
      await renderNote(groupScope, [])

      expect(note()).toBeNull()
    })

    it('says "1 change waiting, not counted yet" for one change', async () => {
      await renderNote(groupScope, [queuedExpense('a')])

      expect(noteText()).toBe('1 change waiting, not counted yet')
    })

    it('says "2 changes waiting, not counted yet" for two changes', async () => {
      await renderNote(groupScope, [queuedExpense('a'), queuedSettlement('b')])

      expect(noteText()).toBe('2 changes waiting, not counted yet')
    })

    it.each(languages)('writes the note in the language of the app (%s)', async (language) => {
      await renderNote(groupScope, [queuedExpense('a'), queuedExpense('b'), queuedExpense('c')], language)

      expect(noteText()).toBe(outboxText(language, 'outbox.notCounted', 3))
    })

    it('uses the Macedonian singular for 21 changes', async () => {
      const items = Array.from({ length: 21 }, (_value, index) => queuedExpense(`item-${index}`))

      await renderNote(groupScope, items, 'mk')

      expect(noteText()).toBe('21 промена чека, сѐ уште не се брои')
    })

    it('is a paragraph', async () => {
      await renderNote(groupScope, [queuedExpense('a')])

      expect(note()?.tagName).toBe('P')
    })

    it('counts a change that is being sent as waiting', async () => {
      await renderNote(groupScope, [queuedExpense('a', { state: 'sending' }), queuedExpense('b')])

      expect(noteText()).toBe('2 changes waiting, not counted yet')
    })

    it('does not count a change that failed, because it will not be counted until it is retried', async () => {
      await renderNote(groupScope, [queuedExpense('a', { state: 'failed' }), queuedExpense('b')])

      expect(noteText()).toBe('1 change waiting, not counted yet')
    })

    it('renders nothing when the only changes failed', async () => {
      await renderNote(homeScope, [queuedExpense('a', { state: 'failed' }), queuedOneBill('b', { state: 'failed' })])

      expect(note()).toBeNull()
    })

    it('follows the sender: it appears when a change is queued and goes away when it is gone', async () => {
      const outbox = await renderNote(groupScope, [])

      outbox.show([queuedExpense('a')])
      expect(noteText()).toBe('1 change waiting, not counted yet')

      outbox.show([])
      expect(note()).toBeNull()
    })
  })

  describe('for a group', () => {
    it('counts the expenses and the payments of that group', async () => {
      await renderNote(groupScope, [queuedExpense('a'), queuedSettlement('b'), queuedExpense('c')])

      expect(noteText()).toBe('3 changes waiting, not counted yet')
    })

    it('does not count the expenses and payments of another group', async () => {
      await renderNote(groupScope, [
        queuedExpense('mine'),
        queuedExpense('theirs', { groupId: otherGroupId }),
        queuedSettlement('their-payment', { groupId: otherGroupId }),
      ])

      expect(noteText()).toBe('1 change waiting, not counted yet')
    })

    it('does not count personal entries or One bills', async () => {
      await renderNote(groupScope, [queuedPersonalEntry('entry'), queuedOneBill('bill')])

      expect(note()).toBeNull()
    })

    it('renders nothing when only another group has waiting changes', async () => {
      await renderNote(groupScope, [queuedExpense('theirs', { groupId: otherGroupId })])

      expect(note()).toBeNull()
    })
  })

  describe('for the personal budget', () => {
    it('counts the personal entries', async () => {
      await renderNote(personalScope, [queuedPersonalEntry('a'), queuedPersonalEntry('b')])

      expect(noteText()).toBe('2 changes waiting, not counted yet')
    })

    it('does not count expenses, payments or One bills', async () => {
      await renderNote(personalScope, [queuedExpense('a'), queuedSettlement('b'), queuedOneBill('c')])

      expect(note()).toBeNull()
    })
  })

  describe('for the home screen', () => {
    it('counts the expenses, payments and One bills of every group', async () => {
      await renderNote(homeScope, [
        queuedExpense('a'),
        queuedExpense('b', { groupId: otherGroupId }),
        queuedSettlement('c'),
        queuedOneBill('d'),
      ])

      expect(noteText()).toBe('4 changes waiting, not counted yet')
    })

    it('does not count personal entries, which do not change the totals of the home screen', async () => {
      await renderNote(homeScope, [queuedPersonalEntry('a'), queuedExpense('b')])

      expect(noteText()).toBe('1 change waiting, not counted yet')
    })
  })
})
