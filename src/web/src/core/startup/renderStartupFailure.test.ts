import { afterEach, describe, expect, it, vi } from 'vitest'
import en from '@/core/i18n/locales/en.json'
import mk from '@/core/i18n/locales/mk.json'
import { renderStartupFailure } from './renderStartupFailure'

describe('renderStartupFailure', () => {
  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('shows the message in both languages with a retry button, without i18next', () => {
    vi.spyOn(console, 'error').mockImplementation(() => {})
    const root = document.createElement('div')

    renderStartupFailure(root, new Error('translations failed to load'))

    const alert = root.querySelector('[role="alert"]')
    expect(alert?.textContent).toContain(en.errors.generic)
    expect(alert?.textContent).toContain(mk.errors.generic)
    expect(alert?.querySelector(`[lang="en"]`)?.textContent).toBe(en.errors.generic)
    expect(alert?.querySelector(`[lang="mk"]`)?.textContent).toBe(mk.errors.generic)
    expect(root.querySelector('button')?.textContent).toBe(
      `${en.common.retry} / ${mk.common.retry}`,
    )
  })

  it('logs the error that stopped the app from starting', () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {})
    const cause = new Error('translations failed to load')

    renderStartupFailure(document.createElement('div'), cause)

    expect(consoleError).toHaveBeenCalledWith('Kvit could not start', cause)
  })

  it('replaces whatever the root already contained', () => {
    vi.spyOn(console, 'error').mockImplementation(() => {})
    const root = document.createElement('div')
    root.textContent = 'half-drawn screen'

    renderStartupFailure(root, new Error('boom'))

    expect(root.textContent).not.toContain('half-drawn screen')
  })
})
