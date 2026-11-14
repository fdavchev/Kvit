import { execFileSync, execSync } from 'node:child_process'
import { existsSync, mkdirSync, readFileSync, rmSync, writeFileSync } from 'node:fs'
import path from 'node:path'
import { afterAll, beforeAll, describe, expect, it } from 'vitest'
import { findForbiddenLicenses } from '../../scripts/licensePolicy.mjs'
import {
  collapseWhitespace,
  licenceFilesOf,
  licencesFile,
  publicFolder,
  readProductionPackages,
  webRoot,
  type InstalledPackage,
} from './productionPackages'

const generatorScript = path.join(webRoot, 'scripts', 'licenses.mjs')
const staleMarker = 'STALE-LICENCE-FILE-MARKER'
const developmentOnlyPackages = [
  'jsdom',
  'oxlint',
  '@testing-library/react',
  '@cloudflare/workers-types',
  '@vitejs/plugin-react',
]
const minutes = 60_000

let generated = ''
let generatedBytes = Buffer.alloc(0)
let fileExistedBeforeTheRun = false

function findForbidden(packages: InstalledPackage[]): { name: string; version: string }[] {
  return findForbiddenLicenses(
    packages.map(({ name, version, license }) => ({ name, version, license })),
  )
}

function locationOf(address: string): string {
  const withoutShorthand = /^[\w.-]+\/[\w.-]+$/.test(address)
    ? `github.com/${address}`
    : address.replace(/^github:/, 'github.com/')
  return withoutShorthand
    .replace(/^git\+/, '')
    .replace(/^[a-z+]+:\/\//, '')
    .replace(/^git@/, '')
    .replace(/^([^/:]+):(?!\d)/, '$1/')
    .replace(/#.*$/, '')
    .replace(/\.git$/, '')
    .replace(/\/+$/, '')
    .toLowerCase()
}

function locationsOf(installed: InstalledPackage): string[] {
  return [installed.repository, installed.homepage]
    .filter((address): address is string => address !== undefined)
    .flatMap((address) => [address.toLowerCase(), locationOf(address)])
}

describe('scripts/licenses.mjs', () => {
  beforeAll(() => {
    mkdirSync(publicFolder, { recursive: true })
    fileExistedBeforeTheRun = existsSync(licencesFile)
    writeFileSync(licencesFile, staleMarker)
    execFileSync(process.execPath, [generatorScript], {
      cwd: webRoot,
      encoding: 'utf8',
      stdio: 'pipe',
      timeout: 3 * minutes,
    })
    generatedBytes = readFileSync(licencesFile)
    generated = generatedBytes.toString('utf8')
  }, 4 * minutes)

  afterAll(() => {
    if (!fileExistedBeforeTheRun) {
      rmSync(licencesFile, { force: true })
    }
  })

  it('writes public/third-party-licenses.txt and replaces what was in the file before', () => {
    expect(generated.length).toBeGreaterThan(0)
    expect(generated).not.toContain(staleMarker)
  })

  it('writes valid UTF-8 text without a byte order mark and without control bytes', () => {
    const decoder = new TextDecoder('utf-8', { fatal: true })

    expect(() => decoder.decode(generatedBytes)).not.toThrow()
    expect([...generatedBytes.subarray(0, 3)]).not.toEqual([0xef, 0xbb, 0xbf])
    expect(generated).not.toContain('\u0000')
    expect(generated).not.toContain('�')
  })

  it('is plain text, not HTML or JSON', () => {
    expect(generated.trimStart()).not.toMatch(/^(<!doctype|<html|[{[])/i)
  })

  it('names every production package, transitive ones included, with its version', () => {
    const missing = readProductionPackages()
      .filter(({ name, version }) => !generated.includes(name) || !generated.includes(version))
      .map(({ name, version }) => `${name}@${version}`)

    expect(missing).toEqual([])
  })

  it('gives the licence of every production package', () => {
    const missing = readProductionPackages()
      .filter(({ license }) => license !== undefined && !generated.includes(license))
      .map(({ name, version, license }) => `${name}@${version} (${license})`)

    expect(missing).toEqual([])
  })

  it('gives the repository or the homepage of every production package that has one', () => {
    const missing = readProductionPackages()
      .filter((installed) => {
        const locations = locationsOf(installed)
        return locations.length > 0 && !locations.some((location) => generated.toLowerCase().includes(location))
      })
      .map(({ name, version }) => `${name}@${version}`)

    expect(missing).toEqual([])
  })

  it('contains the full licence text of every production package that ships exactly one licence file', () => {
    const collapsed = collapseWhitespace(generated)
    const missing = readProductionPackages()
      .filter((installed) => {
        const files = licenceFilesOf(installed)
        return files.length === 1 && !collapsed.includes(collapseWhitespace(readFileSync(files[0], 'utf8')))
      })
      .map(({ name, version }) => `${name}@${version}`)

    expect(missing).toEqual([])
  })

  it.each(developmentOnlyPackages)('leaves out %s, which is only used while developing', (name) => {
    expect(readProductionPackages().map((installed) => installed.name)).not.toContain(name)
    expect(generated).not.toContain(name)
  })

  it('finds react and its dependency scheduler among the production packages, so the list is not empty by accident', () => {
    const names = readProductionPackages().map((installed) => installed.name)

    expect(names).toContain('react')
    expect(names).toContain('scheduler')
    expect(readProductionPackages().length).toBeGreaterThan(100)
  })
})

describe('the licences of the installed production packages', () => {
  it('include no copyleft or unknown licence that the policy forbids', () => {
    const offenders = findForbidden(readProductionPackages()).map(
      ({ name, version }) => `${name}@${version}`,
    )

    expect(offenders).toEqual([])
  })
})

describe('npm run check:licenses', () => {
  it(
    'exits with success on the installed production packages',
    () => {
      const run = () =>
        execSync('npm run check:licenses', {
          cwd: webRoot,
          encoding: 'utf8',
          stdio: 'pipe',
          timeout: 3 * minutes,
        })

      expect(run).not.toThrow()
    },
    4 * minutes,
  )
})
