import en from '@/core/i18n/locales/en.json'
import mk from '@/core/i18n/locales/mk.json'

function paragraph(language: string, text: string): HTMLParagraphElement {
  const element = document.createElement('p')
  element.lang = language
  element.className = 'text-destructive'
  element.textContent = text
  return element
}

export function renderStartupFailure(root: HTMLElement, error: unknown): void {
  console.error('Kvit could not start', error)

  const alert = document.createElement('div')
  alert.setAttribute('role', 'alert')
  alert.className = 'flex flex-col items-center gap-4 text-center'
  alert.append(
    paragraph('en', en.errors.generic),
    paragraph('mk', mk.errors.generic),
  )

  const retry = document.createElement('button')
  retry.type = 'button'
  retry.className =
    'min-h-12 rounded-lg bg-primary px-4 text-base font-medium text-primary-foreground'
  retry.textContent = `${en.common.retry} / ${mk.common.retry}`
  retry.addEventListener('click', () => window.location.reload())
  alert.append(retry)

  const page = document.createElement('main')
  page.className =
    'mx-auto flex min-h-dvh max-w-md flex-col items-center justify-center px-6'
  page.append(alert)

  root.replaceChildren(page)
}
