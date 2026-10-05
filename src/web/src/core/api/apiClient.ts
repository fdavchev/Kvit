interface ApiErrorDetails {
  httpStatus: number | null
  errorCode?: string | null
  detail?: string | null
  cause?: unknown
}

export class ApiError extends Error {
  readonly httpStatus: number | null
  readonly errorCode: string | null
  readonly detail: string | null

  constructor(message: string, details: ApiErrorDetails) {
    super(message, { cause: details.cause })
    this.name = 'ApiError'
    this.httpStatus = details.httpStatus
    this.errorCode = details.errorCode ?? null
    this.detail = details.detail ?? null
  }
}

export const unauthorizedStatus = 401
export const forbiddenStatus = 403
export const notFoundStatus = 404

const jsonMediaTypes = ['application/json', 'application/problem+json']
const noContentStatus = 204

export function jsonRequest(method: 'POST' | 'PUT', body: unknown): RequestInit {
  return {
    method,
    headers: { 'content-type': 'application/json' },
    body: JSON.stringify(body),
  }
}

export async function apiRequest(
  path: string,
  init: RequestInit = {},
): Promise<unknown> {
  const request = `${init.method ?? 'GET'} ${path}`
  let response: Response
  try {
    response = await fetch(path, { ...init, credentials: 'same-origin' })
  } catch (error) {
    throw new ApiError(`${request} could not reach the server`, {
      httpStatus: null,
      cause: error,
    })
  }

  const body = await readBody(response, request)
  if (!response.ok) {
    throw new ApiError(`${request} failed with status ${response.status}`, {
      httpStatus: response.status,
      ...readProblemDetails(body),
    })
  }
  return body
}

async function readBody(response: Response, request: string): Promise<unknown> {
  if (response.status === noContentStatus) {
    return undefined
  }
  const mediaType = (response.headers.get('content-type') ?? '')
    .split(';')[0]
    .trim()
    .toLowerCase()
  try {
    return jsonMediaTypes.includes(mediaType)
      ? await response.json()
      : await response.text()
  } catch (error) {
    throw new ApiError(`${request} returned a body that could not be read`, {
      httpStatus: response.status,
      cause: error,
    })
  }
}

function readProblemDetails(body: unknown): {
  errorCode: string | null
  detail: string | null
} {
  if (typeof body !== 'object' || body === null) {
    return { errorCode: null, detail: null }
  }
  const { errorCode, detail } = body as Record<string, unknown>
  return {
    errorCode: typeof errorCode === 'string' ? errorCode : null,
    detail: typeof detail === 'string' ? detail : null,
  }
}
