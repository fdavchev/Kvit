import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { toast } from 'sonner'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { languages, type Language } from '@/core/i18n/language'
import { legalVersion } from '@/core/legal/legalInfo'
import type { Me } from '@/core/services/me/meService'
import { renderGroupApp } from '@/test/activityTestHelpers'
import { dismissSheetByTappingBackdrop } from '@/test/expenseFormTestHelpers'
import { expensesPath } from '@/test/expenseTestHelpers'
import { stubGoogleSignIn } from '@/test/googleTestHelpers'
import { testGroupId } from '@/test/groupTestData'
import { testOutboxItem } from '@/test/outboxTestData'
import { seedOutbox } from '@/test/outboxTestHelpers'
import { installFakeResizeObservers } from '@/test/resizeObserverTestHelpers'
import {
  jsonAnswer,
  neverAnswer,
  networkFailureAnswer,
  problemAnswer,
  requestCount,
  type AnswerFactory,
} from '@/test/requestTestHelpers'
import { seedMe, testGuestMe, testMe, testMeWithMaterialTerms, testMeWithOldTerms } from '@/test/testMe'
import { stubDeviceColorScheme } from '@/test/themeTestHelpers'
import { rectOfHeight } from '@/test/toastLiftTestHelpers'
import { translated } from '@/test/translated'
import { routes } from './routes'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const acceptPath = '/api/me/terms/accept'
const acceptRequest = `POST ${acceptPath}`
const meRequest = 'GET /api/me'
const millisecondsToWatchForAClose = 50
const termsFields = ['termsVersion', 'termsUpToDate', 'termsUpdateIsMaterial']

const waitingExpense = testOutboxItem('waiting-expense', {
  kind: 'expense',
  groupId: testGroupId,
  payload: {
    clientRequestId: 'waiting-expense',
    title: 'Hotel',
    note: null,
    amountMinor: 300000,
    currency: 'MKD',
    expenseDate: '2026-10-19',
    categoryId: null,
    paidByMemberId: 'member-1',
    splitType: 'Equal',
    shares: [{ memberId: 'member-1', inputValue: 1 }],
  },
})

interface RenderOptions {
  language?: Language
  acceptAnswer?: AnswerFactory
}

function answersFor(me: Me, acceptAnswer?: AnswerFactory): Record<string, AnswerFactory> {
  let hasAccepted = false
  const afterAccepting: Me = { ...me, termsVersion: legalVersion, termsUpToDate: true, termsUpdateIsMaterial: false }
  return {
    [meRequest]: () => Response.json(hasAccepted ? afterAccepting : me),
    [acceptRequest]:
      acceptAnswer ??
      (() => {
        hasAccepted = true
        return new Response(null, { status: 204 })
      }),
  }
}

async function renderAs(me: Me, path: string = routes.settings, options: RenderOptions = {}) {
  const language = options.language ?? 'en'
  const person: Me = { ...me, language }
  return renderGroupApp(path, {
    language,
    answers: answersFor(person, options.acceptAnswer),
    seedCache: seedMe(person),
  })
}

function pill(language: Language = 'en'): HTMLElement | null {
  const text = screen.queryByText(translated(language, 'legal.termsUpdated'))
  return text === null ? null : text.closest<HTMLElement>('[role="status"]')
}

async function findPill(language: Language = 'en'): Promise<HTMLElement> {
  const text = await screen.findByText(translated(language, 'legal.termsUpdated'))
  const region = text.closest<HTMLElement>('[role="status"]')
  if (region === null) {
    throw new Error('The terms-changed text is not inside an element with the role status')
  }
  return region
}

function findMaterialSheet(language: Language = 'en'): Promise<HTMLElement> {
  return screen.findByRole('dialog', { name: translated(language, 'legal.termsMaterial.title') })
}

function sheetControl(sheet: HTMLElement, key: string, language: Language = 'en'): HTMLElement {
  const control = within(sheet).getByText(translated(language, key)).closest<HTMLElement>('a, button')
  if (control === null) {
    throw new Error(`The text of ${key} is not inside a link or a button`)
  }
  return control
}

