import { expect } from 'vitest'

export function sheetBodyOf(sheet: HTMLElement): HTMLElement {
  const body = sheet.querySelector<HTMLElement>('[data-kvit-sheet-body]')
  if (body === null) {
    throw new Error('The sheet has no scrolling body element')
  }
  return body
}

export function expectTitleOutsideScrollingBody(sheet: HTMLElement, titleText: string): void {
  const body = sheetBodyOf(sheet)
  expect(body.className.split(/\s+/)).toEqual(expect.arrayContaining(['min-h-0', 'overflow-y-auto']))
  expect(body.textContent).not.toContain(titleText)
  expect(sheet.textContent).toContain(titleText)
}

export function expectSaveOutsideScrollingFields(sheet: HTMLElement, field: HTMLElement, save: HTMLElement): void {
  const form = save.closest('form')
  if (form === null) {
    throw new Error('The Save button is not inside a form')
  }
  const fields = Array.from(form.children).find((child) => child.contains(field))
  if (fields === undefined) {
    throw new Error('The field is not inside the form')
  }
  expect(form.parentElement).toBe(sheetBodyOf(sheet))
  expect(fields.className.split(/\s+/)).toContain('in-data-kvit-sheet-body:overflow-y-auto')
  expect(fields.contains(save)).toBe(false)
}
