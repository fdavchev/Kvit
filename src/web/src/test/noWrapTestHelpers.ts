import { expect } from 'vitest'
import { plainSpaces } from './expenseTestHelpers'

export const macedonianVerbPhrase = 'платил/а'

export function noWrapElementHolding(container: HTMLElement, text: string): HTMLElement {
  const holders = Array.from(container.querySelectorAll<HTMLElement>('.whitespace-nowrap')).filter((element) =>
    (element.textContent ?? '').includes(text),
  )
  if (holders.length === 0) {
    throw new Error(
      `Found no element with the class whitespace-nowrap that holds "${text}" in the text: ${container.textContent}`,
    )
  }
  return holders[holders.length - 1]
}

export function expectOnlyTheVerbPhraseDoesNotWrap(container: HTMLElement, sentence: string): void {
  const verb = noWrapElementHolding(container, macedonianVerbPhrase)
  const sentenceElement = verb.closest('p')

  expect(sentenceElement).not.toBeNull()
  expect(plainSpaces(sentenceElement?.textContent ?? '')).toBe(plainSpaces(sentence))
  expect(plainSpaces(verb.textContent ?? '')).not.toBe(plainSpaces(sentence))
}
