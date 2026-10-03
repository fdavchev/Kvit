export interface GoogleProfile {
  name: string
  email: string
}

const tokenPartCount = 3

export function readGoogleProfile(idToken: string): GoogleProfile {
  const parts = idToken.split('.')
  if (parts.length !== tokenPartCount) {
    throw new Error(
      `Expected a Google ID token of ${tokenPartCount} dot-separated parts, got ${parts.length}`,
    )
  }
  const claims = decodePayload(parts[1])
  return { name: readClaim(claims, 'name'), email: readClaim(claims, 'email') }
}

function decodePayload(payload: string): object {
  const base64 = payload.replaceAll('-', '+').replaceAll('_', '/')
  const padded = base64.padEnd(Math.ceil(base64.length / 4) * 4, '=')
  let binary: string
  try {
    binary = atob(padded)
  } catch (error) {
    throw new Error('The Google ID token payload is not valid base64url', { cause: error })
  }
  const bytes = Uint8Array.from(binary, (character) => character.charCodeAt(0))
  const text = new TextDecoder('utf-8', { fatal: true }).decode(bytes)
  let claims: unknown
  try {
    claims = JSON.parse(text)
  } catch (error) {
    throw new Error('The Google ID token payload is not JSON', { cause: error })
  }
  if (typeof claims !== 'object' || claims === null) {
    throw new Error(`The Google ID token payload is not a JSON object, got ${typeof claims}`)
  }
  return claims
}

function readClaim(claims: object, name: keyof GoogleProfile): string {
  const value: unknown = Reflect.get(claims, name)
  if (typeof value !== 'string') {
    throw new Error(`The Google ID token has no "${name}" text, got ${typeof value}`)
  }
  return value
}
