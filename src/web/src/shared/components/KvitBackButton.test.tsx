import { act, fireEvent, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { languages } from '@/core/i18n/language'
import { renderElementWithProviders } from '@/test/renderWithProviders'
import { translated } from '@/test/translated'
import { KvitBackButton } from './KvitBackButton'

describe('KvitBackButton', () => {
  it.each(languages)('is a link named with the translated back text (%s)', async (language) => {
    await renderElementWithProviders(<KvitBackButton to="/welcome" />, '/', { language })

    expect(screen.getByRole('link', { name: translated(language, 'common.back') })).toBeTruthy()
  })

  it('points at the address it is given', async () => {
    await renderElementWithProviders(<KvitBackButton to="/welcome" />, '/')

    const link = screen.getByRole('link', { name: translated('en', 'common.back') })

    expect(link.getAttribute('href')).toBe('/welcome')
  })

  it('adds a history entry when it is pressed without the replace prop', async () => {
    const { router } = await renderElementWithProviders(<KvitBackButton to="/welcome" />, '/')

    fireEvent.click(screen.getByRole('link', { name: translated('en', 'common.back') }))

    expect(router.state.historyAction).toBe('PUSH')
    expect(router.state.location.pathname).toBe('/welcome')
  })

  it('replaces the history entry when it is pressed with the replace prop, so the browser Back skips the screen it left', async () => {
    const { router } = await renderElementWithProviders(<KvitBackButton to="/welcome" replace />, '/')

    fireEvent.click(screen.getByRole('link', { name: translated('en', 'common.back') }))

    expect(router.state.historyAction).toBe('REPLACE')
    expect(router.state.location.pathname).toBe('/welcome')
    await act(() => router.navigate(-1))
    expect(router.state.location.pathname).toBe('/welcome')
  })
})
