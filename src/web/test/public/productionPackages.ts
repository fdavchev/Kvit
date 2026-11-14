import { execSync } from 'node:child_process'
import { readdirSync, readFileSync } from 'node:fs'
import path from 'node:path'

export const webRoot = path.resolve(import.meta.dirname, '../..')
export const repositoryRoot = path.resolve(webRoot, '../..')
export const publicFolder = path.join(webRoot, 'public')
export const licencesFile = path.join(publicFolder, 'third-party-licenses.txt')

export interface InstalledPackage {
  name: string
  version: string
  license: string | undefined
  directory: string
  repository: string | undefined
  homepage: string | undefined
}

interface PackageJson {
  name: string
  version: string
  license?: unknown
  repository?: unknown
  homepage?: unknown
}

const licenceFileName = /^(licen[sc]e|copying)(\..*)?$/i

let cachedProductionPackages: InstalledPackage[] | undefined

function readText(value: unknown): string | undefined {
  return typeof value === 'string' && value.trim() !== '' ? value : undefined
}

function readLicense(value: unknown): string | undefined {
  if (typeof value === 'object' && value !== null) {
    return readText(Reflect.get(value, 'type'))
  }
  return readText(value)
}

function readRepository(value: unknown): string | undefined {
  if (typeof value === 'object' && value !== null) {
    return readText(Reflect.get(value, 'url'))
  }
  return readText(value)
}

function readInstalledPackage(directory: string): InstalledPackage {
  const json: PackageJson = JSON.parse(readFileSync(path.join(directory, 'package.json'), 'utf8'))
  return {
    name: json.name,
    version: json.version,
    license: readLicense(json.license),
    directory,
    repository: readRepository(json.repository),
    homepage: readText(json.homepage),
  }
}

export function readProductionPackages(): InstalledPackage[] {
  if (cachedProductionPackages === undefined) {
    const listing = execSync('npm ls --omit=dev --all --parseable', {
      cwd: webRoot,
      encoding: 'utf8',
      maxBuffer: 64 * 1024 * 1024,
    })
    const directories = listing.split(/\r?\n/).filter((line) => line.trim() !== '')
    cachedProductionPackages = directories.slice(1).map(readInstalledPackage)
  }
  return cachedProductionPackages
}

export function licenceFilesOf(installed: InstalledPackage): string[] {
  return readdirSync(installed.directory)
    .filter((fileName) => licenceFileName.test(fileName))
    .map((fileName) => path.join(installed.directory, fileName))
}

export function collapseWhitespace(text: string): string {
  return text.replace(/\s+/g, ' ').trim()
}
