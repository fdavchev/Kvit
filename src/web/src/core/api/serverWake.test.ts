import { afterEach, beforeEach, describe, expect, it, vi, type Mock } from 'vitest'
import { captureError, stubFetch } from '@/test/apiTestHelpers'
import { endpoints } from './endpoints'

const wakeUrl = 'https://kvit-mk-api.onrender.com/wake'
const currentPath = '/groups/7'
const currentSearch = '?tab=balances'
const wakeAddressWithReturn = `${wakeUrl}?returnTo=${encodeURIComponent(currentPath + currentSearch)}`
const pageOpenedAt = 1_800_000_000_000
const twoMinutes = 2 * 60 * 1000

let assign: Mock<(url: string) => void>

function asleepAnswer(address: string | null = wakeUrl): Response {
  const headers = new Headers({ 'content-type': 'text/plain; charset=utf-8', 'x-kvit-server-asleep': '1' })
  if (address !== null) {
    headers.set('x-kvit-wake-url', address)
  }
  return new Response('Kvit proxy: the API server is asleep', { status: 503, headers })
}

function plainAnswer(status: number): Response {
  return new Response(status === 204 ? null : 'answer', { status })
}

async function loadServerWake() {
  vi.resetModules()
  return import('./serverWake')
}

function rememberWakeVisitAt(visitedAt: number | string): void {
  window.sessionStorage.setItem('kvit.lastWakeVisitAt', String(visitedAt))
}

beforeEach(() => {
  assign = vi.fn<(url: string) => void>()
  vi.stubGlobal('location', { pathname: currentPath, search: currentSearch, assign })
  vi.spyOn(Date, 'now').mockReturnValue(pageOpenedAt)
  window.sessionStorage.clear()
})

afterEach(() => {
  vi.unstubAllGlobals()
  vi.restoreAllMocks()
  window.sessionStorage.clear()
})

describe('the first answer to the "who am I" check says the server is asleep', () => {
  it('sends the whole page through the wake address with the current path and search as returnTo', async () => {
    const { followServerAnswer } = await loadServerWake()

    followServerAnswer('GET', endpoints.me, asleepAnswer())

    expect(assign).toHaveBeenCalledExactlyOnceWith(wakeAddressWithReturn)
  })

  it('remembers the moment of the automatic visit for this tab', async () => {
    const { followServerAnswer } = await loadServerWake()

    followServerAnswer('GET', endpoints.me, asleepAnswer())

    expect(window.sessionStorage.getItem('kvit.lastWakeVisitAt')).toBe(String(pageOpenedAt))
  })

  it('does not also offer the wake button', async () => {
    const { followServerAnswer, readOfferedWakeUrl } = await loadServerWake()

    followServerAnswer('GET', endpoints.me, asleepAnswer())

    expect(readOfferedWakeUrl()).toBeNull()
  })

  it('accepts an http wake address on localhost', async () => {
    const { followServerAnswer } = await loadServerWake()

    followServerAnswer('GET', endpoints.me, asleepAnswer('http://localhost:5018/wake'))

    expect(assign).toHaveBeenCalledExactlyOnceWith(`http://localhost:5018/wake?returnTo=${encodeURIComponent(currentPath + currentSearch)}`)
  })

  it('does not navigate again by itself less than 2 minutes after the last wake visit, and offers the button instead', async () => {
    rememberWakeVisitAt(pageOpenedAt - twoMinutes + 1)
    const { followServerAnswer, readOfferedWakeUrl } = await loadServerWake()

    followServerAnswer('GET', endpoints.me, asleepAnswer())

    expect(assign).not.toHaveBeenCalled()
    expect(readOfferedWakeUrl()).toBe(wakeUrl)
  })

  it('navigates by itself again once the last wake visit is 2 minutes old', async () => {
    rememberWakeVisitAt(pageOpenedAt - twoMinutes)
    const { followServerAnswer } = await loadServerWake()

    followServerAnswer('GET', endpoints.me, asleepAnswer())

    expect(assign).toHaveBeenCalledExactlyOnceWith(wakeAddressWithReturn)
  })

  it('treats a saved visit time that is not a number as no visit', async () => {
    rememberWakeVisitAt('not a time')
    const { followServerAnswer } = await loadServerWake()

    followServerAnswer('GET', endpoints.me, asleepAnswer())

    expect(assign).toHaveBeenCalledOnce()
  })

  it('does not navigate by itself and logs the reason when sessionStorage cannot be read', async () => {
    vi.spyOn(Storage.prototype, 'getItem').mockImplementation(() => {
      throw new DOMException('Storage is blocked', 'SecurityError')
    })
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => undefined)
    const { followServerAnswer, readOfferedWakeUrl } = await loadServerWake()

    followServerAnswer('GET', endpoints.me, asleepAnswer())

    expect(assign).not.toHaveBeenCalled()
    expect(readOfferedWakeUrl()).toBe(wakeUrl)
    expect(consoleError).toHaveBeenCalledWith(expect.stringContaining('sessionStorage'), expect.any(DOMException))
  })

  it('does not navigate by itself and logs the reason when sessionStorage cannot be written', async () => {
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => {
      throw new DOMException('Storage is full', 'QuotaExceededError')
    })
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => undefined)
    const { followServerAnswer, readOfferedWakeUrl } = await loadServerWake()

    followServerAnswer('GET', endpoints.me, asleepAnswer())

    expect(assign).not.toHaveBeenCalled()
    expect(readOfferedWakeUrl()).toBe(wakeUrl)
    expect(consoleError).toHaveBeenCalledWith(expect.stringContaining('sessionStorage'), expect.any(DOMException))
  })
})

