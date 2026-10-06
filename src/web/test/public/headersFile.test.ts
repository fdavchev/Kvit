import { readFileSync } from 'node:fs'
import path from 'node:path'
import { describe, expect, it } from 'vitest'

interface HeaderRule {
  pattern: string
  headers: Map<string, string>
}

const maxRules = 100
const maxLineLength = 2000
const headersText = readFileSync(
  path.resolve(import.meta.dirname, '../../public/_headers'),
  'utf8',
)
const lines = headersText.split(/\r?\n/)

function parseHeadersFile(lines: string[]): HeaderRule[] {
  const rules: HeaderRule[] = []
  for (const line of lines) {
    if (line.trim() === '' || line.trim().startsWith('#')) {
      continue
    }
    if (!/^\s/.test(line)) {
      rules.push({ pattern: line.trim(), headers: new Map() })
      continue
    }
    const currentRule = rules.at(-1)
    const separator = line.indexOf(':')
    if (currentRule === undefined || separator === -1) {
      throw new Error(`The _headers line is not a "Name: value" line under a URL pattern: "${line}"`)
    }
    currentRule.headers.set(
      line.slice(0, separator).trim().toLowerCase(),
      line.slice(separator + 1).trim(),
    )
  }
  return rules
}

function parseDirectives(policy: string): Map<string, string[]> {
  const directives = new Map<string, string[]>()
  for (const part of policy.split(';')) {
    const [name, ...sources] = part.trim().split(/\s+/)
    if (name !== '') {
      directives.set(name, sources)
    }
  }
  return directives
}

const rules = parseHeadersFile(lines)
const everyPageRule = rules.find((rule) => rule.pattern === '/*')

function headerOfEveryPage(name: string): string | undefined {
  return everyPageRule?.headers.get(name.toLowerCase())
}

function directive(name: string): string[] {
  const policy = headerOfEveryPage('Content-Security-Policy')
  const sources = parseDirectives(policy ?? '').get(name)
  if (sources === undefined) {
    throw new Error(`The Content-Security-Policy has no "${name}" directive`)
  }
  return sources
}

describe('public/_headers', () => {
  it(`has at most ${maxRules} rules`, () => {
    expect(rules.length).toBeLessThanOrEqual(maxRules)
  })

  it(`has no line of ${maxLineLength} characters or more`, () => {
    const tooLong = lines.filter((line) => line.length >= maxLineLength)

    expect(tooLong).toEqual([])
  })

  it('has one rule for /* that covers every page', () => {
    expect(rules.filter((rule) => rule.pattern === '/*')).toHaveLength(1)
  })

  it('has no rule that mentions /api, because headers are not applied to Functions anyway', () => {
    expect(lines.filter((line) => line.includes('/api'))).toEqual([])
  })

  it.each([
    ['Cross-Origin-Opener-Policy', 'same-origin-allow-popups'],
    ['X-Content-Type-Options', 'nosniff'],
    ['Referrer-Policy', 'strict-origin-when-cross-origin'],
  ])('sends %s: %s on every page', (name, value) => {
    expect(headerOfEveryPage(name)).toBe(value)
  })

  it('sends a Content-Security-Policy on every page', () => {
    expect(headerOfEveryPage('Content-Security-Policy')).toBeTruthy()
  })

  it.each([
    ['default-src', ["'self'"]],
    ['object-src', ["'none'"]],
    ['base-uri', ["'self'"]],
    ['form-action', ["'self'"]],
    ['frame-ancestors', ["'none'"]],
  ])('limits %s to exactly %j', (name, sources) => {
    expect(directive(name)).toEqual(sources)
  })

  it.each([
    ['script-src', ["'self'", 'https://accounts.google.com/gsi/client']],
    ['style-src', ["'self'", "'unsafe-inline'", 'https://accounts.google.com/gsi/style']],
    ['frame-src', ['https://accounts.google.com/gsi/']],
    ['connect-src', ["'self'", 'https://accounts.google.com/gsi/']],
    ['img-src', ["'self'", 'data:', 'https://*.googleusercontent.com']],
  ])('allows %s to use at least %j', (name, required) => {
    expect(directive(name)).toEqual(expect.arrayContaining(required))
  })

  it.each(["'unsafe-inline'", "'unsafe-eval'", '*'])('does not allow %s in script-src', (source) => {
    expect(directive('script-src')).not.toContain(source)
  })

  it.each(['*', 'https:', 'http:', "'unsafe-inline'"])('does not allow %s in img-src, so only the Google picture host is added', (source) => {
    expect(directive('img-src')).not.toContain(source)
  })

  it('names the Google picture host only once in img-src', () => {
    expect(
      directive('img-src').filter((source) => source === 'https://*.googleusercontent.com'),
    ).toHaveLength(1)
  })

  it('does not let a script load from the Google picture host', () => {
    expect(directive('script-src').join(' ')).not.toContain('googleusercontent')
  })
})