function termsPillSpaceOnPage(): string {
  return document.documentElement.style.getPropertyValue('--terms-pill-space')
}

function isBefore(first: Node, second: Node): boolean {
  return (first.compareDocumentPosition(second) & Node.DOCUMENT_POSITION_FOLLOWING) !== 0
}

function watchForAClose(): Promise<void> {
  return new Promise((resolve) => {
    setTimeout(resolve, millisecondsToWatchForAClose)
  })
}

function tapOk(notice: HTMLElement): void {
  fireEvent.click(within(notice).getByRole('button', { name: translated('en', 'legal.termsOk') }))
}

function tapAgree(sheet: HTMLElement): void {
  fireEvent.click(within(sheet).getByRole('button', { name: translated('en', 'legal.termsMaterial.agree') }))
}

async function settingsHasOpened(): Promise<void> {
  await screen.findByRole('heading', { level: 1, name: translated('en', 'settings.title') })
}

const failedAccepts: [string, AnswerFactory, string][] = [
  ['the network fails', networkFailureAnswer(), 'errors.network'],
  ['the server refuses', problemAnswer(429, 'RATE_LIMITED'), 'errors.RATE_LIMITED'],
]

const readablePages: [string, string, string][] = [
  ['the Terms page', routes.terms, 'terms.title'],
  ['the Privacy page', routes.privacy, 'privacy.title'],
]

