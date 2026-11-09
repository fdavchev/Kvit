import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { languages } from '@/core/i18n/language'
import type { OwnerToolsLogEntry } from '@/core/services/ownerTools/ownerToolsService'
import { stubGoogleSignIn } from '@/test/googleTestHelpers'
import {
  logEntries,
  logEntry,
  olderLogEntry,
  oldestLogEntry,
  ownerToolsNow,
} from '@/test/ownerToolsTestData'
import {
  answerWith,
  findToolsTitle,
  openTab,
  renderOwnerTools,
  resultsList,
  toolsUrls,
} from '@/test/ownerToolsTestHelpers'
import { neverAnswer, networkFailureAnswer, problemAnswer } from '@/test/requestTestHelpers'
import { loadingLook } from '@/test/skeletonTestHelpers'
import { stubDeviceColorScheme } from '@/test/themeTestHelpers'
import { translated } from '@/test/translated'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const logGet = 'GET /api/admin/tools/log'
const actions = ['Suspended', 'Unsuspended', 'GroupClosed', 'GroupReopened', 'NameErased'] as const

async function showLog(entries: unknown = logEntries, language: 'en' | 'mk' = 'en') {
  const rendered = await renderOwnerTools({ language, answers: { [logGet]: answerWith(entries) } })
  await findToolsTitle(language)
  openTab('log', language)
  return rendered
}

function rowsOfLog(): HTMLElement[] {
  return within(resultsList() as HTMLElement).getAllByRole('listitem')
}

function lineText(entry: OwnerToolsLogEntry, language: 'en' | 'mk' = 'en'): string {
  return translated(language, 'ownerTools.log.line', {
    admin: entry.adminName,
    action: translated(language, `ownerTools.log.action.${entry.action}`),
    target: entry.targetLabel,
  })
}

