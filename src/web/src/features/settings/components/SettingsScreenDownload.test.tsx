import { fireEvent, screen, waitFor } from '@testing-library/react'
import { toast } from 'sonner'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { endpoints } from '@/core/api/endpoints'
import { languages, type Language } from '@/core/i18n/language'
import { routes } from '@/core/router/routes'
import type { Me } from '@/core/services/me/meService'
import { problemResponse, sentRequest, stubFetch, stubFetchThatNeverAnswers } from '@/test/apiTestHelpers'
import { isDescribedBy } from '@/test/describedByTestHelpers'
import { stubFileDownload, type FileDownloadStub } from '@/test/fileDownloadTestHelpers'
import { renderElementWithProviders } from '@/test/renderWithProviders'
import { seedMe, testAdminMe, testGuestMe, testMe } from '@/test/testMe'
import { resetThemeDocument } from '@/test/themeTestHelpers'
import { translated } from '@/test/translated'
import { SettingsScreen } from './SettingsScreen'

vi.mock('sonner', async () => (await import('@/test/sonnerMock')).sonnerMock)

const exportBody = '{"profile":{"displayName":"Filip"}}'
const people: [string, Me][] = [
  ['a person with a password', testMe],
  ['a person without a password', { ...testMe, hasPassword: false }],
  ['an admin', testAdminMe],
  ['a guest', testGuestMe],
]

function exportResponse(contentDisposition: string | null = 'attachment; filename="kvit-my-data-2026-11-04.json"'): Response {
  const headers = new Headers({ 'content-type': 'application/json' })
  if (contentDisposition !== null) {
    headers.set('content-disposition', contentDisposition)
  }
  return new Response(exportBody, { status: 200, headers })
}

async function renderSettings(me: Me = testMe, language: Language = 'en') {
  return renderElementWithProviders(<SettingsScreen />, routes.settings, {
    language,
    seedCache: seedMe({ ...me, language }),
  })
}

function downloadButton(language: Language = 'en'): HTMLElement {
  return screen.getByRole('button', { name: translated(language, 'settings.downloadData') })
}

function pressDownload(language: Language = 'en'): void {
  fireEvent.click(downloadButton(language))
}

function comesBefore(first: HTMLElement, second: HTMLElement): boolean {
  return Boolean(first.compareDocumentPosition(second) & Node.DOCUMENT_POSITION_FOLLOWING)
}

