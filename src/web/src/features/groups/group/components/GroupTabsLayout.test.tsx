import { screen, within } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { languages, type Language } from '@/core/i18n/language'
import { renderElementWithProviders } from '@/test/renderWithProviders'
import { translated } from '@/test/translated'
import { GroupTabs, type GroupTab } from './GroupTabs'

interface TabCase {
  tab: GroupTab
  titleKey: string
}

const groupId = 'group-1'
const path = `/groups/${groupId}`

const tabCases: readonly TabCase[] = [
  { tab: 'expenses', titleKey: 'expenses.title' },
  { tab: 'balances', titleKey: 'balances.title' },
  { tab: 'activity', titleKey: 'activity.title' },
  { tab: 'totals', titleKey: 'groupTotals.tab' },
]

const allowedPaddingClasses: readonly string[] = ['px-0', 'px-0.5', 'px-px']
const classesThatCutOrHideText: readonly string[] = [
  'truncate',
  'text-ellipsis',
  'text-clip',
  'overflow-hidden',
  'break-words',
  'wrap-anywhere',
  'whitespace-normal',
]
const unprefixedTextSizeClass = /^text-(xs|sm|base|lg|xl|\d+xl|\[.+\])$/
const paddingClass = /^(p|px|pl|pr)-/

async function renderTabs(currentTab: GroupTab, language: Language = 'en'): Promise<void> {
  await renderElementWithProviders(<GroupTabs groupId={groupId} currentTab={currentTab} />, path, { language })
}

function tabLink(titleKey: string, language: Language): HTMLElement {
  return screen.getByRole('link', { name: translated(language, titleKey) })
}

function classesOf(element: HTMLElement): string[] {
  return Array.from(element.classList)
}

function pill(): HTMLElement {
  const found = document.querySelector<HTMLElement>('[data-tab-pill]')
  if (found === null) {
    throw new Error('The group tab bar has no sliding pill')
  }
  return found
}

describe('GroupTabs layout, the four equal cells', () => {
  describe.each(languages)('in %s', (language) => {
    it.each(tabCases)('lets the $tab tab shrink to its cell, with min-w-0 next to flex-1, so a long label cannot widen it', async ({ tab, titleKey }) => {
      await renderTabs(tab, language)

      expect(classesOf(tabLink(titleKey, language))).toEqual(expect.arrayContaining(['min-w-0', 'flex-1']))
    })

    it.each(tabCases)('gives the $tab tab at most px-0.5 of horizontal padding', async ({ tab, titleKey }) => {
      await renderTabs(tab, language)

      const paddings = classesOf(tabLink(titleKey, language)).filter((name) => paddingClass.test(name.split(':').at(-1) ?? ''))

      for (const padding of paddings) {
        expect(allowedPaddingClasses, `the class ${padding} is wider than px-0.5`).toContain(padding)
      }
    })

    it.each(tabCases)('writes the label of the $tab tab at 11 px under 360 px, 12 px from 360 px and 16 px from 480 px wide', async ({ tab, titleKey }) => {
      await renderTabs(tab, language)

      const classes = classesOf(tabLink(titleKey, language))

      expect(classes).toContain('text-[11px]')
      expect(classes).toContain('min-[360px]:text-xs')
      expect(classes).toContain('min-[480px]:text-base')
      expect(classes.filter((name) => unprefixedTextSizeClass.test(name))).toEqual(['text-[11px]'])
    })

    it.each(tabCases)('uses no other text size on the $tab tab than the three breakpoint sizes', async ({ tab, titleKey }) => {
      await renderTabs(tab, language)

      const sizeClasses = classesOf(tabLink(titleKey, language)).filter((name) => /(^|:)text-(xs|sm|base|lg|xl|\d+xl|\[\d+px\])$/.test(name))

      expect(sizeClasses.sort()).toEqual(['min-[360px]:text-xs', 'min-[480px]:text-base', 'text-[11px]'])
    })

    it.each(tabCases)('centres the label of the $tab tab and keeps it on one line', async ({ tab, titleKey }) => {
      await renderTabs(tab, language)

      expect(classesOf(tabLink(titleKey, language))).toEqual(expect.arrayContaining(['text-center', 'whitespace-nowrap']))
    })

    it.each(tabCases)('does not cut, hide or break the label of the $tab tab', async ({ tab, titleKey }) => {
      await renderTabs(tab, language)

      const classes = classesOf(tabLink(titleKey, language))

      for (const forbidden of classesThatCutOrHideText) {
        expect(classes, `the ${tab} tab must not carry ${forbidden}`).not.toContain(forbidden)
      }
    })

    it.each(tabCases)('keeps the $tab tab at least 44 px high', async ({ tab, titleKey }) => {
      await renderTabs(tab, language)

      expect(classesOf(tabLink(titleKey, language))).toContain('min-h-11')
    })

    it('gives all four tabs the same classes, so the cells are equal', async () => {
      await renderTabs('expenses', language)

      const bar = tabLink('expenses.title', language).parentElement
      const classLists = within(bar ?? document.body)
        .getAllByRole('link')
        .map((link) => classesOf(link).sort().join(' '))

      expect(new Set(classLists).size).toBe(1)
    })

    it('shows the four tabs in the order Expenses, Balances, Activity, Totals', async () => {
      await renderTabs('expenses', language)

      const bar = tabLink('expenses.title', language).parentElement
      const labels = within(bar ?? document.body)
        .getAllByRole('link')
        .map((link) => link.textContent)

      expect(labels).toEqual(tabCases.map(({ titleKey }) => translated(language, titleKey)))
    })

    it.each(tabCases.map((current, index) => ({ ...current, index })))('marks only the $tab tab as the current page and puts the pill under it', async ({ tab, index }) => {
      await renderTabs(tab, language)

      const current = tabCases.filter(({ titleKey }) => tabLink(titleKey, language).getAttribute('aria-current') === 'page')
      expect(current.map((item) => item.tab)).toEqual([tab])
      expect(pill().style.transform).toBe(`translateX(${index * 100}%)`)
    })
  })

  it('keeps the pill one quarter of the bar wide inside its padding', async () => {
    await renderTabs('balances')
    const oneQuarterInsidePadding = document.createElement('span').style
    oneQuarterInsidePadding.width = 'calc((100% - 0.5rem) / 4)'

    expect(pill().style.width).toBe(oneQuarterInsidePadding.width)
  })
})
