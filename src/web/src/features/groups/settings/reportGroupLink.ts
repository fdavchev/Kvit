import { supportEmail } from '@/core/legal/legalInfo'

function encodeMailtoText(text: string): string {
  return encodeURIComponent(text.replaceAll(/\r?\n/g, '\r\n'))
}

export function reportGroupLink(subject: string, body: string): string {
  return `mailto:${supportEmail}?subject=${encodeMailtoText(subject)}&body=${encodeMailtoText(body)}`
}
