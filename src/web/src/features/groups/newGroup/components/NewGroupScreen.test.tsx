import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { languages, type Language } from '@/core/i18n/language'
import { routes } from '@/core/router/routes'
import { problemResponse, stubFetchThatNeverAnswers } from '@/test/apiTestHelpers'
import { expectDisabledWhilePending, fieldLabelled, typeInto } from '@/test/formTestHelpers'
import { defaultGroupEmoji, groupEmojis, testGroup } from '@/test/groupTestData'
import { renderRoutesWithProviders } from '@/test/renderWithProviders'
import {
  jsonAnswer,
  networkFailureAnswer,
  problemAnswer,
  requestCount,
  requestsOf,
  stubFetchByRequest,
} from '@/test/requestTestHelpers'
import { seedMe, testMe } from '@/test/testMe'
import { translated } from '@/test/translated'
import { NewGroupScreen } from './NewGroupScreen'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const groupName = 'Greece trip'

async function renderNewGroup(language: Language = 'en') {
  return renderRoutesWithProviders(
    [
      { path: routes.newGroup, element: <NewGroupScreen /> },
      { path: '/groups/:groupId', element: <p>group page</p> },
      { path: routes.groups, element: <p>groups page</p> },
    ],
    routes.newGroup,
    { language, seedCache: seedMe({ ...testMe, language }) },
  )
}

function nameField(language: Language = 'en'): HTMLInputElement {
  return fieldLabelled(translated(language, 'groupFields.name'))
}

function emojiChip(emoji: string): HTMLElement {
  return screen.getByRole('button', { name: emoji })
}

function currencyChip(currency: string): HTMLElement {
  return screen.getByRole('button', { name: currency })
}

function createButton(language: Language = 'en'): HTMLElement {
  return screen.getByRole('button', { name: translated(language, 'newGroup.create') })
}

function pressCreate(language: Language = 'en'): void {
  fireEvent.click(createButton(language))
}

