import { screen } from '@testing-library/react'
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
})
