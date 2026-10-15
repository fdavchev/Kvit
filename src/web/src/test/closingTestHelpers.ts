import { screen, within } from '@testing-library/react'
import type { Language } from '@/core/i18n/language'
import type { Group } from '@/core/services/groups/groupsService'
import type { GroupAppOptions } from './activityTestHelpers'
import { textOf, type ClosingText } from './closingTestData'
import { plainSpaces } from './expenseTestHelpers'
import { testGroup } from './groupTestData'
import { ownerViewMembers } from './memberTestData'
import type { AnswerFactory } from './requestTestHelpers'
import { anaSignedIn } from './settlementActionsTestData'

export interface LiveGroupState {
  group: Group
}

export function ownerView(group: Group, language: Language = 'en'): GroupAppOptions {
  return { group, members: ownerViewMembers, language }
}

export function anaView(group: Group, language: Language = 'en'): GroupAppOptions {
  return anaSignedIn({ group: { ...group, isOwner: false }, language })
}

export function liveGroupAnswer(live: LiveGroupState): AnswerFactory {
  return () => Response.json(live.group)
}

export async function showsGroupScreen(): Promise<void> {
  await screen.findByRole('heading', { level: 1, name: new RegExp(`^${testGroup.name}`) })
}

export async function findClosingBar(
  title: string,
  language: Language = 'en',
): Promise<HTMLElement> {
  const titleElement = await screen.findByText(title, { normalizer: plainSpaces })
  let node: HTMLElement | null = titleElement
  while (node !== null && node.tagName !== 'MAIN') {
    if (node.hasAttribute('aria-label') || node.hasAttribute('aria-labelledby')) {
      return node
    }
    node = node.parentElement
  }
  throw new Error(
    `The text "${title}" (${language}) is not inside an element with an accessible name (aria-label or aria-labelledby)`,
  )
}

export async function findBarTitled(
  text: ClosingText,
  language: Language = 'en',
  params: Record<string, string> = {},
): Promise<HTMLElement> {
  return findClosingBar(textOf(language, text, params), language)
}

export function buttonLabelsIn(container: HTMLElement): string[] {
  return within(container)
    .queryAllByRole('button')
    .map((button) => plainSpaces(button.textContent ?? '').trim())
}

export function buttonIn(container: HTMLElement, name: string): HTMLElement {
  return within(container).getByRole('button', { name })
}

export function plainTextOf(container: HTMLElement): string {
  return plainSpaces(container.textContent ?? '')
}

export function ancestorLooks(element: Element): string[] {
  const looks: string[] = []
  for (
    let node = element.parentElement;
    node !== null && node !== document.body;
    node = node.parentElement
  ) {
    looks.push(`${node.tagName}|${node.className}|${node.getAttribute('style') ?? ''}`)
  }
  return looks
}
