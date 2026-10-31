export function describingElementsOf(field: HTMLElement): HTMLElement[] {
  const ids = (field.getAttribute('aria-describedby') ?? '').split(/\s+/).filter((id) => id !== '')
  return ids.map((id) => {
    const element = field.ownerDocument.getElementById(id)
    if (element === null) {
      throw new Error(`The field points with aria-describedby to "${id}", but no element has that id`)
    }
    return element
  })
}

export function isDescribedBy(field: HTMLElement, message: HTMLElement): boolean {
  return describingElementsOf(field).some((element) => element === message || element.contains(message))
}
