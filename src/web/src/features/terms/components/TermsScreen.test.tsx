import { screen, within } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { languages, type Language } from '@/core/i18n/language'
import { routes } from '@/core/router/routes'
import { stubFetch } from '@/test/apiTestHelpers'
import {
  countOfEmailsInDocument,
  elementShowingText,
  emailCountIn,
  mailtoLinks,
  numbersUpTo,
  sectionBody,
  sectionHeading,
  shortItems,
  shortTitle,
  supportEmailText,
  supportMailtoHref,
} from '@/test/legalScreenTestHelpers'
import { renderElementWithProviders } from '@/test/renderWithProviders'
import { translated } from '@/test/translated'
import { TermsScreen } from './TermsScreen'

const sectionCount = 15
const sectionNumbers = numbersUpTo(sectionCount)
const textWithTheEmail = 'sections.13.body'

const sectionsInEachLanguage = languages.flatMap((language) =>
  sectionNumbers.map((number) => [language, number] as const),
)

async function renderTerms(options: { language?: Language; routerState?: unknown } = {}) {
  return renderElementWithProviders(<TermsScreen />, routes.terms, {
    language: options.language ?? 'en',
    routerState: options.routerState,
  })
}

describe('TermsScreen', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it.each(languages)('shows the terms heading (%s)', async (language) => {
    await renderTerms({ language })

    expect(
      screen.getByRole('heading', { level: 1, name: translated(language, 'terms.title') }),
    ).toBeTruthy()
  })

  it.each(languages)('shows the last-updated line (%s)', async (language) => {
    await renderTerms({ language })

    expect(screen.getByText(translated(language, 'terms.updated'))).toBeTruthy()
  })

  it.each(languages)('shows the In short box with its title as a heading of level 2 (%s)', async (language) => {
    await renderTerms({ language })

    expect(
      screen.getByRole('heading', { level: 2, name: shortTitle(language, 'terms') }),
    ).toBeTruthy()
  })

  it.each(languages)('shows the five short items as one list in order (%s)', async (language) => {
    await renderTerms({ language })

    const items = within(screen.getByRole('list')).getAllByRole('listitem')

    expect(items.map((item) => item.textContent)).toEqual(shortItems(language, 'terms'))
  })

  it.each(languages)('puts the short list after the In short title and before the first section (%s)', async (language) => {
    await renderTerms({ language })

    const title = screen.getByRole('heading', { level: 2, name: shortTitle(language, 'terms') })
    const firstSection = screen.getByRole('heading', {
      level: 2,
      name: sectionHeading(language, 'terms', 1),
    })
    const list = screen.getByRole('list')

    expect(title.compareDocumentPosition(list)).toBe(Node.DOCUMENT_POSITION_FOLLOWING)
    expect(list.compareDocumentPosition(firstSection)).toBe(Node.DOCUMENT_POSITION_FOLLOWING)
  })

  it.each(languages)('shows the In short title and then fifteen section headings in order (%s)', async (language) => {
    await renderTerms({ language })

    const headings = screen
      .getAllByRole('heading', { level: 2 })
      .map((heading) => heading.textContent)

    expect(headings).toEqual([
      shortTitle(language, 'terms'),
      ...sectionNumbers.map((number) => sectionHeading(language, 'terms', number)),
    ])
  })

  it.each(sectionsInEachLanguage)('shows the paragraph of section %s-%i right after its heading', async (language, number) => {
    await renderTerms({ language })

    const heading = screen.getByRole('heading', {
      level: 2,
      name: sectionHeading(language, 'terms', number),
    })

    expect(heading.nextElementSibling?.textContent).toBe(sectionBody(language, 'terms', number))
  })

  it.each(languages)('turns the email address in the contact section into a mailto link and keeps the text the same (%s)', async (language) => {
    await renderTerms({ language })
    const text = translated(language, `terms.${textWithTheEmail}`)

    const element = elementShowingText(text)
    const links = within(element).getAllByRole('link')

    expect(links.map((link) => link.getAttribute('href'))).toEqual(
      Array(emailCountIn(text)).fill(supportMailtoHref),
    )
    expect(links.map((link) => link.textContent)).toEqual(
      Array(emailCountIn(text)).fill(supportEmailText),
    )
    expect(element.textContent).toBe(text)
  })

  it.each(languages)('has a mailto link for every email address in the texts and for nothing else (%s)', async (language) => {
    await renderTerms({ language })

    expect(mailtoLinks()).toHaveLength(countOfEmailsInDocument(language, 'terms', sectionCount))
    expect(mailtoLinks().length).toBeGreaterThan(0)
  })

  it('shows the screen without asking the server anything, so it works for a signed-out visitor', async () => {
    const fetchMock = stubFetch(new Response(null, { status: 401 }))

    await renderTerms()

    expect(fetchMock).not.toHaveBeenCalled()
  })

  it.each([
    ['no screen asked to come back', undefined, routes.welcome],
    ['the welcome screen', { from: routes.welcome }, routes.welcome],
    ['the settings screen', { from: routes.settings }, routes.settings],
  ])('has a back button that leads to the right screen when %s', async (_name, routerState, path) => {
    await renderTerms({ routerState })

    const back = screen.getByRole('link', { name: translated('en', 'common.back') })

    expect(back.getAttribute('href')).toBe(path)
  })
})
