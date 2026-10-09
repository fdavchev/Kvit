import { screen } from '@testing-library/react'
import { expect } from 'vitest'
import type { Language } from '@/core/i18n/language'
import { translated } from './translated'

export const receiptEmoji: string = '\u{1F9FE}'

const oldAddPeopleHeadings: Record<Language, string> = {
  en: 'Add people',
  mk: 'Додај луѓе',
}

const oldSingleEmptySentences: Record<Language, string> = {
  en: 'No expenses yet. Tap + to add the first one.',
  mk: 'Сѐ уште нема трошоци. Допри + за да додадеш.',
}

export function emptyTitle(language: Language = 'en'): HTMLElement {
  return screen.getByText(translated(language, 'expenses.emptyTitle'))
}

export function emptyHint(language: Language = 'en'): HTMLElement {
  return screen.getByText(translated(language, 'expenses.emptyHint'))
}

export function addNameButton(language: Language = 'en'): HTMLElement {
  return screen.getByRole('button', { name: translated(language, 'group.addName') })
}

export function shareLinkButton(language: Language = 'en'): HTMLElement {
  return screen.getByRole('button', { name: translated(language, 'group.shareLink') })
}

export function noKvitLine(language: Language = 'en'): HTMLElement {
  return screen.getByText(translated(language, 'group.addPeopleNoKvit'))
}

export function hasKvitLine(language: Language = 'en'): HTMLElement {
  return screen.getByText(translated(language, 'group.addPeopleHasKvit'))
}

export async function findEmptyTitle(language: Language = 'en'): Promise<HTMLElement> {
  return screen.findByText(translated(language, 'expenses.emptyTitle'))
}

export async function findAddNameButton(language: Language = 'en'): Promise<HTMLElement> {
  return screen.findByRole('button', { name: translated(language, 'group.addName') })
}

export function expectEmptyStateTexts(language: Language = 'en'): void {
  expect(screen.getAllByText(receiptEmoji)).toHaveLength(1)
  expect(emptyTitle(language)).toBeTruthy()
  expect(emptyHint(language)).toBeTruthy()
}

export function expectNoEmptyStateTexts(language: Language = 'en'): void {
  expect(screen.queryByText(receiptEmoji)).toBeNull()
  expect(screen.queryByText(translated(language, 'expenses.emptyTitle'))).toBeNull()
  expect(screen.queryByText(translated(language, 'expenses.emptyHint'))).toBeNull()
  expect(screen.queryByText(oldSingleEmptySentences[language])).toBeNull()
}

export function expectNoAddPeopleActions(language: Language = 'en'): void {
  expect(screen.queryByRole('button', { name: translated(language, 'group.addName') })).toBeNull()
  expect(screen.queryByRole('button', { name: translated(language, 'group.shareLink') })).toBeNull()
  expect(screen.queryByText(translated(language, 'group.addPeopleNoKvit'))).toBeNull()
  expect(screen.queryByText(translated(language, 'group.addPeopleHasKvit'))).toBeNull()
  expect(screen.queryByText(oldAddPeopleHeadings[language])).toBeNull()
}

export function expectNoAddPeopleHeading(language: Language = 'en'): void {
  expect(screen.queryByRole('heading', { name: oldAddPeopleHeadings[language] })).toBeNull()
  expect(screen.queryByText(oldAddPeopleHeadings[language])).toBeNull()
}

export function lowestCommonAncestor(first: HTMLElement, second: HTMLElement): HTMLElement {
  let node: HTMLElement | null = first
  while (node !== null && !node.contains(second)) {
    node = node.parentElement
  }
  if (node === null) {
    throw new Error('The two elements have no common ancestor')
  }
  return node
}

export function ancestorsBelow(element: HTMLElement, stop: HTMLElement): HTMLElement[] {
  const ancestors: HTMLElement[] = []
  let node: HTMLElement | null = element.parentElement
  while (node !== null && node !== stop) {
    ancestors.push(node)
    node = node.parentElement
  }
  if (node === null) {
    throw new Error('The stop element is not an ancestor of the element')
  }
  return ancestors
}

const boldClasses: readonly string[] = ['font-bold', 'font-extrabold', 'font-black']

const largestSmallTextInRem = 0.875

export function isBold(element: HTMLElement): boolean {
  return boldClasses.some((boldClass) => element.classList.contains(boldClass))
}

export function isCentred(element: HTMLElement, stop: HTMLElement): boolean {
  return [element, ...ancestorsBelow(element, stop)].some((candidate) =>
    candidate.classList.contains('text-center'),
  )
}

function isSmallTextClass(className: string): boolean {
  if (className === 'text-xs' || className === 'text-sm') {
    return true
  }
  const sizeInRem: RegExpExecArray | null = /^text-\[(\d*\.?\d+)rem\]$/.exec(className)
  return sizeInRem !== null && Number(sizeInRem[1]) <= largestSmallTextInRem
}

export function isSmallText(element: HTMLElement, stop: HTMLElement): boolean {
  return [element, ...ancestorsBelow(element, stop)].some((candidate) =>
    Array.from(candidate.classList).some(isSmallTextClass),
  )
}

