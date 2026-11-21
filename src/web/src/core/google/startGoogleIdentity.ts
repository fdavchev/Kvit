import { googleClientId } from './googleClientId'
import type { GoogleIdentity } from './googleIdentity'

type CredentialListener = (idToken: string) => void

let startedIdentity: GoogleIdentity | null = null
const credentialListeners: CredentialListener[] = []

export function startGoogleIdentity(identity: GoogleIdentity): void {
  if (startedIdentity === identity) {
    return
  }
  identity.initialize({
    client_id: googleClientId(),
    callback: (response) => passCredential(response.credential),
  })
  startedIdentity = identity
}

export function listenForGoogleCredential(listener: CredentialListener): () => void {
  credentialListeners.push(listener)
  return () => {
    credentialListeners.splice(credentialListeners.indexOf(listener), 1)
  }
}

function passCredential(idToken: string): void {
  const listener = credentialListeners.at(-1)
  if (listener === undefined) {
    throw new Error('Google sent a sign-in answer, but no Google button is on the page to take it')
  }
  listener(idToken)
}
