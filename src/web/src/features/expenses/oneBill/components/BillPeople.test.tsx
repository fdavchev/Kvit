import { screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import type { Language } from '@/core/i18n/language'
import { renderElementWithProviders } from '@/test/renderWithProviders'
import { expectTapTarget } from '@/test/tapTargetTestHelpers'
import { translated } from '@/test/translated'
import type { ExpensePerson } from '../../shared/expensePeople'
import { BillPeople } from './BillPeople'

const longName = 'Александра-Христина Миладиновска-Јовановска'

const people: ExpensePerson[] = [
  { memberId: 'p1', name: 'Filip', pictureUrl: null, colorIndex: 0, isYou: true, isCurrentMember: true },
  { memberId: 'p2', name: 'Ana', pictureUrl: null, colorIndex: 1, isYou: false, isCurrentMember: true },
  { memberId: 'p3', name: longName, pictureUrl: null, colorIndex: 2, isYou: false, isCurrentMember: true },
]

async function renderedRemoveButton(name: string, language: Language = 'en'): Promise<HTMLElement> {
  await renderElementWithProviders(
    <BillPeople people={people} onRemove={() => {}} onAddName={() => {}} />,
    '/people',
    { language },
  )
  return screen.getByRole('button', { name: translated(language, 'oneBill.removeName', { name }) })
}

function chipOf(button: HTMLElement): HTMLElement {
  const chip = button.closest('li')
  if (chip === null) {
    throw new Error(`Expected the remove button to sit inside a list item: ${button.outerHTML}`)
  }
  return chip
}

function avatarOf(chip: HTMLElement): HTMLElement {
  const avatar = chip.firstElementChild
  if (!(avatar instanceof HTMLElement) || avatar.tagName === 'BUTTON') {
    throw new Error(`Expected the first element of the chip to be the avatar: ${chip.outerHTML}`)
  }
  return avatar
}

describe.each([['Ana'], [longName]])('the remove button of the chip of %s', (name) => {
  it('is a separate element beside the avatar, not wrapping it and not inside it', async () => {
    const button = await renderedRemoveButton(name)

    const avatar = avatarOf(chipOf(button))

    expect(button.contains(avatar)).toBe(false)
    expect(avatar.contains(button)).toBe(false)
  })

  it('is placed in the normal flow, not absolutely over the avatar, and neither is anything around it inside the chip', async () => {
    const button = await renderedRemoveButton(name)
    const chip = chipOf(button)

    for (let element: HTMLElement | null = button; element !== null && element !== chip; element = element.parentElement) {
      expect(Array.from(element.classList), `<${element.tagName.toLowerCase()} class="${element.className}">`).not.toContain('absolute')
    }
  })

  it('has no negative offset or margin that pulls it onto the avatar', async () => {
    const button = await renderedRemoveButton(name)

    const negativeClasses = Array.from(button.classList).filter((className) => className.startsWith('-'))

    expect(negativeClasses).toEqual([])
  })

  it('keeps a tap target of 44 px', async () => {
    const button = await renderedRemoveButton(name)

    expectTapTarget(button, `the remove button of ${name}`)
  })
})

describe('the chips of the people of a one bill', () => {
  it('gives no remove button to me', async () => {
    await renderedRemoveButton('Ana')

    expect(screen.queryByRole('button', { name: translated('en', 'oneBill.removeName', { name: 'Filip' }) })).toBeNull()
  })

  it('shows a remove button for the person in Macedonian too, with the same layout', async () => {
    const button = await renderedRemoveButton('Ana', 'mk')

    expect(Array.from(button.classList)).not.toContain('absolute')
  })
})

describe('the name under each circle of the people of a one bill', () => {
  const namedPeople: ExpensePerson[] = [
    { memberId: 'p1', name: 'Filip', pictureUrl: null, colorIndex: 0, isYou: true, isCurrentMember: true },
    { memberId: 'p2', name: 'Александар', pictureUrl: null, colorIndex: 1, isYou: false, isCurrentMember: true },
    { memberId: 'p3', name: 'Aaaaaaaaaaaaaaaaaaaaaaaaaaaaaa', pictureUrl: null, colorIndex: 2, isYou: false, isCurrentMember: true },
    { memberId: 'p4', name: 'Marija Petrovska', pictureUrl: null, colorIndex: 3, isYou: false, isCurrentMember: true },
  ]

  async function renderedLabel(text: string, language: Language = 'en'): Promise<HTMLElement> {
    await renderElementWithProviders(
      <BillPeople people={namedPeople} onRemove={() => {}} onAddName={() => {}} />,
      '/people',
      { language },
    )
    return screen.getByText(text)
  }

  it.each(namedPeople.filter((person) => !person.isYou).map((person) => [person.name]))(
    'lets the name %s use the whole width of its chip, at least the 4 rem circle width, up to two lines, centred, not cut to one line',
    async (name) => {
      const label = await renderedLabel(name)

      expect(Array.from(label.classList)).toEqual(
        expect.arrayContaining(['line-clamp-2', 'col-span-full', 'w-min', 'min-w-16', 'max-w-full', 'justify-self-start', 'text-center', 'break-words']),
      )
      expect(Array.from(label.classList)).not.toContain('max-w-16')
      expect(Array.from(label.classList)).not.toContain('truncate')
    },
  )

  it('keeps the chip of a person with a remove button 4 rem + 2.75 rem wide and my chip 4 rem wide, whatever the name', async () => {
    const label = await renderedLabel('Aaaaaaaaaaaaaaaaaaaaaaaaaaaaaa')
    const myLabel = screen.getByText(translated('en', 'expense.me'))

    expect(Array.from(label.closest('li')?.classList ?? [])).toContain('grid-cols-[4rem_2.75rem]')
    expect(Array.from(myLabel.closest('li')?.classList ?? [])).toContain('grid-cols-[4rem]')
  })

  it.each<Language>(['en', 'mk'])('gives the + Name label the same two-line look as the names (%s)', async (language) => {
    const label = await renderedLabel(translated(language, 'oneBill.nameLabel'), language)

    expect(Array.from(label.classList)).toEqual(expect.arrayContaining(['line-clamp-2', 'max-w-16', 'text-center', 'break-words']))
    expect(Array.from(label.classList)).not.toContain('truncate')
  })

  it('keeps the circles of all chips on the top edge of the row, so a two-line name does not push its neighbours down', async () => {
    const label = await renderedLabel('Marija Petrovska')

    const list = label.closest('ul')

    expect(Array.from(label.closest('li')?.classList ?? [])).toContain('items-start')
    expect(Array.from(list?.classList ?? [])).toContain('items-start')
  })
})
