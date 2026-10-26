export function isChecked(element: HTMLElement): boolean {
  if (element instanceof HTMLInputElement) {
    return element.checked
  }
  return element.getAttribute('aria-checked') === 'true'
}
