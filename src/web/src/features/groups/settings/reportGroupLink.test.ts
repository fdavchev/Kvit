import { describe, expect, it } from 'vitest'
import { supportEmail } from '@/core/legal/legalInfo'
import { testGroupId } from '@/test/groupTestData'
import { parseMailto } from '@/test/mailtoTestHelpers'
import { reportGroupLink } from './reportGroupLink'

const subject = 'Report a Kvit group'
const body = `Group number: ${testGroupId}\n\nWhat is wrong:\n`
const safeEncodedValue = /^[A-Za-z0-9\-_.!~*'()%]*$/

function queryOf(href: string): string {
  return href.slice(href.indexOf('?') + 1)
}

describe('reportGroupLink', () => {
  it('starts with mailto: and the support address from legalInfo.ts, then the subject', () => {
    const href = reportGroupLink(subject, body)

    expect(href.startsWith(`mailto:${supportEmail}?subject=`)).toBe(true)
  })

  it('sends the report to kvit.support@gmail.com with the @ sign left as it is', () => {
    const href = reportGroupLink(subject, body)

    expect(href.startsWith('mailto:kvit.support@gmail.com?')).toBe(true)
  })

  it('has exactly two parameters, the subject first and the body second', () => {
    const parsed = parseMailto(reportGroupLink(subject, body))

    expect(parsed.parameterNames).toEqual(['subject', 'body'])
  })

  it('gives back the subject unchanged when it is decoded', () => {
    const parsed = parseMailto(reportGroupLink(subject, body))

    expect(parsed.parameters.subject).toBe(subject)
  })

  it('gives back the body with the group number unchanged when it is decoded, with each line break as CR LF', () => {
    const parsed = parseMailto(reportGroupLink(subject, body))

    expect(parsed.parameters.body).toBe(body.replaceAll('\n', '\r\n'))
    expect(parsed.parameters.body).toContain(testGroupId)
  })

  it('writes every line break of the body as %0D%0A and keeps the text around it', () => {
    const href = reportGroupLink('S', 'a\n\nb')

    expect(href).toBe(`mailto:${supportEmail}?subject=S&body=a%0D%0A%0D%0Ab`)
  })

  it('never writes a line break as a lone %0A', () => {
    const href = reportGroupLink(subject, body)

    expect(href).toContain('%0D%0A')
    expect(href).not.toMatch(/(?<!%0D)%0A/)
  })

  it('writes a line break in the subject as %0D%0A too', () => {
    const href = reportGroupLink('first\nsecond', 'x')

    expect(href).toBe(`mailto:${supportEmail}?subject=first%0D%0Asecond&body=x`)
  })

  it('encodes the subject and the body with encodeURIComponent', () => {
    const text = 'Fish & chips = 100% #1 (why?) +1'

    const href = reportGroupLink(text, text)

    expect(href).toBe(
      `mailto:${supportEmail}?subject=${encodeURIComponent(text)}&body=${encodeURIComponent(text)}`,
    )
  })

  it.each([
    ['an ampersand', 'Fish & chips'],
    ['an equals sign', 'a=b'],
    ['a question mark', 'why?'],
    ['a hash sign', '#1'],
    ['a plus sign', '1+1'],
    ['a percent sign', '100%'],
    ['a space', 'a b'],
    ['quotes', `"it's"`],
    ['Cyrillic letters', 'Пријава на група'],
    ['an emoji', '\u{1F3D6}\u{FE0F}'],
  ])('keeps %s in the subject and in the body when they are decoded, and adds no extra parameter', (_name, text) => {
    const href = reportGroupLink(text, text)

    const parsed = parseMailto(href)

    expect(parsed.parameterNames).toEqual(['subject', 'body'])
    expect(parsed.parameters.subject).toBe(text)
    expect(parsed.parameters.body).toBe(text)
  })

  it('leaves no character in the query that could end or split it', () => {
    const href = reportGroupLink('a & b = c? #d +e f', 'g & h = i? #j +k l\nm')

    const [subjectPair, bodyPair, ...others] = queryOf(href).split('&')

    expect(others).toEqual([])
    expect(subjectPair.slice('subject='.length)).toMatch(safeEncodedValue)
    expect(bodyPair.slice('body='.length)).toMatch(safeEncodedValue)
  })

  it('writes an empty subject and an empty body as empty parameters', () => {
    expect(reportGroupLink('', '')).toBe(`mailto:${supportEmail}?subject=&body=`)
  })
})
