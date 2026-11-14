import { existsSync, readFileSync } from 'node:fs'
import path from 'node:path'
import { describe, expect, it } from 'vitest'
import { supportEmail } from '@/core/legal/legalInfo'

const webRoot = path.resolve(import.meta.dirname, '../..')
const repositoryRoot = path.resolve(webRoot, '../..')
const publicFolder = path.join(webRoot, 'public')
const securityTxtFile = path.join(publicFolder, '.well-known', 'security.txt')
const securityMdFile = path.join(repositoryRoot, 'SECURITY.md')
const isoDateTime = /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d+)?(Z|[+-]\d{2}:\d{2})$/

const securityTxt = readFileSync(securityTxtFile, 'utf8')

function readFields(text: string): Map<string, string[]> {
  const fields = new Map<string, string[]>()
  for (const line of text.split(/\r?\n/)) {
    if (line.trim() === '' || line.startsWith('#')) {
      continue
    }
    const separator = line.indexOf(':')
    if (separator === -1) {
      throw new Error(`The security.txt line is not a "Name: value" line: "${line}"`)
    }
    const name = line.slice(0, separator)
    fields.set(name, [...(fields.get(name) ?? []), line.slice(separator + 1).trim()])
  }
  return fields
}

function single(name: string): string {
  const values = readFields(securityTxt).get(name)
  if (values?.length !== 1) {
    throw new Error(`Expected exactly one "${name}" line in security.txt, found ${values?.length ?? 0}`)
  }
  return values[0]
}

function headerFilePatterns(text: string): string[] {
  return text
    .split(/\r?\n/)
    .filter((line) => line.trim() !== '' && !line.trim().startsWith('#') && !/^\s/.test(line))
    .map((line) => line.trim())
}

describe('public/.well-known/security.txt', () => {
  it('exists under public/.well-known/, which Vite copies to the root of the site as /.well-known/security.txt', () => {
    expect(existsSync(securityTxtFile)).toBe(true)
  })

  it('has a Contact that is the support email from legalInfo.ts as a mailto address', () => {
    expect(single('Contact')).toBe(`mailto:${supportEmail}`)
  })

  it('has the Contact mailto:kvit.support@gmail.com', () => {
    expect(single('Contact')).toBe('mailto:kvit.support@gmail.com')
  })

  it('has an Expires that is an ISO 8601 date-time', () => {
    expect(single('Expires')).toMatch(isoDateTime)
    expect(Number.isNaN(Date.parse(single('Expires')))).toBe(false)
  })

  it('has an Expires still in the future; when this test starts failing, security.txt must be renewed (RFC 9116)', () => {
    expect(Date.parse(single('Expires'))).toBeGreaterThan(Date.now())
  })

  it('has an Expires less than one year from now, as RFC 9116 recommends', () => {
    const oneYearFromNow = new Date()
    oneYearFromNow.setUTCFullYear(oneYearFromNow.getUTCFullYear() + 1)

    expect(Date.parse(single('Expires'))).toBeLessThan(oneYearFromNow.getTime())
  })

  it('has the Canonical address of the live site', () => {
    expect(single('Canonical')).toBe('https://kvit-mk.pages.dev/.well-known/security.txt')
  })

  it('has the Preferred-Languages English and Macedonian', () => {
    expect(single('Preferred-Languages')).toBe('en, mk')
  })

  it('has a Policy that points to SECURITY.md over https', () => {
    const policy = single('Policy')

    expect(policy).toMatch(/^https:\/\//)
    expect(policy).toMatch(/\/SECURITY\.md$/)
  })

  it('is plain ASCII text without a byte order mark', () => {
    const notPlainAscii = [...securityTxt].filter((character) => {
      const code = character.charCodeAt(0)
      return code > 126 || (code < 32 && character !== '\n' && character !== '\r')
    })

    expect(notPlainAscii).toEqual([])
  })

  it('uses LF or CRLF line endings and never a lone CR', () => {
    expect(securityTxt).not.toMatch(/\r(?!\n)/)
  })

  it('ends with a line break', () => {
    expect(securityTxt.endsWith('\n')).toBe(true)
  })

  it('has no line that is not a "Name: value" line or a comment', () => {
    expect(() => readFields(securityTxt)).not.toThrow()
  })
})

describe('how the site serves /.well-known/security.txt', () => {
  const viteConfig = readFileSync(path.join(webRoot, 'vite.config.ts'), 'utf8')

  it('uses the default publicDir of Vite, so everything in public/ is copied to the site root', () => {
    expect(viteConfig).not.toMatch(/\bpublicDir\b/)
  })

  it('does not turn off the copying of the public folder in the build', () => {
    expect(viteConfig).not.toMatch(/\bcopyPublicDir\b/)
  })

  it('keeps the Vite root at the folder with vite.config.ts, so public/ means src/web/public', () => {
    expect(viteConfig).not.toMatch(/\broot\s*:/)
  })

  it('has _headers rules for /* and /third-party-licenses.txt only, so no rule is written for the /.well-known path', () => {
    const headers = readFileSync(path.join(publicFolder, '_headers'), 'utf8')

    expect(headerFilePatterns(headers)).toEqual(['/*', '/third-party-licenses.txt'])
    expect(headers).not.toContain('.well-known')
  })

  it.each(['location', 'refresh'])('has no %s header in _headers that could send the path somewhere else', (name) => {
    const headers = readFileSync(path.join(publicFolder, '_headers'), 'utf8')

    expect(headers.toLowerCase()).not.toMatch(new RegExp(`^\\s+${name}\\s*:`, 'm'))
  })

  it.each([path.join(publicFolder, '_redirects'), path.join(webRoot, '_redirects')])('has no _redirects file at %s that could swallow /.well-known/*', (file) => {
    expect(existsSync(file)).toBe(false)
  })

  it('lets the Pages Function run only for /api/*, so /.well-known/security.txt is served as a plain file', () => {
    const routes: unknown = JSON.parse(readFileSync(path.join(publicFolder, '_routes.json'), 'utf8'))

    expect(Reflect.get(Object(routes), 'include')).toEqual(['/api/*'])
  })
})

describe('SECURITY.md in the root of the repository', () => {
  it('exists', () => {
    expect(existsSync(securityMdFile)).toBe(true)
  })

  it('names the support email address', () => {
    expect(readFileSync(securityMdFile, 'utf8')).toContain(supportEmail)
    expect(readFileSync(securityMdFile, 'utf8')).toContain('kvit.support@gmail.com')
  })

  it('mentions /.well-known/security.txt', () => {
    expect(readFileSync(securityMdFile, 'utf8')).toContain('/.well-known/security.txt')
  })
})
