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

export function jsonRequest(method: 'POST' | 'PUT' | 'DELETE', body: unknown): RequestInit {
  return {
    method,
    headers: { 'content-type': 'application/json' },
    body: JSON.stringify(body),
  }
}

export interface ApiFile {
  blob: Blob
  fileName: string | null
}

export async function apiRequest(
  path: string,
  init: RequestInit = {},
): Promise<unknown> {
  const request = `${init.method ?? 'GET'} ${path}`
  const response = await sendRequest(path, init, request)
  return readBody(response, request)
}

export async function apiFileRequest(path: string): Promise<ApiFile> {
  const request = `GET ${path}`
  const response = await sendRequest(path, {}, request)
  const fileName = readFileName(response, request)
  try {
    return { blob: await response.blob(), fileName }
  } catch (error) {
    throw new ApiError(`${request} returned a file that could not be read`, {
      httpStatus: response.status,
      cause: error,
    })
  }
}

async function sendRequest(path: string, init: RequestInit, request: string): Promise<Response> {
  let response: Response
  try {
    response = await fetch(path, { ...init, credentials: 'same-origin' })
  } catch (error) {
    throw new ApiError(`${request} could not reach the server`, {
      httpStatus: null,
      cause: error,
    })
  }

  if (!response.ok) {
    const body = await readBody(response, request)
    throw new ApiError(`${request} failed with status ${response.status}`, {
      httpStatus: response.status,
      ...readProblemDetails(body),
    })
  }
  return response
}

const fileNameParameter = /(?:^|;)\s*filename\s*=\s*(?:"((?:[^"\\]|\\.)*)"|([^;]*))/i

function readFileName(response: Response, request: string): string | null {
  const contentDisposition = response.headers.get('content-disposition')
  if (contentDisposition === null) {
    return null
  }
  const match = fileNameParameter.exec(contentDisposition)
  const fileName = (match?.[1]?.replace(/\\(.)/g, '$1') ?? match?.[2] ?? '').trim()
  if (fileName === '') {
    throw new ApiError(`${request} sent a Content-Disposition header without a file name: ${contentDisposition}`, {
      httpStatus: response.status,
    })
  }
  return fileName
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
