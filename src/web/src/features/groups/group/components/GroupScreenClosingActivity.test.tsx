import { screen, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi, type MockInstance } from 'vitest'
import { languages } from '@/core/i18n/language'
import { routes } from '@/core/router/routes'
import { activityEventOf, type ActivityEventJson } from '@/test/activityTestData'
import { renderGroupApp } from '@/test/activityTestHelpers'
import { closingActivityTexts, textOf, type ClosingText } from '@/test/closingTestData'
import { showsGroupScreen } from '@/test/closingTestHelpers'
import { freezeTime, plainSpaces, unfreezeTime } from '@/test/expenseTestHelpers'
import { testGroup } from '@/test/groupTestData'
import { stubDeviceColorScheme } from '@/test/themeTestHelpers'
import { translated } from '@/test/translated'

const groupId = testGroup.id
const crashMessage = 'A screen crashed while rendering'

interface ClosingEventCase {
  name: string
  event: ActivityEventJson
  text: ClosingText
  params: Record<string, string>
}

const closingEventCases: ClosingEventCase[] = [
  {
    name: 'ClosingStarted',
    event: activityEventOf({ type: 'ClosingStarted' }),
    text: closingActivityTexts.closingStarted,
    params: { name: 'Ana' },
  },
  {
    name: 'ClosingConfirmed',
    event: activityEventOf({ type: 'ClosingConfirmed' }),
    text: closingActivityTexts.closingConfirmed,
    params: { name: 'Ana' },
  },
  {
    name: 'ClosingObjected without a reason',
    event: activityEventOf({ type: 'ClosingObjected', data: { reason: null } }),
    text: closingActivityTexts.closingObjected,
    params: { name: 'Ana' },
  },
  {
    name: 'ClosingObjected with a reason',
    event: activityEventOf({ type: 'ClosingObjected', data: { reason: 'Marko still owes me for the taxi' } }),
    text: closingActivityTexts.closingObjectedWithReason,
    params: { name: 'Ana', reason: 'Marko still owes me for the taxi' },
  },
  {
    name: 'ClosingCancelled',
    event: activityEventOf({ type: 'ClosingCancelled' }),
    text: closingActivityTexts.closingCancelled,
    params: { name: 'Ana' },
  },
  {
    name: 'GroupFinished',
    event: activityEventOf({ type: 'GroupFinished' }),
    text: closingActivityTexts.groupFinished,
    params: { name: 'Ana' },
  },
  {
    name: 'GroupReopened',
    event: activityEventOf({ type: 'GroupReopened' }),
    text: closingActivityTexts.groupReopened,
    params: { name: 'Ana' },
  },
]

function crashErrors(consoleError: MockInstance<typeof console.error>): Error[] {
  return consoleError.mock.calls
    .filter(([message]) => message === crashMessage)
    .map(([, error]) => error)
    .filter((error): error is Error => error instanceof Error)
}

describe('the closing events on the Activity tab', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    freezeTime()
    stubDeviceColorScheme('light')
  })

  afterEach(() => {
    unfreezeTime()
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  describe.each(languages)('in %s', (language) => {
    it.each(closingEventCases)('writes the event $name as one sentence', async ({ event, text, params }) => {
      await renderGroupApp(routes.groupActivity(groupId), { language, events: [event] })
      await showsGroupScreen()
      const sentence = textOf(language, text, params)

      expect(await screen.findByText(plainSpaces(sentence), { normalizer: plainSpaces })).toBeTruthy()
    })

    it('shows four closing events in the order of the answer', async () => {
      const events = [
        activityEventOf({ type: 'GroupReopened' }),
        activityEventOf({ type: 'GroupFinished' }),
        activityEventOf({ type: 'ClosingConfirmed' }),
        activityEventOf({ type: 'ClosingStarted' }),
      ]
      await renderGroupApp(routes.groupActivity(groupId), { language, events })
      await showsGroupScreen()

      const sentences = [
        closingActivityTexts.groupReopened,
        closingActivityTexts.groupFinished,
        closingActivityTexts.closingConfirmed,
        closingActivityTexts.closingStarted,
      ].map((text) => plainSpaces(textOf(language, text, { name: 'Ana' })))
      const shown = await Promise.all(
        sentences.map((sentence) => screen.findByText(sentence, { normalizer: plainSpaces })),
      )

      for (let index = 1; index < shown.length; index += 1) {
        expect(shown[index - 1].compareDocumentPosition(shown[index]) & Node.DOCUMENT_POSITION_FOLLOWING).not.toBe(0)
      }
    })
  })

  it('writes the reason as plain text, not as markup', async () => {
    const reason = 'Marko <b>owes</b> me & Ana'
    await renderGroupApp(routes.groupActivity(groupId), {
      events: [activityEventOf({ type: 'ClosingObjected', data: { reason } })],
    })
    await showsGroupScreen()

    const sentence = textOf('en', closingActivityTexts.closingObjectedWithReason, { name: 'Ana', reason })

    expect(await screen.findByText(plainSpaces(sentence), { normalizer: plainSpaces })).toBeTruthy()
    expect(document.querySelector('b')).toBeNull()
  })

  it('writes the sentence of a finished group without the name of the person in the event', async () => {
    await renderGroupApp(routes.groupActivity(groupId), {
      events: [activityEventOf({ type: 'GroupFinished', actorName: 'Ana' })],
    })
    await showsGroupScreen()

    const sentence = await screen.findByText(translated('en', closingActivityTexts.groupFinished.key))

    expect(sentence.textContent).not.toContain('Ana')
  })

  it('shows the closing events without a link to an expense, because they are not about one expense', async () => {
    await renderGroupApp(routes.groupActivity(groupId), {
      events: [activityEventOf({ type: 'ClosingStarted' })],
    })
    await showsGroupScreen()

    const sentence = await screen.findByText(plainSpaces(textOf('en', closingActivityTexts.closingStarted, { name: 'Ana' })), {
      normalizer: plainSpaces,
    })

    expect(sentence.closest('a')).toBeNull()
  })

  it('still stops with an error that names the event type when the type is one Kvit has no sentence for', async () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {})
    await renderGroupApp(routes.groupActivity(groupId), {
      events: [
        activityEventOf({ type: 'ClosingStarted' }),
        activityEventOf({ type: 'ClosingSomethingNew' }),
      ],
    })

    expect((await screen.findByRole('alert')).textContent).toContain(translated('en', 'errors.generic'))
    await waitFor(() => {
      expect(crashErrors(consoleError).some((error) => error.message.includes('ClosingSomethingNew'))).toBe(true)
    })
    expect(screen.queryByText(textOf('en', closingActivityTexts.closingStarted, { name: 'Ana' }))).toBeNull()
  })
})
