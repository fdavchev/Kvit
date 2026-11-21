import { act, fireEvent, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { renderElementWithProviders } from '@/test/renderWithProviders'
import { KvitLinkButton } from './KvitLinkButton'

describe('KvitLinkButton', () => {
  it('is a link that points at the address it is given', async () => {
    await renderElementWithProviders(<KvitLinkButton to="/login">Log in</KvitLinkButton>, '/')

    expect(screen.getByRole('link', { name: 'Log in' }).getAttribute('href')).toBe('/login')
  })

  it('adds a history entry when it is pressed without the replace prop', async () => {
    const { router } = await renderElementWithProviders(<KvitLinkButton to="/login">Log in</KvitLinkButton>, '/')

    fireEvent.click(screen.getByRole('link', { name: 'Log in' }))

    expect(router.state.historyAction).toBe('PUSH')
    expect(router.state.location.pathname).toBe('/login')
  })

  it('replaces the history entry when it is pressed with the replace prop, so the browser Back skips the screen it left', async () => {
    const { router } = await renderElementWithProviders(
      <KvitLinkButton to="/login" replace>
        Log in
      </KvitLinkButton>,
      '/',
    )

    fireEvent.click(screen.getByRole('link', { name: 'Log in' }))

    expect(router.state.historyAction).toBe('REPLACE')
    expect(router.state.location.pathname).toBe('/login')
    await act(() => router.navigate(-1))
    expect(router.state.location.pathname).toBe('/login')
  })

  it('carries the router state it is given to the next screen', async () => {
    const { router } = await renderElementWithProviders(
      <KvitLinkButton to="/login" state={{ joinToken: 'abc' }} replace>
        Log in
      </KvitLinkButton>,
      '/',
    )

    fireEvent.click(screen.getByRole('link', { name: 'Log in' }))

    expect(router.state.location.state).toEqual({ joinToken: 'abc' })
  })
})