describe('SettingsScreen, Download my data', () => {
  let download: FileDownloadStub

  beforeEach(() => {
    window.localStorage.clear()
    resetThemeDocument()
    vi.clearAllMocks()
    download = stubFileDownload()
  })

  afterEach(() => {
    download.restore()
    vi.unstubAllGlobals()
  })

  describe('the row', () => {
    it.each(people)('shows the Download my data button to %s', async (_name, me) => {
      await renderSettings(me)

      expect(downloadButton()).toBeTruthy()
    })

    it.each(languages)('shows the button and its grey hint in %s', async (language) => {
      await renderSettings(testMe, language)

      expect(downloadButton(language)).toBeTruthy()
      expect(screen.getByText(translated(language, 'settings.downloadDataHint'))).toBeTruthy()
    })

    it.each(people)('describes the button with the hint for %s', async (_name, me) => {
      await renderSettings(me)

      const hint = screen.getByText(translated('en', 'settings.downloadDataHint'))

      expect(isDescribedBy(downloadButton(), hint)).toBe(true)
    })

    it.each(people)('shows exactly one Download my data button to %s', async (_name, me) => {
      await renderSettings(me)

      expect(screen.getAllByRole('button', { name: translated('en', 'settings.downloadData') })).toHaveLength(1)
    })

    it('is a button and not a link, because it downloads a file', async () => {
      await renderSettings()

      expect(screen.queryByRole('link', { name: translated('en', 'settings.downloadData') })).toBeNull()
    })

    it.each(people)('puts the button, then its hint, then the Terms link, then the Privacy link for %s', async (_name, me) => {
      await renderSettings(me)
      const hint = screen.getByText(translated('en', 'settings.downloadDataHint'))
      const terms = screen.getByRole('link', { name: translated('en', 'common.terms') })
      const privacy = screen.getByRole('link', { name: translated('en', 'common.privacy') })

      expect(comesBefore(downloadButton(), hint)).toBe(true)
      expect(comesBefore(hint, terms)).toBe(true)
      expect(comesBefore(terms, privacy)).toBe(true)
    })

    it.each([true, false])('puts the button after the password link when hasPassword is %s', async (hasPassword) => {
      await renderSettings({ ...testMe, hasPassword })
      const passwordLink = screen.getByRole('link', {
        name: translated('en', hasPassword ? 'auth.changePassword.title' : 'auth.setPassword.title'),
      })

      expect(comesBefore(passwordLink, downloadButton())).toBe(true)
    })

    it.each(people)('puts the button before Log out for %s', async (_name, me) => {
      await renderSettings(me)
      const logOut = screen.getByRole('button', { name: translated('en', 'settings.logOut') })

      expect(comesBefore(downloadButton(), logOut)).toBe(true)
    })

    it.each(people)('is enabled and not busy before it is tapped for %s', async (_name, me) => {
      await renderSettings(me)

      expect(downloadButton().matches(':disabled')).toBe(false)
      expect(downloadButton().getAttribute('aria-busy')).not.toBe('true')
    })

    it.each(people)('sends no request and shows no toast before the tap for %s', async (_name, me) => {
      const fetchMock = stubFetch(exportResponse())

      await renderSettings(me)

      expect(fetchMock).not.toHaveBeenCalled()
      expect(download.savedFiles).toEqual([])
      expect(toast.success).not.toHaveBeenCalled()
      expect(toast.error).not.toHaveBeenCalled()
    })
  })

  describe('the download', () => {
    it.each(people)('asks GET /api/me/export with one tap for %s', async (_name, me) => {
      const fetchMock = stubFetch(exportResponse())
      await renderSettings(me)

      pressDownload()

      await waitFor(() => {
        expect(fetchMock).toHaveBeenCalledOnce()
      })
      expect(sentRequest(fetchMock).url).toBe(endpoints.meExport)
      expect(sentRequest(fetchMock).method ?? 'GET').toBe('GET')
    })

    it.each(people)('saves the answer as a file named by the Content-Disposition header for %s', async (_name, me) => {
      stubFetch(exportResponse('attachment; filename="kvit-my-data-2026-11-04.json"'))
      await renderSettings(me)

      pressDownload()

      await waitFor(() => {
        expect(download.savedFiles).toHaveLength(1)
      })
      expect(download.savedFiles[0].fileName).toBe('kvit-my-data-2026-11-04.json')
      expect(await download.createdBlobs[0].text()).toBe(exportBody)
    })

    it('saves the file as kvit-my-data.json when the answer has no Content-Disposition header', async () => {
      stubFetch(exportResponse(null))
      await renderSettings()

      pressDownload()

      await waitFor(() => {
        expect(download.savedFiles).toHaveLength(1)
      })
      expect(download.savedFiles[0].fileName).toBe('kvit-my-data.json')
      expect(await download.createdBlobs[0].text()).toBe(exportBody)
    })

    it.each(languages)('shows the success toast after the file was saved (%s)', async (language) => {
      stubFetch(exportResponse())
      await renderSettings(testMe, language)

      pressDownload(language)

      await waitFor(() => {
        expect(toast.success).toHaveBeenCalledWith(translated(language, 'settings.downloadDataDone'))
      })
      expect(toast.success).toHaveBeenCalledOnce()
      expect(download.savedFiles).toHaveLength(1)
      expect(toast.error).not.toHaveBeenCalled()
    })

    it('shows the success toast only after the file was saved', async () => {
      stubFetch(exportResponse())
      await renderSettings()
      let savedFilesWhenToastShown = -1
      vi.mocked(toast.success).mockImplementationOnce(() => {
        savedFilesWhenToastShown = download.savedFiles.length
        return 1
      })

      pressDownload()

      await waitFor(() => {
        expect(toast.success).toHaveBeenCalled()
      })
      expect(savedFilesWhenToastShown).toBe(1)
    })

    it('stays on the settings screen and keeps the row usable after a successful download', async () => {
      stubFetch(exportResponse())
      const { router } = await renderSettings()

      pressDownload()
      await waitFor(() => {
        expect(toast.success).toHaveBeenCalled()
      })

      expect(router.state.location.pathname).toBe(routes.settings)
      expect(downloadButton().matches(':disabled')).toBe(false)
      expect(downloadButton().getAttribute('aria-busy')).not.toBe('true')
    })

    it('downloads again with a second tap', async () => {
      const fetchMock = stubFetch(exportResponse(), exportResponse())
      await renderSettings()
      pressDownload()
      await waitFor(() => {
        expect(download.savedFiles).toHaveLength(1)
      })
      await waitFor(() => {
        expect(downloadButton().matches(':disabled')).toBe(false)
      })

      pressDownload()

      await waitFor(() => {
        expect(download.savedFiles).toHaveLength(2)
      })
      expect(fetchMock).toHaveBeenCalledTimes(2)
    })
  })

  describe('while it runs', () => {
    it('disables the button and marks it busy while the server has not answered', async () => {
      stubFetchThatNeverAnswers()
      await renderSettings()

      pressDownload()

      await waitFor(() => {
        expect(downloadButton().matches(':disabled')).toBe(true)
      })
      expect(downloadButton().getAttribute('aria-busy')).toBe('true')
    })

    it('creates no file and shows no toast while the server has not answered', async () => {
      stubFetchThatNeverAnswers()
      await renderSettings()

      pressDownload()

      await waitFor(() => {
        expect(downloadButton().matches(':disabled')).toBe(true)
      })
      expect(download.savedFiles).toEqual([])
      expect(toast.success).not.toHaveBeenCalled()
      expect(toast.error).not.toHaveBeenCalled()
    })

    it('sends one request even when the button is tapped twice quickly', async () => {
      const fetchMock = stubFetch(exportResponse())
      await renderSettings()

      pressDownload()
      pressDownload()

      await waitFor(() => {
        expect(download.savedFiles).toHaveLength(1)
      })
      expect(fetchMock).toHaveBeenCalledOnce()
    })
  })

  describe('when it fails', () => {
    it.each(languages)('shows the too-many-tries toast when the server answers RATE_LIMITED (%s)', async (language) => {
      stubFetch(problemResponse(429, 'RATE_LIMITED'))
      await renderSettings(testMe, language)

      pressDownload(language)

      await waitFor(() => {
        expect(toast.error).toHaveBeenCalledWith(translated(language, 'errors.RATE_LIMITED'))
      })
    })

    it('shows the network toast when the server cannot be reached', async () => {
      stubFetch(new TypeError('Failed to fetch'))
      await renderSettings()

      pressDownload()

      await waitFor(() => {
        expect(toast.error).toHaveBeenCalledWith(translated('en', 'errors.network'))
      })
    })

    it('shows the generic toast when the server fails without a reason', async () => {
      stubFetch(new Response(null, { status: 500 }))
      await renderSettings()

      pressDownload()

      await waitFor(() => {
        expect(toast.error).toHaveBeenCalledWith(translated('en', 'errors.generic'))
      })
    })

    it.each([
      ['RATE_LIMITED', problemResponse(429, 'RATE_LIMITED')],
      ['a network failure', new TypeError('Failed to fetch')],
      ['a server error', new Response(null, { status: 500 })],
    ])('creates no file and no success toast after %s', async (_name, failure) => {
      stubFetch(failure)
      await renderSettings()

      pressDownload()

      await waitFor(() => {
        expect(toast.error).toHaveBeenCalledOnce()
      })
      expect(download.createdBlobs).toEqual([])
      expect(download.savedFiles).toEqual([])
      expect(toast.success).not.toHaveBeenCalled()
    })

    it('makes the row work again after a failure and downloads on the next tap', async () => {
      stubFetch(problemResponse(429, 'RATE_LIMITED'), exportResponse())
      await renderSettings()
      pressDownload()
      await waitFor(() => {
        expect(toast.error).toHaveBeenCalledOnce()
      })
      await waitFor(() => {
        expect(downloadButton().matches(':disabled')).toBe(false)
      })
      expect(downloadButton().getAttribute('aria-busy')).not.toBe('true')

      pressDownload()

      await waitFor(() => {
        expect(download.savedFiles).toHaveLength(1)
      })
      expect(toast.success).toHaveBeenCalledWith(translated('en', 'settings.downloadDataDone'))
    })
  })
})
