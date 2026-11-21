import { existsSync } from 'node:fs'
import { createServer } from 'node:net'
import path from 'node:path'
import { chromium, errors } from 'playwright'
import { preview } from 'vite'

const webRoot = path.resolve(import.meta.dirname, '..')
const builtPage = path.join(webRoot, 'dist', 'index.html')
const pagesToCheck = ['/welcome', '/login', '/privacy']
const textWaitMilliseconds = 15_000
const host = '127.0.0.1'

function findFreePort() {
  return new Promise((resolve, reject) => {
    const probe = createServer()
    probe.on('error', reject)
    probe.listen(0, host, () => {
      const { port } = probe.address()
      probe.close(() => resolve(port))
    })
  })
}

async function waitForBodyText(page) {
  try {
    await page.waitForFunction(() => document.body.innerText.trim().length > 0, undefined, { timeout: textWaitMilliseconds })
    return true
  } catch (error) {
    if (error instanceof errors.TimeoutError) {
      return false
    }
    throw error
  }
}

async function problemsOnPage(browser, origin, pagePath) {
  const page = await browser.newPage()
  const pageErrors = []
  page.on('pageerror', (error) => pageErrors.push(error.message))
  await page.route('**/api/**', (route) => route.abort('connectionrefused'))
  try {
    await page.goto(`${origin}${pagePath}`, { waitUntil: 'load' })
    const hasText = await waitForBodyText(page)
    await page.waitForLoadState('networkidle')
    const problems = pageErrors.map((message) => `page error: ${message}`)
    if (!hasText) {
      problems.push(`the page text stayed empty for ${textWaitMilliseconds / 1000} seconds`)
    }
    return problems
  } finally {
    await page.close()
  }
}

if (!existsSync(builtPage)) {
  console.error(`No built website at ${builtPage}. Run "npm run build" first, then "npm run smoke".`)
  process.exit(1)
}

const port = await findFreePort()
const server = await preview({ root: webRoot, logLevel: 'warn', preview: { host, port, strictPort: true, open: false } })
const origin = `http://${host}:${port}`
const browser = await chromium.launch()
const failures = []
try {
  for (const pagePath of pagesToCheck) {
    const problems = await problemsOnPage(browser, origin, pagePath)
    if (problems.length === 0) {
      console.log(`OK    ${pagePath}`)
    } else {
      console.log(`FAIL  ${pagePath}`)
      failures.push(...problems.map((problem) => `${pagePath}: ${problem}`))
    }
  }
} finally {
  await browser.close()
  await server.close()
}

if (failures.length > 0) {
  console.error(`The built website failed the smoke check in Chromium:\n  ${failures.join('\n  ')}`)
  process.exit(1)
}
console.log(`The built website drew ${pagesToCheck.length} pages in Chromium without page errors.`)
