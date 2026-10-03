import type { GoogleIdentity } from './googleIdentity'

const googleScriptUrl = 'https://accounts.google.com/gsi/client'

let loading: Promise<GoogleIdentity> | null = null

export function loadGoogleIdentity(): Promise<GoogleIdentity> {
  const identity = globalThis.google?.accounts.id
  if (identity !== undefined) {
    return Promise.resolve(identity)
  }
  loading ??= addGoogleScript()
  return loading
}

function addGoogleScript(): Promise<GoogleIdentity> {
  return new Promise<GoogleIdentity>((resolve, reject) => {
    const script = document.createElement('script')
    script.src = googleScriptUrl
    script.async = true

    function fail(error: Error): void {
      script.remove()
      loading = null
      reject(error)
    }

    script.addEventListener('load', () => {
      const identity = globalThis.google?.accounts.id
      if (identity === undefined) {
        fail(new Error(`${googleScriptUrl} loaded but did not define google.accounts.id`))
        return
      }
      resolve(identity)
    })
    script.addEventListener('error', () => {
      fail(new Error(`Could not load the Google sign-in script from ${googleScriptUrl}`))
    })
    document.head.appendChild(script)
  })
}