describe('the terms-changed notice, a thin pill above the tab bar', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    stubDeviceColorScheme('light')
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it.each(languages)('shows the text, a Read link and an OK button in a status region to a person whose terms are out of date (%s)', async (language) => {
    await renderAs(testMeWithOldTerms, routes.settings, { language })

    const notice = await findPill(language)

    expect(within(notice).getByRole('link', { name: translated(language, 'legal.termsRead') })).toBeTruthy()
    expect(within(notice).getByRole('button', { name: translated(language, 'legal.termsOk') })).toBeTruthy()
  })

  it('shows the notice on the other tab screens as well', async () => {
    await renderAs(testMeWithOldTerms, routes.groups)

    expect(await findPill()).toBeTruthy()
  })

  it('shows the notice to a guest whose terms are out of date', async () => {
    await renderAs({ ...testGuestMe, termsVersion: null, termsUpToDate: false })

    expect(await findPill()).toBeTruthy()
  })

  it('has a Read link that points to the Terms', async () => {
    await renderAs(testMeWithOldTerms)
    const notice = await findPill()

    const read = within(notice).getByRole('link', { name: translated('en', 'legal.termsRead') })

    expect(read.getAttribute('href')).toBe(routes.terms)
  })

  it('opens the Terms from the Read link and remembers the screen it was on for Back', async () => {
    const { router } = await renderAs(testMeWithOldTerms)
    const notice = await findPill()

    fireEvent.click(within(notice).getByRole('link', { name: translated('en', 'legal.termsRead') }))

    expect(router.state.location.pathname).toBe(routes.terms)
    expect(router.state.location.state).toEqual({ from: routes.settings })
  })

  it('does not ask the server anything until OK is tapped', async () => {
    const { fetchMock } = await renderAs(testMeWithOldTerms)
    await findPill()

    expect(requestCount(fetchMock, 'POST', acceptPath)).toBe(0)
  })

  it('sends one accept request when OK is tapped and then takes the notice away', async () => {
    const { fetchMock } = await renderAs(testMeWithOldTerms)
    const notice = await findPill()

    tapOk(notice)

    await waitFor(() => {
      expect(pill()).toBeNull()
    })
    expect(requestCount(fetchMock, 'POST', acceptPath)).toBe(1)
  })

  it('stays on the same page after OK', async () => {
    await renderAs(testMeWithOldTerms)
    const notice = await findPill()

    tapOk(notice)

    await waitFor(() => {
      expect(pill()).toBeNull()
    })
    expect(screen.getByRole('heading', { level: 1, name: translated('en', 'settings.title') })).toBeTruthy()
  })

  it('makes room on the page for its own height and a 10px gap while it shows, and gives the room back after OK', async () => {
    const observers = installFakeResizeObservers()
    await renderAs(testMeWithOldTerms)
    const notice = await findPill()

    notice.getBoundingClientRect = () => rectOfHeight(85)
    observers.fireFor(notice)

    expect(termsPillSpaceOnPage()).toBe('95px')

    tapOk(notice)

    await waitFor(() => {
      expect(pill()).toBeNull()
    })
    expect(termsPillSpaceOnPage()).toBe('')
  })

  it('gives the OK button a tap target at least 44px high', async () => {
    await renderAs(testMeWithOldTerms)
    const notice = await findPill()

    const ok = within(notice).getByRole('button', { name: translated('en', 'legal.termsOk') })

    expect(ok.className.split(/\s+/)).toContain('min-h-11')
  })

  it('makes no room for the pill for a person whose terms are up to date', async () => {
    await renderAs(testMe)
    await settingsHasOpened()

    expect(termsPillSpaceOnPage()).toBe('')
  })

  it.each(failedAccepts)('shows the error toast and keeps the notice when OK is tapped and %s', async (_name, acceptAnswer, textKey) => {
    await renderAs(testMeWithOldTerms, routes.settings, { acceptAnswer })
    const notice = await findPill()

    tapOk(notice)

    await waitFor(() => {
      expect(toast.error).toHaveBeenCalledWith(translated('en', textKey))
    })
    expect(pill()).toBeTruthy()
  })

  it('shows nothing to a person whose terms are up to date', async () => {
    await renderAs(testMe)
    await settingsHasOpened()

    expect(pill()).toBeNull()
    expect(screen.queryByRole('dialog')).toBeNull()
  })

  it('shows nothing when the saved me has no terms fields, because nobody has said yet that the terms changed', async () => {
    const olderMe = Object.fromEntries(Object.entries(testMe).filter(([name]) => !termsFields.includes(name)))
    await renderGroupApp(routes.settings, { answers: { [meRequest]: jsonAnswer(olderMe) } })
    await settingsHasOpened()

    expect(pill()).toBeNull()
    expect(screen.queryByRole('dialog')).toBeNull()
  })

  it('shows no sheet next to the notice', async () => {
    await renderAs(testMeWithOldTerms)
    await findPill()

    expect(screen.queryByRole('dialog')).toBeNull()
  })

  it('is not shown to a visitor who is not signed in', async () => {
    stubGoogleSignIn()
    await renderGroupApp(routes.welcome, {
      answers: {
        [meRequest]: problemAnswer(401, 'AUTH_NOT_SIGNED_IN'),
        'GET /api/health': () => new Response('Healthy'),
      },
    })
    await screen.findByText(translated('en', 'welcome.tagline'))

    expect(pill()).toBeNull()
    expect(screen.queryByRole('dialog')).toBeNull()
  })

  it.each(readablePages)('is not shown on %s, even to a signed-in person whose terms are out of date', async (_name, path, titleKey) => {
    await renderAs(testMeWithOldTerms, path)
    await screen.findByRole('heading', { level: 1, name: translated('en', titleKey) })

    expect(pill()).toBeNull()
  })

  it('sits before the outbox bar in the page when both show, so the two do not cover each other', async () => {
    seedOutbox(testMe.id, [waitingExpense])
    await renderGroupApp(routes.settings, {
      answers: { ...answersFor(testMeWithOldTerms), [`POST ${expensesPath}`]: neverAnswer() },
      seedCache: seedMe(testMeWithOldTerms),
    })
    const notice = await findPill()
    await waitFor(() => {
      expect(document.querySelector('[data-outbox-bar]')).not.toBeNull()
    })

    expect(isBefore(notice, document.querySelector('[data-outbox-bar]') as HTMLElement)).toBe(true)
  })
})

