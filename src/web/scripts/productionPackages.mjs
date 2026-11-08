import { execSync } from 'node:child_process'
import { readdirSync, readFileSync } from 'node:fs'
import path from 'node:path'

const licenceFileName = /^(licen[sc]e|copying|notice)([.\-_].*)?$/i

function readText(value) {
  return typeof value === 'string' && value.trim() !== '' ? value : undefined
}

function readTextOrField(value, fieldName) {
  return typeof value === 'object' && value !== null ? readText(value[fieldName]) : readText(value)
}

function readInstalledPackage(directory) {
  const json = JSON.parse(readFileSync(path.join(directory, 'package.json'), 'utf8'))
  const licenceFiles = readdirSync(directory, { withFileTypes: true })
    .filter((entry) => entry.isFile() && licenceFileName.test(entry.name))
    .map((entry) => path.join(directory, entry.name))
    .sort()
  return {
    name: json.name,
    version: json.version,
    license: readTextOrField(json.license, 'type'),
    repository: readTextOrField(json.repository, 'url'),
    homepage: readText(json.homepage),
    licenceFiles,
  }
}

function byNameThenVersion(first, second) {
  return (
    first.name.localeCompare(second.name, 'en') ||
    first.version.localeCompare(second.version, 'en', { numeric: true })
  )
}

export function readProductionPackages(webRoot) {
  const listing = execSync('npm ls --omit=dev --all --parseable', {
    cwd: webRoot,
    encoding: 'utf8',
    maxBuffer: 64 * 1024 * 1024,
  })
  const directories = listing
    .split(/\r?\n/)
    .filter((line) => line.trim() !== '')
    .slice(1)
  const packagesByKey = new Map()
  for (const installed of directories.map(readInstalledPackage)) {
    const key = `${installed.name}@${installed.version}`
    if (!packagesByKey.has(key)) {
      packagesByKey.set(key, installed)
    }
  }
  return [...packagesByKey.values()].sort(byNameThenVersion)
}