describe('the log of the owner tools', () => {
  beforeEach(() => {
    window.localStorage.clear()
    vi.clearAllMocks()
    vi.useFakeTimers({ toFake: ['Date'] })
    vi.setSystemTime(ownerToolsNow)
    stubDeviceColorScheme('light')
    stubGoogleSignIn()
  })

  afterEach(() => {
    vi.useRealTimers()
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  describe('what is asked', () => {
    it('asks the server for the log when the Log tab is opened, once', async () => {
      const { fetchMock } = await showLog()
      await screen.findByText(logEntry.reason)

      expect(toolsUrls(fetchMock, 'GET')).toEqual(['/api/admin/tools/log?limit=50'])
    })

    it('does not ask for the log while another tab is open', async () => {
      const { fetchMock } = await renderOwnerTools({ answers: { [logGet]: answerWith(logEntries) } })
      await findToolsTitle()

      expect(toolsUrls(fetchMock, 'GET')).toEqual([])
    })
  })

  describe('the rows', () => {
    it('shows one row for each entry, in the order of the answer, newest first', async () => {
      await showLog()
      await screen.findByText(logEntry.reason)

      const rows = rowsOfLog()

      expect(rows).toHaveLength(3)
      expect(rows[0].textContent).toContain(logEntry.reason)
      expect(rows[1].textContent).toContain(olderLogEntry.reason)
      expect(rows[2].textContent).toContain(oldestLogEntry.reason)
    })

    it('shows who did it, what, to whom, and the reason in each row', async () => {
      await showLog()
      await screen.findByText(logEntry.reason)

      const [first, second, third] = rowsOfLog()

      expect(within(first).getByText(lineText(logEntry))).toBeTruthy()
      expect(within(first).getByText(logEntry.reason)).toBeTruthy()
      expect(within(second).getByText(lineText(olderLogEntry))).toBeTruthy()
      expect(within(second).getByText(olderLogEntry.reason)).toBeTruthy()
      expect(within(third).getByText(lineText(oldestLogEntry))).toBeTruthy()
      expect(within(third).getByText(oldestLogEntry.reason)).toBeTruthy()
    })

    it.each(actions)('writes the action %s with its own text', async (action) => {
      const entry: OwnerToolsLogEntry = { ...logEntry, action }
      await showLog([entry])

      expect(await screen.findByText(lineText(entry))).toBeTruthy()
    })

    it.each(languages)('writes the line of a row in the language of the person (%s)', async (language) => {
      await showLog([logEntry], language)

      expect(await screen.findByText(lineText(logEntry, language))).toBeTruthy()
    })

    it('shows how long ago a recent entry was done, with the date helpers of the app', async () => {
      await showLog([logEntry])
      await screen.findByText(logEntry.reason)

      expect(
        within(rowsOfLog()[0]).getByText(translated('en', 'time.hours_other', { count: '2' })),
      ).toBeTruthy()
    })

    it('shows the day of an entry that is older than a week', async () => {
      await showLog([olderLogEntry])
      await screen.findByText(olderLogEntry.reason)

      expect(within(rowsOfLog()[0]).getByText('20 Oct')).toBeTruthy()
    })

    it('shows the day of an old entry in Macedonian', async () => {
      await showLog([olderLogEntry], 'mk')
      await screen.findByText(olderLogEntry.reason)

      expect(within(rowsOfLog()[0]).getByText('20 окт')).toBeTruthy()
    })

    it('shows a reason with markup characters as plain text', async () => {
      const reason = 'Wrote <b>spam</b> & more'
      await showLog([{ ...logEntry, reason }])

      expect(await screen.findByText(reason)).toBeTruthy()
    })
  })

  describe('when there is nothing yet', () => {
    it('shows Nothing done yet. and no row', async () => {
      await showLog([])

      expect(await screen.findByText(translated('en', 'ownerTools.log.empty'))).toBeTruthy()
      expect(resultsList()).toBeNull()
    })

    it.each(languages)('writes that text in the language of the person (%s)', async (language) => {
      await showLog([], language)

      expect(await screen.findByText(translated(language, 'ownerTools.log.empty'))).toBeTruthy()
    })

    it('does not show that text when there are entries', async () => {
      await showLog()
      await screen.findByText(logEntry.reason)

      expect(screen.queryByText(translated('en', 'ownerTools.log.empty'))).toBeNull()
    })
  })

  describe('while the answer is on its way and when it fails', () => {
    it('shows placeholder blocks in a loading status, not a spinner, and no row', async () => {
      await renderOwnerTools({ answers: { [logGet]: neverAnswer() } })
      await findToolsTitle()

      openTab('log')

      expect(await screen.findByRole('status')).toBeTruthy()
      const look = loadingLook()
      expect(look.skeletonCount).toBeGreaterThan(0)
      expect(look.hasSpinner).toBe(false)
      expect(resultsList()).toBeNull()
    })

    it('takes the placeholders away when the answer arrives', async () => {
      await showLog()

      await screen.findByText(logEntry.reason)

      expect(screen.queryByRole('status')).toBeNull()
      expect(document.querySelector('[data-skeleton]')).toBeNull()
    })

    it.each([
      ['a lost connection', networkFailureAnswer(), 'errors.network'],
      ['a refusal with a code', problemAnswer(429, 'RATE_LIMITED'), 'errors.RATE_LIMITED'],
    ])('shows the translated message in the standard error with Try again after %s, and no row', async (_name, answer, textKey) => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      await renderOwnerTools({ answers: { [logGet]: answer } })
      await findToolsTitle()
      openTab('log')

      const alert = await screen.findByRole('alert')

      expect(within(alert).getByText(translated('en', textKey))).toBeTruthy()
      expect(within(alert).getByRole('button', { name: translated('en', 'common.retry') })).toBeTruthy()
      expect(resultsList()).toBeNull()
    })

    it('asks again and shows the rows when Try again is pressed', async () => {
      vi.spyOn(console, 'error').mockImplementation(() => {})
      let calls = 0
      const { fetchMock } = await renderOwnerTools({
        answers: {
          [logGet]: () => {
            calls += 1
            return calls === 1 ? new TypeError('Failed to fetch') : Response.json(logEntries)
          },
        },
      })
      await findToolsTitle()
      openTab('log')
      const alert = await screen.findByRole('alert')

      fireEvent.click(within(alert).getByRole('button', { name: translated('en', 'common.retry') }))

      expect(await screen.findByText(logEntry.reason)).toBeTruthy()
      expect(screen.queryByRole('alert')).toBeNull()
      await waitFor(() => {
        expect(toolsUrls(fetchMock, 'GET')).toHaveLength(2)
      })
    })
  })
})