describe('the terms-changed sheet, for a change that matters', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    stubDeviceColorScheme('light')
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it.each(languages)('opens a sheet with the title, the body, a Read the Terms button and an I agree button (%s)', async (language) => {
    await renderAs(testMeWithMaterialTerms, routes.settings, { language })

    const sheet = await findMaterialSheet(language)

    expect(within(sheet).getByText(translated(language, 'legal.termsMaterial.body'))).toBeTruthy()
    expect(sheetControl(sheet, 'legal.termsMaterial.read', language)).toBeTruthy()
    expect(within(sheet).getByRole('button', { name: translated(language, 'legal.termsMaterial.agree') })).toBeTruthy()
  })

  it('shows no thin notice next to the sheet', async () => {
    await renderAs(testMeWithMaterialTerms)
    await findMaterialSheet()

    expect(pill()).toBeNull()
  })

  it('opens the sheet for a guest as well', async () => {
    await renderAs({ ...testGuestMe, termsVersion: null, termsUpToDate: false, termsUpdateIsMaterial: true })

    expect(await findMaterialSheet()).toBeTruthy()
  })

  it('opens the Terms from the Read the Terms button and remembers the screen it was on for Back', async () => {
    const { router } = await renderAs(testMeWithMaterialTerms)
    const sheet = await findMaterialSheet()

    fireEvent.click(sheetControl(sheet, 'legal.termsMaterial.read'))

    await waitFor(() => {
      expect(router.state.location.pathname).toBe(routes.terms)
    })
    expect(router.state.location.state).toEqual({ from: routes.settings })
  })

  it('does not ask the server anything until I agree is tapped', async () => {
    const { fetchMock } = await renderAs(testMeWithMaterialTerms)
    await findMaterialSheet()

    expect(requestCount(fetchMock, 'POST', acceptPath)).toBe(0)
  })

  it('sends one accept request when I agree is tapped and then closes the sheet', async () => {
    const { fetchMock } = await renderAs(testMeWithMaterialTerms)
    const sheet = await findMaterialSheet()

    tapAgree(sheet)

    await waitFor(() => {
      expect(screen.queryByRole('dialog')).toBeNull()
    })
    expect(requestCount(fetchMock, 'POST', acceptPath)).toBe(1)
  })

  it.each(failedAccepts)('shows the error toast and keeps the sheet open when I agree is tapped and %s', async (_name, acceptAnswer, textKey) => {
    await renderAs(testMeWithMaterialTerms, routes.settings, { acceptAnswer })
    const sheet = await findMaterialSheet()

    tapAgree(sheet)

    await waitFor(() => {
      expect(toast.error).toHaveBeenCalledWith(translated('en', textKey))
    })
    expect(await findMaterialSheet()).toBeTruthy()
  })

  it('stays open when Escape is pressed', async () => {
    await renderAs(testMeWithMaterialTerms)
    const sheet = await findMaterialSheet()

    fireEvent.keyDown(sheet, { key: 'Escape' })
    await watchForAClose()

    expect(await findMaterialSheet()).toBeTruthy()
  })

  it('stays open when the backdrop is tapped', async () => {
    await renderAs(testMeWithMaterialTerms)
    await findMaterialSheet()

    dismissSheetByTappingBackdrop()
    await watchForAClose()

    expect(await findMaterialSheet()).toBeTruthy()
  })

  it.each(readablePages)('does not cover %s, so the person can read it before agreeing', async (_name, path, titleKey) => {
    await renderAs(testMeWithMaterialTerms, path)
    await screen.findByRole('heading', { level: 1, name: translated('en', titleKey) })

    expect(screen.queryByRole('dialog')).toBeNull()
  })

  it('is not shown to a person whose terms are up to date', async () => {
    await renderAs({ ...testMe, termsUpdateIsMaterial: true })
    await settingsHasOpened()

    expect(screen.queryByRole('dialog')).toBeNull()
  })
})