describe('an asleep answer that is not the first answer to the "who am I" check', () => {
  it.each([
    ['POST', endpoints.logIn],
    ['GET', endpoints.groups],
    ['GET', `${endpoints.me}/export`],
    ['PUT', endpoints.me],
  ])('never navigates by itself for %s %s and offers the wake button', async (method, path) => {
    const { followServerAnswer, readOfferedWakeUrl } = await loadServerWake()

    followServerAnswer(method, path, asleepAnswer())

    expect(assign).not.toHaveBeenCalled()
    expect(readOfferedWakeUrl()).toBe(wakeUrl)
  })

  it('never navigates by itself for a later "who am I" check after the first one was answered', async () => {
    const { followServerAnswer, readOfferedWakeUrl } = await loadServerWake()
    followServerAnswer('GET', endpoints.me, plainAnswer(200))

    followServerAnswer('GET', endpoints.me, asleepAnswer())

    expect(assign).not.toHaveBeenCalled()
    expect(readOfferedWakeUrl()).toBe(wakeUrl)
  })

  it('never navigates by itself when the first "who am I" answer was another failure', async () => {
    const { followServerAnswer } = await loadServerWake()
    followServerAnswer('GET', endpoints.me, plainAnswer(502))

    followServerAnswer('GET', endpoints.me, asleepAnswer())

    expect(assign).not.toHaveBeenCalled()
  })

  it('still lets the first "who am I" answer navigate when other requests were answered asleep before it', async () => {
    const { followServerAnswer } = await loadServerWake()
    followServerAnswer('GET', endpoints.groups, asleepAnswer())

    followServerAnswer('GET', endpoints.me, asleepAnswer())

    expect(assign).toHaveBeenCalledExactlyOnceWith(wakeAddressWithReturn)
  })
})

describe('an asleep answer whose wake address cannot be trusted', () => {
  it.each([
    ['an http address that is not localhost', 'http://kvit-mk-api.onrender.com/wake'],
    ['a javascript address', 'javascript:alert(1)'],
    ['a relative path', '/wake'],
    ['an ftp address', 'ftp://kvit-mk-api.onrender.com/wake'],
    ['text that is no address', 'wake me up'],
  ])('never navigates, offers no button and logs a clear error for %s', async (_case, address) => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => undefined)
    const { followServerAnswer, readOfferedWakeUrl } = await loadServerWake()

    followServerAnswer('GET', endpoints.me, asleepAnswer(address))

    expect(assign).not.toHaveBeenCalled()
    expect(readOfferedWakeUrl()).toBeNull()
    expect(consoleError).toHaveBeenCalledWith(expect.stringContaining(JSON.stringify(address)))
  })

  it('logs a clear error when the wake address header is missing', async () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => undefined)
    const { followServerAnswer, readOfferedWakeUrl } = await loadServerWake()

    followServerAnswer('GET', endpoints.me, asleepAnswer(null))

    expect(assign).not.toHaveBeenCalled()
    expect(readOfferedWakeUrl()).toBeNull()
    expect(consoleError).toHaveBeenCalledWith(expect.stringContaining('wake address null'))
  })

  it('ignores a wake address on an answer that does not say the server is asleep', async () => {
    const { followServerAnswer, readOfferedWakeUrl } = await loadServerWake()

    followServerAnswer('GET', endpoints.me, new Response('down', { status: 503, headers: { 'x-kvit-wake-url': wakeUrl } }))

    expect(assign).not.toHaveBeenCalled()
    expect(readOfferedWakeUrl()).toBeNull()
  })
})

