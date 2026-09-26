import { afterEach, describe, expect, it, vi } from 'vitest'
import { proxyToApi } from '../../functions/api/[[path]]'

const apiOrigin = 'https://kvit-mk-api.onrender.com'

function stubUpstream(response: Response) {
  const fetchMock = vi.fn(async (_request: Request) => response)
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

function forwardedRequest(fetchMock: ReturnType<typeof stubUpstream>): Request {
  const [request] = fetchMock.mock.calls[0]
  return request
}

describe('proxyToApi', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('keeps the path and query string', async () => {
    const fetchMock = stubUpstream(new Response('Healthy'))

    await proxyToApi(
      new Request('https://kvit-mk.pages.dev/api/groups/7/expenses?page=2&size=20'),
      apiOrigin,
    )

    expect(forwardedRequest(fetchMock).url).toBe(
      'https://kvit-mk-api.onrender.com/api/groups/7/expenses?page=2&size=20',
    )
  })

  it('forwards the method, headers and body', async () => {
    const fetchMock = stubUpstream(new Response(null, { status: 204 }))

    await proxyToApi(
      new Request('https://kvit-mk.pages.dev/api/login', {
        method: 'POST',
        headers: { 'content-type': 'application/json', cookie: 'kvit=abc' },
        body: JSON.stringify({ email: 'ana@example.com' }),
      }),
      apiOrigin,
    )

    const forwarded = forwardedRequest(fetchMock)
    expect(forwarded.method).toBe('POST')
    expect(forwarded.headers.get('content-type')).toBe('application/json')
    expect(forwarded.headers.get('cookie')).toBe('kvit=abc')
    expect(await forwarded.text()).toBe('{"email":"ana@example.com"}')
  })

  it('passes the upstream answer back unchanged', async () => {
    stubUpstream(
      new Response('{"name":"Greece trip"}', {
        status: 201,
        headers: { 'set-cookie': 'kvit=abc; HttpOnly' },
      }),
    )

    const response = await proxyToApi(
      new Request('https://kvit-mk.pages.dev/api/groups', { method: 'POST' }),
      apiOrigin,
    )

    expect(response.status).toBe(201)
    expect(response.headers.get('set-cookie')).toBe('kvit=abc; HttpOnly')
    expect(await response.text()).toBe('{"name":"Greece trip"}')
  })

  it('does not follow upstream redirects', async () => {
    const fetchMock = stubUpstream(
      new Response(null, {
        status: 302,
        headers: { location: 'https://kvit-mk-api.onrender.com/elsewhere' },
      }),
    )

    const response = await proxyToApi(
      new Request('https://kvit-mk.pages.dev/api/health'),
      apiOrigin,
    )

    expect(forwardedRequest(fetchMock).redirect).toBe('manual')
    expect(response.status).toBe(302)
    expect(response.headers.get('location')).toBe(
      'https://kvit-mk-api.onrender.com/elsewhere',
    )
  })

  it('answers 500 with a clear message when API_ORIGIN is missing', async () => {
    const fetchMock = stubUpstream(new Response('Healthy'))

    const response = await proxyToApi(
      new Request('https://kvit-mk.pages.dev/api/health'),
      undefined,
    )

    expect(response.status).toBe(500)
    expect(await response.text()).toBe(
      'Kvit proxy is misconfigured: API_ORIGIN is not set',
    )
    expect(fetchMock).not.toHaveBeenCalled()
  })

  it.each([
    'kvit-mk-api.onrender.com',
    'ftp://kvit-mk-api.onrender.com',
    'https://kvit-mk-api.onrender.com/api',
  ])('answers 500 with a clear message when API_ORIGIN is "%s"', async (value) => {
    const fetchMock = stubUpstream(new Response('Healthy'))

    const response = await proxyToApi(
      new Request('https://kvit-mk.pages.dev/api/health'),
      value,
    )

    expect(response.status).toBe(500)
    expect(await response.text()).toBe(
      `Kvit proxy is misconfigured: API_ORIGIN must be an http(s) origin like https://kvit-mk-api.onrender.com, got "${value}"`,
    )
    expect(fetchMock).not.toHaveBeenCalled()
  })
})
