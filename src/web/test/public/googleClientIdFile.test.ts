import { existsSync, readFileSync } from 'node:fs'
import path from 'node:path'
import { spawnSync } from 'node:child_process'
import { describe, expect, it } from 'vitest'

const webRoot = path.resolve(import.meta.dirname, '../..')
const repositoryRoot = path.resolve(webRoot, '../..')
const envFile = path.join(webRoot, '.env')
const appSettingsFile = path.join(repositoryRoot, 'src/api/Kvit.Api/appsettings.json')
const clientIdName = 'VITE_GOOGLE_CLIENT_ID'

function readEnvValue(name: string): string | undefined {
  const line = readFileSync(envFile, 'utf8')
    .split(/\r?\n/)
    .find((candidate) => candidate.startsWith(`${name}=`))
  return line?.slice(name.length + 1).trim().replace(/^(['"])(.*)\1$/, '$2')
}

function readApiClientId(): unknown {
  const settings: unknown = JSON.parse(readFileSync(appSettingsFile, 'utf8'))
  const google: unknown = Reflect.get(Object(settings), 'Google')
  return Reflect.get(Object(google), 'ClientId')
}

function isIgnoredByGit(file: string): boolean {
  const result = spawnSync('git', ['check-ignore', '--quiet', file], { cwd: repositoryRoot })
  if (result.error !== undefined) {
    throw result.error
  }
  if (result.status !== 0 && result.status !== 1) {
    throw new Error(`git check-ignore failed with status ${result.status}: ${result.stderr.toString()}`)
  }
  return result.status === 0
}

describe('src/web/.env', () => {
  it('exists', () => {
    expect(existsSync(envFile)).toBe(true)
  })

  it(`defines ${clientIdName} with a Google client id`, () => {
    expect(readEnvValue(clientIdName)).toMatch(/^\d+-[a-z0-9]+\.apps\.googleusercontent\.com$/)
  })

  it('holds the same client id as Google:ClientId in the API appsettings.json', () => {
    expect(readEnvValue(clientIdName)).toBe(readApiClientId())
  })

  it('is not ignored by git, so the public client id reaches the build', () => {
    expect(isIgnoredByGit(envFile)).toBe(false)
  })
})
