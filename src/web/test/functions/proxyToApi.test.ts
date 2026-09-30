import { afterEach, describe, expect, it, vi } from 'vitest'
import { onRequest, proxyToApi } from '../../functions/api/[[path]]'

const apiOrigin = 'https://kvit-mk-api.onrender.com'
const visitorAddress = '203.0.113.9'
const proxySecret = 'shared-secret-known-to-the-api'

function visitorRequest(url: string, init: RequestInit = {}): Request {
  const headers = new Headers(init.headers)
  if (!headers.has('cf-connecting-ip')) {
    headers.set('cf-connecting-ip', visitorAddress)
  }
  return new Request(url, { ...init, headers })
}

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
      visitorRequest('https://kvit-mk.pages.dev/api/groups/7/expenses?page=2&size=20'),
      apiOrigin,
      proxySecret,
    )

    expect(forwardedRequest(fetchMock).url).toBe(
      'https://kvit-mk-api.onrender.com/api/groups/7/expenses?page=2&size=20',
    )
  })

  it('forwards the method, headers and body', async () => {
    const fetchMock = stubUpstream(new Response(null, { status: 204 }))

    await proxyToApi(
      visitorRequest('https://kvit-mk.pages.dev/api/login', {
        method: 'POST',
        headers: { 'content-type': 'application/json', cookie: 'kvit=abc' },
        body: JSON.stringify({ email: 'ana@example.com' }),
      }),
      apiOrigin,
      proxySecret,
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
      visitorRequest('https://kvit-mk.pages.dev/api/groups', { method: 'POST' }),
      apiOrigin,
      proxySecret,
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
      visitorRequest('https://kvit-mk.pages.dev/api/health'),
      apiOrigin,
      proxySecret,
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
      visitorRequest('https://kvit-mk.pages.dev/api/health'),
      undefined,
      proxySecret,
    )

    expect(response.status).toBe(500)
    expect(await response.text()).toBe(
      'Kvit proxy is misconfigured: API_ORIGIN is not set',
    )
    expect(fetchMock).not.toHaveBeenCalled()
  })

  it.each([undefined, '', '   '])(
    'answers 500 with a clear message when API_PROXY_SECRET is %j',
    async (missingSecret) => {
      const fetchMock = stubUpstream(new Response('Healthy'))

      const response = await proxyToApi(
        visitorRequest('https://kvit-mk.pages.dev/api/health'),
        apiOrigin,
        missingSecret,
      )

      expect(response.status).toBe(500)
      expect(await response.text()).toBe(
        'Kvit proxy is misconfigured: API_PROXY_SECRET is not set',
      )
      expect(fetchMock).not.toHaveBeenCalled()
    },
  )

  it('reports a missing API_ORIGIN before a missing API_PROXY_SECRET', async () => {
    const response = await proxyToApi(
      visitorRequest('https://kvit-mk.pages.dev/api/health'),
      undefined,
      undefined,
    )

    expect(await response.text()).toBe(
      'Kvit proxy is misconfigured: API_ORIGIN is not set',
    )
  })

  it('reports a missing API_PROXY_SECRET before a missing cf-connecting-ip', async () => {
    const response = await proxyToApi(
      new Request('https://kvit-mk.pages.dev/api/health'),
      apiOrigin,
      undefined,
    )

    expect(await response.text()).toBe(
      'Kvit proxy is misconfigured: API_PROXY_SECRET is not set',
    )
  })

  it.each([
    'kvit-mk-api.onrender.com',
    'ftp://kvit-mk-api.onrender.com',
    'https://kvit-mk-api.onrender.com/api',
    'http://kvit-mk-api.onrender.com',
  ])('answers 500 with a clear message when API_ORIGIN is "%s"', async (value) => {
    const fetchMock = stubUpstream(new Response('Healthy'))

    const response = await proxyToApi(
      visitorRequest('https://kvit-mk.pages.dev/api/health'),
      value,
      proxySecret,
    )

    expect(response.status).toBe(500)
    expect(await response.text()).toBe(
      `Kvit proxy is misconfigured: API_ORIGIN must be an https origin like https://kvit-mk-api.onrender.com (http is only allowed for localhost), got "${value}"`,
    )
    expect(fetchMock).not.toHaveBeenCalled()
  })

  it.each(['http://localhost:5018', 'http://127.0.0.1:5018'])(
    'allows plain http for the local API_ORIGIN "%s"',
    async (value) => {
      const fetchMock = stubUpstream(new Response('Healthy'))

      const response = await proxyToApi(
        visitorRequest('https://kvit-mk.pages.dev/api/health'),
        value,
        proxySecret,
      )

      expect(response.status).toBe(200)
      expect(forwardedRequest(fetchMock).url).toBe(`${value}/api/health`)
    },
  )

  it('sends the proxy secret and the cf-connecting-ip value in the two Kvit headers and sets no x-forwarded-for', async () => {
    const fetchMock = stubUpstream(new Response('Healthy'))

    await proxyToApi(
      visitorRequest('https://kvit-mk.pages.dev/api/health'),
      apiOrigin,
      proxySecret,
    )

    const forwarded = forwardedRequest(fetchMock)
    expect(forwarded.headers.get('x-kvit-proxy-secret')).toBe(proxySecret)
    expect(forwarded.headers.get('x-kvit-visitor-ip')).toBe(visitorAddress)
    expect(forwarded.headers.get('x-forwarded-for')).toBeNull()
  })

  it('replaces visitor-sent x-kvit-proxy-secret and x-kvit-visitor-ip with its own values', async () => {
    const fetchMock = stubUpstream(new Response('Healthy'))

    await proxyToApi(
      visitorRequest('https://kvit-mk.pages.dev/api/health', {
        headers: {
          'x-kvit-proxy-secret': 'guessed-by-the-visitor',
          'x-kvit-visitor-ip': '1.2.3.4',
        },
      }),
      apiOrigin,
      proxySecret,
    )

    const forwarded = forwardedRequest(fetchMock)
    expect(forwarded.headers.get('x-kvit-proxy-secret')).toBe(proxySecret)
    expect(forwarded.headers.get('x-kvit-visitor-ip')).toBe(visitorAddress)
  })

  it('drops client-supplied forwarding headers and keeps the other headers', async () => {
    const fetchMock = stubUpstream(new Response('Healthy'))

    await proxyToApi(
      visitorRequest('https://kvit-mk.pages.dev/api/health', {
        headers: {
          'cf-connecting-ip': visitorAddress,
          'cf-connecting-ipv6': '2001:db8::1',
          forwarded: 'for=1.2.3.4;host=evil.example;proto=http',
          'true-client-ip': '1.2.3.4',
          'x-forwarded-for': '1.2.3.4',
          'x-forwarded-host': 'evil.example',
          'x-forwarded-port': '80',
          'x-forwarded-proto': 'http',
          'x-real-ip': '1.2.3.4',
          'x-request-id': 'keep-me',
        },
      }),
      apiOrigin,
      proxySecret,
    )

    const forwarded = forwardedRequest(fetchMock)
    expect(forwarded.headers.get('x-request-id')).toBe('keep-me')
    for (const name of [
      'cf-connecting-ip',
      'cf-connecting-ipv6',
      'forwarded',
      'true-client-ip',
      'x-forwarded-for',
      'x-forwarded-host',
      'x-forwarded-port',
      'x-forwarded-proto',
      'x-real-ip',
    ]) {
      expect(forwarded.headers.get(name), name).toBeNull()
    }
  })

  it('answers 500 with a clear message when cf-connecting-ip is missing, even if the visitor sent x-kvit-visitor-ip', async () => {
    const fetchMock = stubUpstream(new Response('Healthy'))

    const response = await proxyToApi(
      new Request('https://kvit-mk.pages.dev/api/health', {
        headers: { 'x-kvit-visitor-ip': '1.2.3.4', 'x-forwarded-for': '1.2.3.4' },
      }),
      apiOrigin,
      proxySecret,
    )

    expect(response.status).toBe(500)
    expect(await response.text()).toBe(
      'Kvit proxy is misconfigured: the request has no cf-connecting-ip header, so the visitor address is unknown',
    )
    expect(fetchMock).not.toHaveBeenCalled()
  })

  it.each([
    'https://kvit-mk.pages.dev/api/%2e%2e/health',
    'https://kvit-mk.pages.dev/api/../health',
    'https://kvit-mk.pages.dev/api\\..\\health',
    'https://kvit-mk.pages.dev//evil.example/x',
    'https://kvit-mk.pages.dev/health',
    'https://kvit-mk.pages.dev/api',
    'https://kvit-mk.pages.dev/apis/groups',
  ])('rejects "%s" with 400 before calling the API', async (url) => {
    const fetchMock = stubUpstream(new Response('Healthy'))

    const response = await proxyToApi(visitorRequest(url), apiOrigin, proxySecret)

    expect(response.status).toBe(400)
    expect(await response.text()).toBe(
      'Kvit proxy only forwards paths that start with /api/',
    )
    expect(fetchMock).not.toHaveBeenCalled()
  })

  it('keeps a double slash inside an /api/ path on the API host', async () => {
    const fetchMock = stubUpstream(new Response('Healthy'))

    await proxyToApi(
      visitorRequest('https://kvit-mk.pages.dev/api//evil.example/x'),
      apiOrigin,
      proxySecret,
    )

    expect(forwardedRequest(fetchMock).url).toBe(
      'https://kvit-mk-api.onrender.com/api//evil.example/x',
    )
  })

  it('answers 502 with a clear message when the API cannot be reached', async () => {
    const cause = new TypeError('fetch failed')
    vi.stubGlobal(
      'fetch',
      vi.fn(async () => {
        throw cause
      }),
    )
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {})

    const response = await proxyToApi(
      visitorRequest('https://kvit-mk.pages.dev/api/health'),
      apiOrigin,
      proxySecret,
    )

    expect(response.status).toBe(502)
    expect(response.headers.get('content-type')).toBe('text/plain; charset=utf-8')
    expect(await response.text()).toBe(
      'Kvit proxy could not reach the API: the request to the API server failed',
    )
    expect(consoleError).toHaveBeenCalledWith(
      'Kvit proxy: request to the API failed',
      cause,
    )
    consoleError.mockRestore()
  })

  it.each(['DELETE', 'PUT'])(
    'forwards a %s with no body as a request with no body',
    async (method) => {
      const fetchMock = stubUpstream(new Response(null, { status: 204 }))

      const response = await proxyToApi(
        visitorRequest('https://kvit-mk.pages.dev/api/groups/7', { method }),
        apiOrigin,
        proxySecret,
      )

      const forwarded = forwardedRequest(fetchMock)
      expect(response.status).toBe(204)
      expect(forwarded.method).toBe(method)
      expect(forwarded.body).toBeNull()
    },
  )

  it('forwards a PUT that has a body', async () => {
    const fetchMock = stubUpstream(new Response(null, { status: 204 }))

    await proxyToApi(
      visitorRequest('https://kvit-mk.pages.dev/api/groups/7', {
        method: 'PUT',
        body: '{"name":"Greece"}',
      }),
      apiOrigin,
      proxySecret,
    )

    expect(await forwardedRequest(fetchMock).text()).toBe('{"name":"Greece"}')
  })
})

