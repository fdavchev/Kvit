import { readFileSync } from 'node:fs'
import path from 'node:path'
import { describe, expect, it } from 'vitest'
import { legalVersion } from '@/core/legal/legalInfo'
import en from '@/core/i18n/locales/en.json'
import type { Language } from '@/core/i18n/language'
import { legalTexts } from '@/test/legalTextsTestData'
import { translated } from '@/test/translated'
import { repositoryRoot } from './productionPackages'

type LegalDocumentName = 'terms' | 'privacy'

interface Section {
  heading: string
  body: string
}

interface DocumentPart {
  title: string
  updated: string
  shortTitle: string
  shortItems: string[]
  sections: Section[]
}

interface DatedCopy {
  firstLine: string
  parts: Record<Language, DocumentPart>
}

const documents: { name: LegalDocumentName; firstLine: string; sectionCount: number }[] = [
  {
    name: 'terms',
    firstLine: `# Terms of Use, version ${legalVersion}`,
    sectionCount: Object.keys(en.terms.sections).length,
  },
  {
    name: 'privacy',
    firstLine: `# Privacy notice, version ${legalVersion}`,
    sectionCount: Object.keys(en.privacy.sections).length,
  },
]
const languages: Language[] = ['en', 'mk']
const approvedTexts = new Map(
  legalTexts.map(([key, english, macedonian]) => [key, { en: english, mk: macedonian }]),
)

function approved(language: Language, key: string): string {
  const text = approvedTexts.get(key)?.[language]
  if (text === undefined) {
    throw new Error(`legalTexts.test.ts has no approved ${language} text for the key "${key}"`)
  }
  return text
}

const sources: { name: string; textOf: (language: Language, key: string) => string }[] = [
  { name: 'the texts of the app (en.json and mk.json)', textOf: (language, key) => translated(language, key) },
  { name: 'the approved texts pinned in legalTexts.test.ts', textOf: approved },
]
const shortItemNumbers = [1, 2, 3, 4, 5]

function readDatedCopy(name: LegalDocumentName): string {
  return readFileSync(path.join(repositoryRoot, 'docs', 'legal', `${name}-${legalVersion}.md`), 'utf8').replace(
    /\r\n/g,
    '\n',
  )
}

function lineStartingWith(text: string, prefix: string): string {
  const line = text.split('\n').find((candidate) => candidate.startsWith(prefix))
  if (line === undefined) {
    throw new Error(`The dated copy has no line that starts with "${prefix}"`)
  }
  return line.slice(prefix.length).trim()
}

function parsePart(partText: string): DocumentPart {
  const [beforeShort, shortChunk, ...sectionChunks] = partText.split(/^### /m)
  if (shortChunk === undefined) {
    throw new Error('The dated copy has no "### " heading for the In short box')
  }
  const [shortTitle, ...shortLines] = shortChunk.split('\n')
  const updatedLine = lineStartingWith(beforeShort, '_')
  return {
    title: lineStartingWith(beforeShort, '## '),
    updated: updatedLine.replace(/_$/, ''),
    shortTitle: shortTitle.trim(),
    shortItems: shortLines.filter((line) => line.startsWith('- ')).map((line) => line.slice(2).trim()),
    sections: sectionChunks.map((chunk) => {
      const [headingLine, ...bodyLines] = chunk.split('\n')
      return { heading: headingLine.trim(), body: bodyLines.join('\n').trim() }
    }),
  }
}

function parseDatedCopy(name: LegalDocumentName): DatedCopy {
  const text = readDatedCopy(name)
  const parts = text.split(/^---$/m)
  if (parts.length !== 2) {
    throw new Error(`The dated copy of ${name} must have exactly one --- line between English and Macedonian, found ${parts.length - 1}`)
  }
  return {
    firstLine: text.split('\n')[0],
    parts: { en: parsePart(parts[0]), mk: parsePart(parts[1]) },
  }
}

function numbersUpTo(count: number): number[] {
  return Array.from({ length: count }, (_, index) => index + 1)
}

describe.each(documents)('the dated copy of the $name in docs/legal/', ({ name, firstLine, sectionCount }) => {
  const copy = parseDatedCopy(name)

  it('is named with the version of the legal texts in legalInfo.ts and says so in its first line', () => {
    expect(copy.firstLine).toBe(firstLine)
  })

  describe.each(sources)('matches $name', ({ textOf }) => {
    describe.each(languages)('in %s', (language) => {
      const part = copy.parts[language]

      it('has the title', () => {
        expect(part.title).toBe(textOf(language, `${name}.title`))
      })

      it('has the last-updated line', () => {
        expect(part.updated).toBe(textOf(language, `${name}.updated`))
      })

      it('has the In short title', () => {
        expect(part.shortTitle).toBe(textOf(language, `${name}.short.title`))
      })

      it('has the five In short items, in order', () => {
        expect(part.shortItems).toEqual(
          shortItemNumbers.map((number) => textOf(language, `${name}.short.items.${number}`)),
        )
      })

      it('has as many sections as the app', () => {
        expect(part.sections).toHaveLength(sectionCount)
      })

      it.each(numbersUpTo(sectionCount))('has section %i with the heading and the full text', (number) => {
        const section = part.sections[number - 1]

        expect(section).toEqual({
          heading: `${number}. ${textOf(language, `${name}.sections.${number}.heading`)}`,
          body: textOf(language, `${name}.sections.${number}.body`),
        })
      })
    })
  })
})