describe('the offered wake button', () => {
  it.each([200, 204, 400, 401, 404, 429])('goes away after an answer with status %i, because the server answered', async (status) => {
    const { followServerAnswer, readOfferedWakeUrl } = await loadServerWake()
    followServerAnswer('GET', endpoints.groups, asleepAnswer())

    followServerAnswer('GET', endpoints.groups, plainAnswer(status))

    expect(readOfferedWakeUrl()).toBeNull()
  })

  it.each([500, 502, 504])('stays after an answer with status %i that does not say the server is asleep', async (status) => {
    const { followServerAnswer, readOfferedWakeUrl } = await loadServerWake()
    followServerAnswer('GET', endpoints.groups, asleepAnswer())

    followServerAnswer('GET', endpoints.groups, plainAnswer(status))

    expect(readOfferedWakeUrl()).toBe(wakeUrl)
  })

  it('tells its listeners when it appears and when it goes away', async () => {
    const { followServerAnswer, subscribeToWakeOffer } = await loadServerWake()
    const listener = vi.fn()
    subscribeToWakeOffer(listener)

    followServerAnswer('GET', endpoints.groups, asleepAnswer())
    followServerAnswer('GET', endpoints.groups, asleepAnswer())
    followServerAnswer('GET', endpoints.groups, plainAnswer(200))

    expect(listener).toHaveBeenCalledTimes(2)
  })

  it('sends the page through the wake address with the current path when tapped, and remembers the visit', async () => {
    const { wakeServer } = await loadServerWake()

    wakeServer(wakeUrl)

    expect(assign).toHaveBeenCalledExactlyOnceWith(wakeAddressWithReturn)
    expect(window.sessionStorage.getItem('kvit.lastWakeVisitAt')).toBe(String(pageOpenedAt))
  })

  it('still sends the page through the wake address when tapped while sessionStorage cannot be written', async () => {
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => {
      throw new DOMException('Storage is full', 'QuotaExceededError')
    })
    vi.spyOn(console, 'error').mockImplementation(() => undefined)
    const { wakeServer } = await loadServerWake()

    wakeServer(wakeUrl)

    expect(assign).toHaveBeenCalledExactlyOnceWith(wakeAddressWithReturn)
  })
})

describe('apiRequest with an asleep answer', () => {
  it('sends the page through the wake address on the first "who am I" check and still fails as a server that is not ready', async () => {
    stubFetch(asleepAnswer())
    const { apiRequest } = await loadApiClient()
    const { errorMessageKey, isServerNotReachedError } = await import('./errors')

    const error = await captureError(apiRequest(endpoints.me))

    expect(assign).toHaveBeenCalledExactlyOnceWith(wakeAddressWithReturn)
    expect(isServerNotReachedError(error)).toBe(true)
    expect(errorMessageKey(error)).toBe('errors.network')
  })

  it('offers the wake button for a save and fails as a server that is not ready, so the save is queued', async () => {
    stubFetch(asleepAnswer())
    const { apiRequest, jsonRequest } = await loadApiClient()
    const { isServerNotReachedError } = await import('./errors')
    const { readOfferedWakeUrl } = await import('./serverWake')

    const error = await captureError(apiRequest(endpoints.groups, jsonRequest('POST', { name: 'Greece trip' })))

    expect(assign).not.toHaveBeenCalled()
    expect(readOfferedWakeUrl()).toBe(wakeUrl)
    expect(isServerNotReachedError(error)).toBe(true)
  })

  it('takes the wake button away after a successful answer', async () => {
    stubFetch(asleepAnswer(), new Response('{"id":"g1"}', { status: 200, headers: { 'content-type': 'application/json' } }))
    const { apiRequest } = await loadApiClient()
    const { readOfferedWakeUrl } = await import('./serverWake')
    await captureError(apiRequest(endpoints.groups))

    await apiRequest(endpoints.groups)

    expect(readOfferedWakeUrl()).toBeNull()
  })
})

async function loadApiClient() {
  vi.resetModules()
  return import('./apiClient')
}
