import { screen } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { languages } from '@/core/i18n/language'
import { chip, isPressed } from '@/test/adminStatsTestHelpers'
import { stubGoogleSignIn } from '@/test/googleTestHelpers'
import { answerWith, findToolsTitle, openTab, renderOwnerTools, toolsRequests } from '@/test/ownerToolsTestHelpers'
import { logEntries } from '@/test/ownerToolsTestData'
import { stubDeviceColorScheme } from '@/test/themeTestHelpers'
import { translated } from '@/test/translated'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const tabKeys = ['people', 'groups', 'log'] as const
const logGet = 'GET /api/admin/tools/log'

describe('the Owner tools screen, its title and its three tabs', () => {
  beforeEach(() => {
    window.localStorage.clear()
    vi.clearAllMocks()
    stubDeviceColorScheme('light')
    stubGoogleSignIn()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it.each(languages)('shows the title as the one level 1 heading (%s)', async (language) => {
    await renderOwnerTools({ language, answers: { [logGet]: answerWith(logEntries) } })

    expect(await findToolsTitle(language)).toBeTruthy()
    expect(screen.getAllByRole('heading', { level: 1 })).toHaveLength(1)
  })

  it.each(languages)('shows the three tabs as buttons People, Groups and Log, in this order (%s)', async (language) => {
    await renderOwnerTools({ language })
    await findToolsTitle(language)

    const names = tabKeys.map((key) => translated(language, `ownerTools.tabs.${key}`))
    const buttons = names.map((name) => screen.getByRole('button', { name }))

    expect(buttons).toHaveLength(3)
    for (let index = 1; index < buttons.length; index += 1) {
      expect(
        Boolean(buttons[index - 1].compareDocumentPosition(buttons[index]) & Node.DOCUMENT_POSITION_FOLLOWING),
        names[index],
      ).toBe(true)
    }
  })

  it('opens on People, with People pressed and the other two not', async () => {
    await renderOwnerTools()
    await findToolsTitle()

    expect(isPressed(chip('People'))).toBe(true)
    expect(isPressed(chip('Groups'))).toBe(false)
    expect(isPressed(chip('Log'))).toBe(false)
  })

  it('shows the search field of the people and a Search button on the first tab', async () => {
    await renderOwnerTools()
    await findToolsTitle()

    expect(screen.getByLabelText(translated('en', 'ownerTools.people.searchLabel'))).toBeTruthy()
    expect(screen.getByRole('button', { name: translated('en', 'ownerTools.people.search') })).toBeTruthy()
    expect(screen.queryByLabelText(translated('en', 'ownerTools.groups.searchLabel'))).toBeNull()
  })

  it('asks the server for nothing before anyone searches, on the People tab', async () => {
    const { fetchMock } = await renderOwnerTools()
    await findToolsTitle()

    expect(toolsRequests(fetchMock)).toEqual([])
  })

  it('shows the group number field and a Find button after the Groups tab is pressed, and hides the people field', async () => {
    await renderOwnerTools()
    await findToolsTitle()

    openTab('groups')

    expect(screen.getByLabelText(translated('en', 'ownerTools.groups.searchLabel'))).toBeTruthy()
    expect(screen.getByRole('button', { name: translated('en', 'ownerTools.groups.search') })).toBeTruthy()
    expect(screen.queryByLabelText(translated('en', 'ownerTools.people.searchLabel'))).toBeNull()
    expect(isPressed(chip('Groups'))).toBe(true)
    expect(isPressed(chip('People'))).toBe(false)
  })

  it('shows the list of the log after the Log tab is pressed, and hides both search fields', async () => {
    await renderOwnerTools({ answers: { [logGet]: answerWith(logEntries) } })
    await findToolsTitle()

    openTab('log')

    expect(await screen.findByText(logEntries[0].reason)).toBeTruthy()
    expect(screen.queryByLabelText(translated('en', 'ownerTools.people.searchLabel'))).toBeNull()
    expect(screen.queryByLabelText(translated('en', 'ownerTools.groups.searchLabel'))).toBeNull()
    expect(isPressed(chip('Log'))).toBe(true)
  })

  it('goes back to the search field of the people when the People tab is pressed again', async () => {
    await renderOwnerTools({ answers: { [logGet]: answerWith(logEntries) } })
    await findToolsTitle()
    openTab('log')
    await screen.findByText(logEntries[0].reason)

    openTab('people')

    expect(screen.getByLabelText(translated('en', 'ownerTools.people.searchLabel'))).toBeTruthy()
    expect(screen.queryByText(logEntries[0].reason)).toBeNull()
  })

  it('writes the title and the three tabs in Macedonian', async () => {
    await renderOwnerTools({ language: 'mk' })

    expect(await findToolsTitle('mk')).toBeTruthy()
    for (const key of tabKeys) {
      expect(screen.getByRole('button', { name: translated('mk', `ownerTools.tabs.${key}`) })).toBeTruthy()
    }
    expect(screen.getByLabelText(translated('mk', 'ownerTools.people.searchLabel'))).toBeTruthy()
  })
})
