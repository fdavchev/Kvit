export interface GoogleCredentialResponse {
  credential: string
}

export interface GoogleInitializeConfig {
  client_id: string
  callback: (response: GoogleCredentialResponse) => void
}

export interface GoogleButtonOptions {
  type: 'standard'
  theme: 'outline' | 'filled_black'
  size: 'large'
  text: 'continue_with'
  shape: 'pill'
  logo_alignment: 'left'
  width: string
  locale: string
}

export interface GoogleIdentity {
  initialize: (config: GoogleInitializeConfig) => void
  renderButton: (element: HTMLElement, options: GoogleButtonOptions) => void
}

declare global {
  var google: { accounts: { id: GoogleIdentity } } | undefined
}
