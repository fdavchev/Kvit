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

describe('proxyToApi when the Render server is asleep', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  function forwardTo(upstream: Response, origin: string = apiOrigin): Promise<Response> {
    stubUpstream(upstream)
    return proxyToApi(visitorRequest('https://kvit-mk.pages.dev/api/me'), origin, proxySecret)
  }

  function renderHibernating(routing: string = 'hibernate-rate-limited'): Response {
    return new Response('Too Many Requests', {
      status: 429,
      headers: { 'content-type': 'text/plain', 'x-render-routing': routing },
    })
  }

  function cloudflareChallenge(): Response {
    return new Response('<!DOCTYPE html><title>Just a moment...</title>', {
      status: 429,
      headers: { 'content-type': 'text/html; charset=UTF-8', 'cf-mitigated': 'challenge' },
    })
  }

  it('answers 503 instead of Render\'s 429 with x-render-routing: hibernate-rate-limited', async () => {
    const response = await forwardTo(renderHibernating())

    expect(response.status).toBe(503)
  })

  it.each(['hibernate-rate-limited', 'hibernate-wake-error', 'HIBERNATE-rate-limited', 'hibernate'])(
    'treats the x-render-routing value %s as asleep',
    async (routing) => {
      const response = await forwardTo(renderHibernating(routing))

      expect(response.status).toBe(503)
      expect(response.headers.get('x-kvit-server-asleep')).toBe('1')
    },
  )

  it('answers 503 instead of the Cloudflare robot check (429 with cf-mitigated: challenge)', async () => {
    const response = await forwardTo(cloudflareChallenge())

    expect(response.status).toBe(503)
    expect(response.headers.get('x-kvit-server-asleep')).toBe('1')
  })

  it('marks the 503 as the server being asleep and names the wake address on the API origin', async () => {
    const response = await forwardTo(renderHibernating())

    expect(response.headers.get('x-kvit-server-asleep')).toBe('1')
    expect(response.headers.get('x-kvit-wake-url')).toBe('https://kvit-mk-api.onrender.com/wake')
  })

  it('builds the wake address from API_ORIGIN even when API_ORIGIN ends with a slash', async () => {
    const response = await forwardTo(renderHibernating(), 'https://kvit-mk-api.onrender.com/')

    expect(response.headers.get('x-kvit-wake-url')).toBe('https://kvit-mk-api.onrender.com/wake')
  })

  it('answers the asleep 503 as plain text that is never cached', async () => {
    const response = await forwardTo(cloudflareChallenge())

    expect(response.headers.get('content-type')).toBe('text/plain; charset=utf-8')
    expect(response.headers.get('cache-control')).toBe('no-store')
    expect(await response.text()).not.toContain('Just a moment')
  })

  it('does not pass on Render\'s own headers or the robot check page', async () => {
    const response = await forwardTo(cloudflareChallenge())

    expect(response.headers.get('cf-mitigated')).toBeNull()
    expect(response.headers.get('x-render-routing')).toBeNull()
  })

  it('passes Kvit\'s own 429 rate-limit answer through untouched, with its body and Retry-After', async () => {
    const problem = '{"title":"Too Many Requests","status":429,"errorCode":"RATE_LIMITED"}'
    const upstream = new Response(problem, {
      status: 429,
      headers: { 'content-type': 'application/problem+json', 'retry-after': '42' },
    })

    const response = await forwardTo(upstream)

    expect(response).toBe(upstream)
    expect(response.status).toBe(429)
    expect(response.headers.get('retry-after')).toBe('42')
    expect(response.headers.get('x-kvit-server-asleep')).toBeNull()
    expect(await response.text()).toBe(problem)
  })

  it.each([
    ['an x-render-routing value that does not start with hibernate', { 'x-render-routing': 'dynamic-rate-limited' }],
    ['a cf-mitigated value other than challenge', { 'cf-mitigated': 'block' }],
  ])('passes a 429 with %s through untouched', async (_case, headers) => {
    const upstream = new Response('Too Many Requests', { status: 429, headers })

    const response = await forwardTo(upstream)

    expect(response).toBe(upstream)
    expect(response.status).toBe(429)
  })

  it.each([200, 403, 500, 503])('passes a %i answer with x-render-routing: hibernate-rate-limited through untouched', async (status) => {
    const upstream = new Response(null, { status, headers: { 'x-render-routing': 'hibernate-rate-limited' } })

    const response = await forwardTo(upstream)

    expect(response).toBe(upstream)
    expect(response.headers.get('x-kvit-server-asleep')).toBeNull()
  })
})
