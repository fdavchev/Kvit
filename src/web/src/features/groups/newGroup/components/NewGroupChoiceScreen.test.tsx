import { fireEvent, screen, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { languages, type Language } from '@/core/i18n/language'
import { routes } from '@/core/router/routes'
import { isBefore, startsWith } from '@/test/expenseTestHelpers'
import { renderRoutesWithProviders } from '@/test/renderWithProviders'
import { stubFetchByRequest } from '@/test/requestTestHelpers'
import { seedMe, testMe } from '@/test/testMe'
import { translated } from '@/test/translated'
import { NewGroupChoiceScreen } from './NewGroupChoiceScreen'

async function renderChoice(language: Language = 'en') {
  const fetchMock = stubFetchByRequest({})
  const rendered = await renderRoutesWithProviders(
    [
      { path: routes.newGroup, element: <NewGroupChoiceScreen /> },
      { path: routes.newBill, element: <p>one bill page</p> },
      { path: routes.newGroupGroup, element: <p>group form page</p> },
      { path: routes.groups, element: <p>groups page</p> },
    ],
    routes.newGroup,
    { language, seedCache: seedMe({ ...testMe, language }) },
  )
  return { fetchMock, ...rendered }
}

function oneBillRow(language: Language = 'en'): HTMLElement {
  return screen.getByRole('link', { name: startsWith(translated(language, 'newGroup.oneBill')) })
}

function groupRow(language: Language = 'en'): HTMLElement {
  return screen.getByRole('link', { name: startsWith(translated(language, 'newGroup.group')) })
}

describe('NewGroupChoiceScreen', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it.each(languages)('shows the New group heading (%s)', async (language) => {
    await renderChoice(language)

    expect(
      screen.getByRole('heading', { level: 1, name: translated(language, 'newGroup.title') }),
    ).toBeTruthy()
  })

  it('has a back button to the groups list', async () => {
    await renderChoice()

    const back = screen.getByRole('link', { name: translated('en', 'common.back') })

    expect(back.getAttribute('href')).toBe(routes.groups)
  })

  it.each(languages)('offers two rows, One bill with "A dinner or a taxi" and Group with "A trip or a household" (%s)', async (language) => {
    await renderChoice(language)

    expect(oneBillRow(language).textContent).toContain(translated(language, 'newGroup.oneBillHint'))
    expect(groupRow(language).textContent).toContain(translated(language, 'newGroup.groupHint'))
  })

  it('offers nothing but the back button and the two rows', async () => {
    await renderChoice()

    expect(screen.getAllByRole('link')).toHaveLength(3)
    expect(screen.queryAllByRole('button')).toHaveLength(0)
  })

  it('puts One bill above Group', async () => {
    await renderChoice()

    expect(isBefore(oneBillRow(), groupRow())).toBe(true)
  })

  it('links One bill to the One bill form and Group to the group form', async () => {
    await renderChoice()

    expect(oneBillRow().getAttribute('href')).toBe(routes.newBill)
    expect(groupRow().getAttribute('href')).toBe(routes.newGroupGroup)
  })

  it('opens the One bill form with one tap on its row', async () => {
    const { router } = await renderChoice()

    fireEvent.click(oneBillRow())

    await waitFor(() => {
      expect(router.state.location.pathname).toBe(routes.newBill)
    })
    expect(screen.getByText('one bill page')).toBeTruthy()
  })

  it('opens the group form with one tap on its row', async () => {
    const { router } = await renderChoice()

    fireEvent.click(groupRow())

    await waitFor(() => {
      expect(router.state.location.pathname).toBe(routes.newGroupGroup)
    })
    expect(screen.getByText('group form page')).toBeTruthy()
  })

  it('opens the groups list when the back button is pressed', async () => {
    const { router } = await renderChoice()

    fireEvent.click(screen.getByRole('link', { name: translated('en', 'common.back') }))

    await waitFor(() => {
      expect(router.state.location.pathname).toBe(routes.groups)
    })
  })

  it('sends no request', async () => {
    const { fetchMock } = await renderChoice()

    expect(fetchMock).not.toHaveBeenCalled()
  })
})
