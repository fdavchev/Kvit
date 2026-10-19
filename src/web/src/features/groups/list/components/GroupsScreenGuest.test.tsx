import { screen } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { languages, type Language } from '@/core/i18n/language'
import { routes } from '@/core/router/routes'
import { emptyGroupList, flatGroupRow, greeceGroupRow, groupListOf, summerFinishedRow } from '@/test/groupTestData'
import { renderRoutesWithProviders } from '@/test/renderWithProviders'
import { jsonAnswer, stubFetchByRequest } from '@/test/requestTestHelpers'
import { seedMe, testGuestMe, testMe } from '@/test/testMe'
import { translated } from '@/test/translated'
import { GroupsScreen } from './GroupsScreen'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

async function renderGroups(list: unknown, language: Language = 'en', isGuest: boolean = true) {
  stubFetchByRequest({ 'GET /api/groups': jsonAnswer(list) })
  const me = isGuest ? testGuestMe : testMe
  return renderRoutesWithProviders(
    [{ path: routes.groups, element: <GroupsScreen /> }],
    routes.groups,
    { language, seedCache: seedMe({ ...me, language }) },
  )
}

describe('GroupsScreen for a guest', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it.each(languages)('shows the groups of the guest without a New group button (%s)', async (language) => {
    await renderGroups(groupListOf({ groups: [greeceGroupRow, flatGroupRow], finishedGroups: [summerFinishedRow] }), language)

    expect(await screen.findByRole('link', { name: new RegExp(greeceGroupRow.name) })).toBeTruthy()
    expect(screen.queryByRole('link', { name: translated(language, 'groups.newButton') })).toBeNull()
  })

  it.each(languages)('shows the guest empty text, which does not ask to create a group, and no New group button (%s)', async (language) => {
    await renderGroups(emptyGroupList, language)

    expect(await screen.findByText(translated(language, 'groups.emptyGuest'))).toBeTruthy()
    expect(screen.queryByText(translated(language, 'groups.empty'))).toBeNull()
    expect(screen.queryByRole('link', { name: translated(language, 'groups.newButton') })).toBeNull()
  })

  it('still shows the Recently deleted link to a guest', async () => {
    await renderGroups(emptyGroupList)

    expect(await screen.findByRole('link', { name: new RegExp(translated('en', 'groups.recentlyDeletedLink')) })).toBeTruthy()
  })

  it('still shows the New group button and the normal empty text to a person with an account', async () => {
    await renderGroups(emptyGroupList, 'en', false)

    expect(await screen.findByText(translated('en', 'groups.empty'))).toBeTruthy()
    expect(screen.getByRole('link', { name: translated('en', 'groups.newButton') }).getAttribute('href')).toBe(routes.newGroup)
  })
})