describe('onRequest', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  function context(
    request: Request,
    env: { API_ORIGIN?: string; API_PROXY_SECRET?: string },
  ) {
    return { request, env } as unknown as Parameters<typeof onRequest>[0]
  }

  it('forwards the request to the API_ORIGIN with the API_PROXY_SECRET from the Cloudflare environment', async () => {
    const fetchMock = stubUpstream(new Response('Healthy'))

    const response = await onRequest(
      context(visitorRequest('https://kvit-mk.pages.dev/api/health'), {
        API_ORIGIN: apiOrigin,
        API_PROXY_SECRET: proxySecret,
      }),
    )

    const forwarded = forwardedRequest(fetchMock)
    expect(response.status).toBe(200)
    expect(forwarded.url).toBe('https://kvit-mk-api.onrender.com/api/health')
    expect(forwarded.headers.get('x-kvit-proxy-secret')).toBe(proxySecret)
  })

  it('answers 500 when the Cloudflare environment has no API_ORIGIN', async () => {
    const fetchMock = stubUpstream(new Response('Healthy'))

    const response = await onRequest(
      context(visitorRequest('https://kvit-mk.pages.dev/api/health'), {
        API_PROXY_SECRET: proxySecret,
      }),
    )

    expect(response.status).toBe(500)
    expect(fetchMock).not.toHaveBeenCalled()
  })

  it('answers 500 when the Cloudflare environment has no API_PROXY_SECRET', async () => {
    const fetchMock = stubUpstream(new Response('Healthy'))

    const response = await onRequest(
      context(visitorRequest('https://kvit-mk.pages.dev/api/health'), {
        API_ORIGIN: apiOrigin,
      }),
    )

    expect(response.status).toBe(500)
    expect(await response.text()).toBe(
      'Kvit proxy is misconfigured: API_PROXY_SECRET is not set',
    )
    expect(fetchMock).not.toHaveBeenCalled()
  })
})