describe('NewGroupScreen', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it.each(languages)('shows the New group heading (%s)', async (language) => {
    await renderNewGroup(language)

    expect(
      screen.getByRole('heading', { name: translated(language, 'newGroup.title') }),
    ).toBeTruthy()
  })

  it('has a back button to the groups list', async () => {
    await renderNewGroup()

    const back = screen.getByRole('link', { name: translated('en', 'common.back') })

    expect(back.getAttribute('href')).toBe(routes.groups)
  })

  it.each(languages)('has an empty name field with the translated placeholder (%s)', async (language) => {
    await renderNewGroup(language)

    expect(nameField(language).value).toBe('')
    expect(nameField(language).placeholder).toBe(translated(language, 'groupFields.namePlaceholder'))
  })

  it('offers the six emojis with the beach pressed first', async () => {
    await renderNewGroup()

    const emojiGroup = screen.getByRole('group', { name: translated('en', 'groupFields.emoji') })
    const chips = within(emojiGroup).getAllByRole('button')

    expect(chips.map((chip) => chip.textContent)).toEqual(groupEmojis)
    expect(chips.map((chip) => chip.getAttribute('aria-pressed'))).toEqual([
      'true',
      'false',
      'false',
      'false',
      'false',
      'false',
    ])
  })

  it('offers MKD and EUR with MKD pressed first', async () => {
    await renderNewGroup()

    expect(currencyChip('MKD').getAttribute('aria-pressed')).toBe('true')
    expect(currencyChip('EUR').getAttribute('aria-pressed')).toBe('false')
  })

  it.each(languages)('tells which currency new expenses use (%s)', async (language) => {
    await renderNewGroup(language)

    expect(screen.getByText(translated(language, 'groupFields.currencyHint'))).toBeTruthy()
  })

  it.each(languages)('has the Create group button (%s)', async (language) => {
    await renderNewGroup(language)

    expect(createButton(language)).toBeTruthy()
  })

  it('shows no error before anything is sent', async () => {
    await renderNewGroup()

    expect(screen.queryByRole('alert')).toBeNull()
  })

  it('moves the pressed mark to the emoji that is chosen', async () => {
    await renderNewGroup()

    fireEvent.click(emojiChip(groupEmojis[5]))

    expect(emojiChip(groupEmojis[5]).getAttribute('aria-pressed')).toBe('true')
    expect(emojiChip(defaultGroupEmoji).getAttribute('aria-pressed')).toBe('false')
  })

  it('moves the pressed mark to the currency that is chosen', async () => {
    await renderNewGroup()

    fireEvent.click(currencyChip('EUR'))

    expect(currencyChip('EUR').getAttribute('aria-pressed')).toBe('true')
    expect(currencyChip('MKD').getAttribute('aria-pressed')).toBe('false')
  })

  it('creates the group with the name, the first emoji and MKD when only the name is typed', async () => {
    const fetchMock = stubFetchByRequest({ 'POST /api/groups': jsonAnswer(testGroup) })
    await renderNewGroup()
    typeInto(translated('en', 'groupFields.name'), groupName)

    pressCreate()

    await waitFor(() => {
      expect(requestCount(fetchMock, 'POST', '/api/groups')).toBe(1)
    })
    expect(requestsOf(fetchMock, 'POST', '/api/groups')[0]).toMatchObject({
      contentType: 'application/json',
      body: { name: groupName, emoji: defaultGroupEmoji, currency: 'MKD' },
    })
  })

  it('creates the group with the emoji and the currency that were chosen', async () => {
    const fetchMock = stubFetchByRequest({ 'POST /api/groups': jsonAnswer(testGroup) })
    await renderNewGroup()
    typeInto(translated('en', 'groupFields.name'), 'Flat 4B')
    fireEvent.click(emojiChip(groupEmojis[2]))
    fireEvent.click(currencyChip('EUR'))

    pressCreate()

    await waitFor(() => {
      expect(requestCount(fetchMock, 'POST', '/api/groups')).toBe(1)
    })
    expect(requestsOf(fetchMock, 'POST', '/api/groups')[0].body).toEqual({
      name: 'Flat 4B',
      emoji: groupEmojis[2],
      currency: 'EUR',
    })
  })

  it('creates the group when the form is submitted with the Enter key', async () => {
    const fetchMock = stubFetchByRequest({ 'POST /api/groups': jsonAnswer(testGroup) })
    await renderNewGroup()
    typeInto(translated('en', 'groupFields.name'), groupName)
    const form = nameField().form
    if (form === null) {
      throw new Error('The name field is not inside a form')
    }

    fireEvent.submit(form)

    await waitFor(() => {
      expect(requestCount(fetchMock, 'POST', '/api/groups')).toBe(1)
    })
  })

  it('opens the new group after it is created', async () => {
    stubFetchByRequest({ 'POST /api/groups': jsonAnswer(testGroup) })
    const { router } = await renderNewGroup()
    typeInto(translated('en', 'groupFields.name'), groupName)

    pressCreate()

    await waitFor(() => {
      expect(router.state.location.pathname).toBe(routes.group(testGroup.id))
    })
    expect(screen.getByText('group page')).toBeTruthy()
  })

  it('disables the Create button while the request waits for an answer', async () => {
    stubFetchThatNeverAnswers()
    await renderNewGroup()
    typeInto(translated('en', 'groupFields.name'), groupName)

    pressCreate()

    await expectDisabledWhilePending(createButton())
  })

  it('shows the name error and sends nothing when the name is empty', async () => {
    const fetchMock = stubFetchByRequest({ 'POST /api/groups': jsonAnswer(testGroup) })
    const { router } = await renderNewGroup()

    pressCreate()

    expect(await screen.findByText(translated('en', 'groupFields.nameInvalid'))).toBeTruthy()
    expect(fetchMock).not.toHaveBeenCalled()
    expect(router.state.location.pathname).toBe(routes.newGroup)
  })

  it('shows the name error and sends nothing when the name is only spaces', async () => {
    const fetchMock = stubFetchByRequest({ 'POST /api/groups': jsonAnswer(testGroup) })
    await renderNewGroup()
    typeInto(translated('en', 'groupFields.name'), '   ')

    pressCreate()

    expect(await screen.findByText(translated('en', 'groupFields.nameInvalid'))).toBeTruthy()
    expect(fetchMock).not.toHaveBeenCalled()
  })

  it.each(languages)('shows the name error in %s next to the empty name field', async (language) => {
    await renderNewGroup(language)

    pressCreate(language)

    expect(await screen.findByText(translated(language, 'groupFields.nameInvalid'))).toBeTruthy()
    expect(nameField(language).getAttribute('aria-invalid')).toBe('true')
  })

  it('keeps the chosen emoji and currency after the name error', async () => {
    await renderNewGroup()
    fireEvent.click(emojiChip(groupEmojis[3]))
    fireEvent.click(currencyChip('EUR'))

    pressCreate()
    await screen.findByText(translated('en', 'groupFields.nameInvalid'))

    expect(emojiChip(groupEmojis[3]).getAttribute('aria-pressed')).toBe('true')
    expect(currencyChip('EUR').getAttribute('aria-pressed')).toBe('true')
  })

  it('creates the group once the name is typed after the name error', async () => {
    const fetchMock = stubFetchByRequest({ 'POST /api/groups': jsonAnswer(testGroup) })
    await renderNewGroup()
    pressCreate()
    await screen.findByText(translated('en', 'groupFields.nameInvalid'))
    typeInto(translated('en', 'groupFields.name'), groupName)

    pressCreate()

    await waitFor(() => {
      expect(requestCount(fetchMock, 'POST', '/api/groups')).toBe(1)
    })
  })

  it.each(
    languages.flatMap((language) =>
      (['GROUP_NAME_INVALID', 'GROUP_EMOJI_INVALID', 'GROUP_CURRENCY_INVALID'] as const).map(
        (code) => [language, code] as const,
      ),
    ),
  )('shows the %s text for the answer %s', async (language, code) => {
    stubFetchByRequest({ 'POST /api/groups': problemAnswer(400, code) })
    await renderNewGroup(language)
    typeInto(translated(language, 'groupFields.name'), groupName)

    pressCreate(language)

    expect((await screen.findByRole('alert')).textContent).toContain(
      translated(language, `errors.${code}`),
    )
  })

  it('keeps the typed name, the chosen emoji and currency and the screen after the server refuses', async () => {
    stubFetchByRequest({ 'POST /api/groups': problemAnswer(400, 'GROUP_EMOJI_INVALID') })
    const { router } = await renderNewGroup()
    typeInto(translated('en', 'groupFields.name'), groupName)
    fireEvent.click(emojiChip(groupEmojis[4]))
    fireEvent.click(currencyChip('EUR'))

    pressCreate()
    await screen.findByRole('alert')

    expect(nameField().value).toBe(groupName)
    expect(emojiChip(groupEmojis[4]).getAttribute('aria-pressed')).toBe('true')
    expect(currencyChip('EUR').getAttribute('aria-pressed')).toBe('true')
    expect(router.state.location.pathname).toBe(routes.newGroup)
  })

  it('shows the network message when the server cannot be reached', async () => {
    stubFetchByRequest({ 'POST /api/groups': networkFailureAnswer() })
    await renderNewGroup()
    typeInto(translated('en', 'groupFields.name'), groupName)

    pressCreate()

    expect((await screen.findByRole('alert')).textContent).toContain(
      translated('en', 'errors.network'),
    )
  })

  it('opens the new group and clears the error when Create is pressed again after the server refused the first try', async () => {
    let tries = 0
    stubFetchByRequest({
      'POST /api/groups': () => {
        tries += 1
        return tries === 1 ? problemResponse(400, 'GROUP_NAME_INVALID') : Response.json(testGroup)
      },
    })
    const { router } = await renderNewGroup()
    typeInto(translated('en', 'groupFields.name'), groupName)
    pressCreate()
    await screen.findByRole('alert')

    pressCreate()

    await waitFor(() => {
      expect(router.state.location.pathname).toBe(routes.group(testGroup.id))
    })
    expect(screen.queryByRole('alert')).toBeNull()
  })
})
