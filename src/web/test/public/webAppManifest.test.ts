import { existsSync, readFileSync } from 'node:fs'
import path from 'node:path'
import { describe, expect, it } from 'vitest'
import { publicFolder, webRoot } from './productionPackages'

interface ManifestIcon {
  src: string
  sizes: string
  type: string
  purpose: string
}

interface WebAppManifest {
  name: string
  short_name: string
  start_url: string
  scope: string
  display: string
  background_color: string
  theme_color: string
  icons: ManifestIcon[]
}

const manifestUrlPath = '/manifest.webmanifest'
const pngSignature = Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a])
const indexHtml = readFileSync(path.join(webRoot, 'index.html'), 'utf8')
const page = new DOMParser().parseFromString(indexHtml, 'text/html')
const manifest: WebAppManifest = JSON.parse(readFileSync(path.join(publicFolder, 'manifest.webmanifest'), 'utf8'))

function publicFile(urlPath: string): string {
  return path.join(publicFolder, ...urlPath.split('/').filter((part) => part !== ''))
}

function pngSize(file: string): { width: number; height: number } {
  const bytes = readFileSync(file)
  if (!bytes.subarray(0, pngSignature.length).equals(pngSignature)) {
    throw new Error(`${file} is not a PNG file`)
  }
  return { width: bytes.readUInt32BE(16), height: bytes.readUInt32BE(20) }
}

function lightThemeColor(): string | undefined {
  return page
    .querySelector('meta[name="theme-color"][media="(prefers-color-scheme: light)"]')
    ?.getAttribute('content')
    ?.toLowerCase()
}

describe('index.html', () => {
  it('links the web app manifest exactly once', () => {
    const links = Array.from(page.querySelectorAll('link[rel="manifest"]'))

    expect(links.map((link) => link.getAttribute('href'))).toEqual([manifestUrlPath])
  })

  it('links an apple-touch-icon that exists as a 180 x 180 PNG', () => {
    const href = page.querySelector('link[rel="apple-touch-icon"]')?.getAttribute('href')

    expect(href).toBe('/apple-touch-icon.png')
    expect(pngSize(publicFile(href ?? ''))).toEqual({ width: 180, height: 180 })
  })
})

describe('the web app manifest', () => {
  it('opens the installed app on the start page, which asks the server who is signed in', () => {
    expect(manifest.start_url).toBe('/')
    expect(manifest.scope).toBe('/')
  })

  it('names the app Kvit and opens it without the browser bar', () => {
    expect(manifest.name).toBe('Kvit')
    expect(manifest.short_name).toBe('Kvit')
    expect(manifest.display).toBe('standalone')
  })

  it('uses the light page colour of index.html for the splash screen and the status bar', () => {
    expect(manifest.background_color).toBe(lightThemeColor())
    expect(manifest.theme_color).toBe(lightThemeColor())
  })

  it.each(['192x192', '512x512'])('has a %s PNG icon for any use', (sizes) => {
    const icons = manifest.icons.filter((icon) => icon.sizes === sizes && icon.purpose === 'any')

    expect(icons).toHaveLength(1)
    expect(icons[0].type).toBe('image/png')
  })

  it.each(manifest.icons.map((icon) => [icon.src, icon] as const))('has the icon file %s in public/ with the size it claims', (_src, icon) => {
    const file = publicFile(icon.src)
    const [width, height] = icon.sizes.split('x').map(Number)

    expect(existsSync(file)).toBe(true)
    expect(pngSize(file)).toEqual({ width, height })
  })
})

describe('how the site serves the manifest', () => {
  it('serves it as a file and not through the Pages Function, which only runs for /api/*', () => {
    const routes: { include: string[] } = JSON.parse(readFileSync(path.join(publicFolder, '_routes.json'), 'utf8'))

    expect(routes.include).toEqual(['/api/*'])
  })
})
