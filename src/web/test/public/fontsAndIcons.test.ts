import { existsSync, readdirSync, readFileSync } from 'node:fs'
import path from 'node:path'
import { describe, expect, it } from 'vitest'
import { findForbiddenLicenses } from '../../scripts/licensePolicy.mjs'
import { publicFolder, webRoot } from './productionPackages'

interface PackageManifest {
  dependencies: Record<string, string>
  devDependencies: Record<string, string>
}

interface LicenseEntry {
  name: string
  version: string
  license?: string
}

const manifest: PackageManifest = JSON.parse(readFileSync(path.join(webRoot, 'package.json'), 'utf8'))
const sourceFolder = path.join(webRoot, 'src')
const indexCss = readFileSync(path.join(sourceFolder, 'index.css'), 'utf8')
const indexHtml = readFileSync(path.join(webRoot, 'index.html'), 'utf8')
const fontFile = /\.(woff2?|ttf|otf|eot)$/i
const iconOrFontPackage =
  /lucide|hugeicons|heroicons|phosphor|tabler|react-icons|fontawesome|font-awesome|fontsource|material-symbols|material-icons|iconify|remixicon|bootstrap-icons|icons?$/i
const cssPackagesImportedByIndexCss = ['shadcn', 'tw-animate-css', 'tailwindcss']

function findForbidden(entries: LicenseEntry[]): LicenseEntry[] {
  return findForbiddenLicenses(entries)
}

function filesBelow(folder: string, matches: (fileName: string) => boolean): string[] {
  return readdirSync(folder, { recursive: true, encoding: 'utf8' })
    .filter((relativePath) => matches(relativePath))
    .map((relativePath) => path.join(folder, relativePath))
}

function cssFilesOfPackage(name: string): string[] {
  const folder = path.join(webRoot, 'node_modules', name)
  return filesBelow(folder, (relativePath) => relativePath.endsWith('.css') && !relativePath.includes('node_modules'))
}

function installedLicense(name: string): LicenseEntry {
  const packageJsonPath = path.join(webRoot, 'node_modules', name, 'package.json')
  if (!existsSync(packageJsonPath)) {
    throw new Error(`${name} is in package.json but not installed: ${packageJsonPath}`)
  }
  const json: { version: string; license?: string } = JSON.parse(readFileSync(packageJsonPath, 'utf8'))
  return { name, version: json.version, license: json.license }
}

describe('fonts', () => {
  it('are the fonts already on the phone: index.css sets the sans font to system-ui', () => {
    expect(indexCss).toMatch(/--font-sans:\s*system-ui/)
  })

  it('are not downloaded or bundled: no @font-face anywhere in the css under src/', () => {
    const cssFiles = filesBelow(sourceFolder, (relativePath) => relativePath.endsWith('.css'))
    const offenders = cssFiles.filter((file) => /@font-face/i.test(readFileSync(file, 'utf8')))

    expect(cssFiles.length).toBeGreaterThan(0)
    expect(offenders).toEqual([])
  })

  it.each(cssPackagesImportedByIndexCss)('are not bundled by the css of %s, which index.css imports', (name) => {
    const offenders = cssFilesOfPackage(name).filter((file) => /@font-face/i.test(readFileSync(file, 'utf8')))

    expect(offenders).toEqual([])
  })

  it.each([
    ['index.css', indexCss],
    ['index.html', indexHtml],
  ])('are not loaded from Google Fonts or any other font host in %s', (_name, text) => {
    expect(text).not.toMatch(/fonts\.googleapis\.com|fonts\.gstatic\.com|use\.typekit\.net|fonts\.bunny\.net/i)
    expect(text).not.toMatch(/@import\s+url\(/i)
    expect(text).not.toMatch(/<link[^>]+rel=["']?(stylesheet|preload)["']?[^>]*font/i)
  })

  it('have no font file in public/, so none is copied into the site', () => {
    expect(filesBelow(publicFolder, (relativePath) => fontFile.test(relativePath))).toEqual([])
  })

  it('have no font file in src/, so none is bundled by Vite', () => {
    expect(filesBelow(sourceFolder, (relativePath) => fontFile.test(relativePath))).toEqual([])
  })
})

describe('icon and font packages in package.json', () => {
  const iconOrFontDependencies = [
    ...Object.keys(manifest.dependencies),
    ...Object.keys(manifest.devDependencies),
  ].filter((name) => iconOrFontPackage.test(name))

  it('are all production dependencies, because what the bundle ships must be listed in the licence file', () => {
    const onlyForDevelopment = iconOrFontDependencies.filter((name) => !(name in manifest.dependencies))

    expect(onlyForDevelopment).toEqual([])
  })

  it('all have a licence that the policy allows, read from the installed package', () => {
    const offenders = findForbidden(iconOrFontDependencies.map(installedLicense)).map(
      ({ name, license }) => `${name} (${license})`,
    )

    expect(offenders).toEqual([])
  })
})
