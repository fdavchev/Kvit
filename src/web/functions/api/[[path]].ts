interface Env {
  API_ORIGIN?: string
  API_PROXY_SECRET?: string
}

const apiPathPrefix = '/api/'
const visitorAddressHeader = 'cf-connecting-ip'
const proxySecretHeader = 'x-kvit-proxy-secret'
const kvitVisitorAddressHeader = 'x-kvit-visitor-ip'
const headersDroppedFromVisitor: readonly string[] = [
  'cf-connecting-ip',
  'cf-connecting-ipv6',
  'forwarded',
  'true-client-ip',
  'x-forwarded-for',
  'x-forwarded-host',
  'x-forwarded-port',
  'x-forwarded-proto',
  'x-kvit-proxy-secret',
  'x-kvit-visitor-ip',
  'x-real-ip',
]
const localHostnames: readonly string[] = ['localhost', '127.0.0.1', '[::1]']

export const onRequest: PagesFunction<Env> = ({ request, env }) =>
  proxyToApi(request, env.API_ORIGIN, env.API_PROXY_SECRET)

export async function proxyToApi(
  request: Request,
  apiOrigin: string | undefined,
  proxySecret: string | undefined,
): Promise<Response> {
  if (apiOrigin === undefined || apiOrigin.trim() === '') {
    return configurationError('API_ORIGIN is not set')
  }
  const origin: string | null = parseOrigin(apiOrigin)
  if (origin === null) {
    return configurationError(
      `API_ORIGIN must be an https origin like https://kvit-mk-api.onrender.com (http is only allowed for localhost), got "${apiOrigin}"`,
    )
  }
  if (proxySecret === undefined || proxySecret.trim() === '') {
    return configurationError('API_PROXY_SECRET is not set')
  }

  const visitorAddress: string | null = request.headers.get(visitorAddressHeader)
  if (visitorAddress === null || visitorAddress.trim() === '') {
    return configurationError(
      `the request has no ${visitorAddressHeader} header, so the visitor address is unknown`,
    )
  }

  const incomingUrl = new URL(request.url)
  const upstreamUrl = new URL(incomingUrl.pathname + incomingUrl.search, origin)
  if (
    upstreamUrl.origin !== origin ||
    !upstreamUrl.pathname.startsWith(apiPathPrefix)
  ) {
    return textResponse(
      400,
      `Kvit proxy only forwards paths that start with ${apiPathPrefix}`,
    )
  }

  const headers = new Headers(request.headers)
  for (const name of headersDroppedFromVisitor) {
    headers.delete(name)
  }
  headers.set(proxySecretHeader, proxySecret)
  headers.set(kvitVisitorAddressHeader, visitorAddress)

  const body: ArrayBuffer | null =
    request.body === null ? null : await request.arrayBuffer()

  try {
    return await fetch(
      new Request(upstreamUrl.toString(), {
        method: request.method,
        headers,
        body,
        redirect: 'manual',
      }),
    )
  } catch (error) {
    console.error('Kvit proxy: request to the API failed', error)
    return textResponse(
      502,
      'Kvit proxy could not reach the API: the request to the API server failed',
    )
  }
}

function parseOrigin(value: string): string | null {
  let url: URL
  try {
    url = new URL(value)
  } catch {
    return null
  }
  const isAllowedScheme =
    url.protocol === 'https:' ||
    (url.protocol === 'http:' && localHostnames.includes(url.hostname))
  const isOriginOnly = url.pathname === '/' && url.search === '' && url.hash === ''
  return isAllowedScheme && isOriginOnly ? url.origin : null
}

function configurationError(message: string): Response {
  return textResponse(500, `Kvit proxy is misconfigured: ${message}`)
}

function textResponse(status: number, message: string): Response {
  return new Response(message, {
    status,
    headers: { 'content-type': 'text/plain; charset=utf-8' },
  })
}
