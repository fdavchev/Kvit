interface Env {
  API_ORIGIN?: string
}

const methodsWithoutBody = ['GET', 'HEAD']

export const onRequest: PagesFunction<Env> = ({ request, env }) =>
  proxyToApi(request, env.API_ORIGIN)

export async function proxyToApi(
  request: Request,
  apiOrigin: string | undefined,
): Promise<Response> {
  if (apiOrigin === undefined || apiOrigin.trim() === '') {
    return configurationError('API_ORIGIN is not set')
  }
  const origin = parseOrigin(apiOrigin)
  if (origin === null) {
    return configurationError(
      `API_ORIGIN must be an http(s) origin like https://kvit-mk-api.onrender.com, got "${apiOrigin}"`,
    )
  }

  const incomingUrl = new URL(request.url)
  const upstreamUrl = new URL(incomingUrl.pathname + incomingUrl.search, origin)
  const body = methodsWithoutBody.includes(request.method)
    ? null
    : await request.arrayBuffer()

  return fetch(
    new Request(upstreamUrl.toString(), {
      method: request.method,
      headers: request.headers,
      body,
      redirect: 'manual',
    }),
  )
}

function parseOrigin(value: string): string | null {
  let url: URL
  try {
    url = new URL(value)
  } catch {
    return null
  }
  const isHttp = url.protocol === 'https:' || url.protocol === 'http:'
  const isOriginOnly = url.pathname === '/' && url.search === '' && url.hash === ''
  return isHttp && isOriginOnly ? url.origin : null
}

function configurationError(message: string): Response {
  return new Response(`Kvit proxy is misconfigured: ${message}`, {
    status: 500,
    headers: { 'content-type': 'text/plain; charset=utf-8' },
  })
}
