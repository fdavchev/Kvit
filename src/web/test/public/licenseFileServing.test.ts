import { existsSync, readFileSync } from 'node:fs'
import path from 'node:path'
import { describe, expect, it } from 'vitest'
import { parseHeadersFile } from './headersFileParser'
import { publicFolder, webRoot } from './productionPackages'

const licencesUrlPath = '/third-party-licenses.txt'
const headersText = readFileSync(path.join(publicFolder, '_headers'), 'utf8')
const rules = parseHeadersFile(headersText.split(/\r?\n/))
const licencesRules = rules.filter((rule) => rule.pattern === licencesUrlPath)

describe('how the site serves /third-party-licenses.txt', () => {
  it('has exactly one _headers rule for the file', () => {
    expect(licencesRules).toHaveLength(1)
  })

  it('sends Content-Type: text/plain; charset=utf-8 for the file', () => {
    expect(licencesRules[0]?.headers.get('content-type')).toBe('text/plain; charset=utf-8')
  })

  it('does not force a download, so the link opens the text in the new tab', () => {
    expect(licencesRules[0]?.headers.has('content-disposition')).toBe(false)
  })

  it.each(['location', 'refresh'])('has no %s header in the rule for the file that could send it somewhere else', (name) => {
    expect(licencesRules[0]?.headers.has(name)).toBe(false)
  })

  it('keeps the rule for /* in the file, so every page still gets the security headers', () => {
    const everyPage = rules.filter((rule) => rule.pattern === '/*')

    expect(everyPage).toHaveLength(1)
    expect(everyPage[0].headers.get('content-security-policy')).toBeTruthy()
    expect(everyPage[0].headers.get('x-content-type-options')).toBe('nosniff')
  })

  it.each([path.join(publicFolder, '_redirects'), path.join(webRoot, '_redirects')])('has no _redirects file at %s that could swallow the file', (file) => {
    expect(existsSync(file)).toBe(false)
  })

  it('lets the Pages Function run only for /api/*, with nothing else changed in _routes.json', () => {
    const routes: unknown = JSON.parse(readFileSync(path.join(publicFolder, '_routes.json'), 'utf8'))

    expect(routes).toEqual({ version: 1, include: ['/api/*'], exclude: [] })
  })
})
