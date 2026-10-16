import { render } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { KvitUnbrokenSlashWords } from './KvitUnbrokenSlashWords'

function renderedSentence(text: string): HTMLElement {
  const { container } = render(
    <p>
      <KvitUnbrokenSlashWords text={text} />
    </p>,
  )
  return container
}

function unbrokenTexts(container: HTMLElement): string[] {
  return Array.from(container.querySelectorAll('.whitespace-nowrap')).map((element) => element.textContent ?? '')
}

const wordOfTwelveCharacters = 'abcde/fghijk'
const wordOfThirteenCharacters = 'abcde/fghijkl'

describe('KvitUnbrokenSlashWords', () => {
  it('keeps a short slash word on one line', () => {
    const container = renderedSentence('Марко платил/а 600 ден.')

    expect(unbrokenTexts(container)).toEqual(['платил/а'])
  })

  it('keeps the whole sentence text unchanged', () => {
    const container = renderedSentence('Марко платил/а 600 ден.')

    expect(container.textContent).toBe('Марко платил/а 600 ден.')
  })

  it('keeps a slash word of exactly 12 characters on one line', () => {
    expect(wordOfTwelveCharacters.length).toBe(12)

    const container = renderedSentence(`See ${wordOfTwelveCharacters} here`)

    expect(unbrokenTexts(container)).toEqual([wordOfTwelveCharacters])
  })

  it('lets a slash word of 13 characters wrap', () => {
    expect(wordOfThirteenCharacters.length).toBe(13)

    const container = renderedSentence(`See ${wordOfThirteenCharacters} here`)

    expect(unbrokenTexts(container)).toEqual([])
  })

  it('lets a long name with slashes wrap', () => {
    const longName = 'Роденденска/прослава/викенд/Охрид/со/многу/другари'

    const container = renderedSentence(`Ана платил/а во ${longName}`)

    expect(unbrokenTexts(container)).toEqual(['платил/а'])
    expect(container.textContent).toBe(`Ана платил/а во ${longName}`)
  })

  it('keeps each short slash word on its own and still lets a long one wrap in the same sentence', () => {
    const container = renderedSentence('a/b then Роденденска/прослава/викенд then c/d')

    expect(unbrokenTexts(container)).toEqual(['a/b', 'c/d'])
  })

  it('leaves a sentence without a slash as plain text with nothing to keep together', () => {
    const container = renderedSentence('Marko paid you 600 den.')

    expect(unbrokenTexts(container)).toEqual([])
    expect(container.textContent).toBe('Marko paid you 600 den.')
  })

  it('leaves an empty text empty', () => {
    const container = renderedSentence('')

    expect(unbrokenTexts(container)).toEqual([])
    expect(container.textContent).toBe('')
  })
})
